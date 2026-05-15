using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using Microsoft.AspNetCore.SignalR;
using JobApplicationAssistant.Api.Data;
using JobApplicationAssistant.Api.Models;
using JobApplicationAssistant.Api.Hubs;
using JobApplicationAssistant.Api.Services.Adapters;

namespace JobApplicationAssistant.Api.Services;

public class PlaywrightAutomationService : IAutomationService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<PlaywrightAutomationService> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly IBrowserSessionManager _sessionManager;
    private readonly IHubContext<ApplicationHub> _hubContext;
    private readonly IAIService _aiService;
    private readonly List<IJobBoardAdapter> _adapters;

    public PlaywrightAutomationService(
        ApplicationDbContext db, 
        ILogger<PlaywrightAutomationService> logger,
        IWebHostEnvironment env,
        IBrowserSessionManager sessionManager,
        IHubContext<ApplicationHub> hubContext,
        IAIService aiService)
    {
        _db = db;
        _logger = logger;
        _env = env;
        _sessionManager = sessionManager;
        _hubContext = hubContext;
        _aiService = aiService;
        _adapters = new List<IJobBoardAdapter>
        {
            new LinkedInAdapter(),
            new GenericAdapter()
        };
    }

    public async Task ProcessApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        var application = await _db.JobApplications
            .Include(a => a.UserProfile)
            .FirstOrDefaultAsync(a => a.Id == applicationId, cancellationToken);

        if (application == null || application.UserProfile == null) return;

        try
        {
            application.State = ApplicationState.Analyzing;
            await _db.SaveChangesAsync(cancellationToken);
            await _hubContext.Clients.All.SendAsync("ApplicationUpdated", application.Id, cancellationToken);

            var playwright = await Playwright.CreateAsync();
            
            // USE PERSISTENT CONTEXT: This saves cookies/login state to the UserData folder
            var userDataPath = Path.Combine(Directory.GetCurrentDirectory(), "UserData");
            var browserContext = await playwright.Chromium.LaunchPersistentContextAsync(userDataPath, new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = true,
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/121.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
            });

            var page = browserContext.Pages.FirstOrDefault() ?? await browserContext.NewPageAsync();

            _logger.LogInformation("Navigating to {Url}", application.JobPostingUrl);
            await page.GotoAsync(application.JobPostingUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 60000 });
            await Task.Delay(5000, cancellationToken); 

            // AI STEP 1: Extract Job Description
            var pageContent = await page.InnerTextAsync("body");
            if (pageContent.Length > 10000) pageContent = pageContent.Substring(0, 10000);
            
            application.RawJobDescription = await _aiService.ExtractJobDescriptionAsync(pageContent, cancellationToken);
            
            application.State = ApplicationState.Tailoring;
            await _db.SaveChangesAsync(cancellationToken);
            await _hubContext.Clients.All.SendAsync("ApplicationUpdated", application.Id, cancellationToken);

            // AI STEP 2: Tailor Cover Letter and Resume
            var (coverLetter, resumeTips) = await _aiService.TailorApplicationAsync(
                application.RawJobDescription, 
                application.UserProfile.BaseResumeText ?? "No base resume provided.", 
                cancellationToken);
            
            application.TailoredCoverLetter = coverLetter;
            
            _db.ApplicationLogs.Add(new ApplicationLog
            {
                JobApplicationId = application.Id,
                Message = $"AI Tailoring Complete. Resume Tips: {resumeTips}",
                Level = LogLevel.Information
            });

            application.State = ApplicationState.FillingForm;
            await _db.SaveChangesAsync(cancellationToken);
            await _hubContext.Clients.All.SendAsync("ApplicationUpdated", application.Id, cancellationToken);

            // Select and execute adapter
            var adapter = _adapters.FirstOrDefault(a => a.CanHandle(application.JobPostingUrl)) ?? _adapters.Last();
            bool fillSuccess = await adapter.FillFormAsync(page, application.UserProfile, cancellationToken);

            string screenshotFileName = $"{application.Id}.png";
            string screenshotPath = Path.Combine(_env.WebRootPath, "screenshots", screenshotFileName);
            await page.ScreenshotAsync(new PageScreenshotOptions { Path = screenshotPath, FullPage = true });

            application.State = ApplicationState.AwaitingManualApproval;
            _db.ApplicationLogs.Add(new ApplicationLog
            {
                JobApplicationId = application.Id,
                Message = fillSuccess 
                    ? $"Form filled using {adapter.GetType().Name}. Awaiting manual review." 
                    : $"Automation ran via {adapter.GetType().Name} but may have missed some fields. Please review carefully.",
                ScreenshotLocalPath = $"/screenshots/{screenshotFileName}"
            });

            await _db.SaveChangesAsync(cancellationToken);
            
            // CRITICAL FIX: Keep playwright and context alive
            _sessionManager.RegisterSession(application.Id, playwright, browserContext, page);
            
            await _hubContext.Clients.All.SendAsync("ApplicationUpdated", application.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during automation for {Id}", applicationId);
            application.State = ApplicationState.Failed;
            _db.ApplicationLogs.Add(new ApplicationLog
            {
                JobApplicationId = application.Id,
                Message = $"Automation failed: {ex.Message}",
                Level = LogLevel.Error
            });
            await _db.SaveChangesAsync(cancellationToken);
            await _hubContext.Clients.All.SendAsync("ApplicationUpdated", application.Id, cancellationToken);
        }
    }
}
