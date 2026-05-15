using Microsoft.EntityFrameworkCore;
using JobApplicationAssistant.Api.Data;
using JobApplicationAssistant.Api.Models;
using JobApplicationAssistant.Api.Services;
using JobApplicationAssistant.Api.BackgroundJobs;
using JobApplicationAssistant.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Playwright;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true) // Allow any origin
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for SignalR
    });
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IApplicationQueue, ApplicationQueue>();
builder.Services.AddSingleton<IBrowserSessionManager, BrowserSessionManager>();
builder.Services.AddScoped<IAutomationService, PlaywrightAutomationService>();
builder.Services.AddScoped<IAIService, GeminiService>();
builder.Services.AddScoped<IScoutService, UniversalScoutService>();
builder.Services.AddHostedService<AutomationWorker>();
builder.Services.AddHostedService<ScoutWorker>();
builder.Services.AddSignalR();

// Fix circular references in JSON serialization
builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
{
    options.SerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseStaticFiles();

app.MapHub<ApplicationHub>("/applicationHub");

// Seed data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    if (!db.UserProfiles.Any())
    {
        db.UserProfiles.Add(new UserProfile 
        { 
            FullName = "John Doe", 
            Email = "john.doe@example.com",
            LinkedInUrl = "https://linkedin.com/in/johndoe",
            BaseResumeText = "Senior Software Engineer with 10 years experience in .NET and React."
        });
        db.SaveChanges();
    }

    if (!db.SearchCriteria.Any())
    {
        db.SearchCriteria.Add(new SearchCriteria 
        { 
            TargetJobTitle = "Senior Software Engineer", 
            PreferredLocation = "Remote",
            IsActive = true 
        });
        db.SaveChanges();
    }
}

// UserProfile Endpoints
// ... (rest of endpoints)
// ... (omitted for brevity in replacement, but I will provide full file)
app.MapGet("/api/profiles", async (ApplicationDbContext db) =>
    await db.UserProfiles.ToListAsync());

app.MapGet("/api/profiles/{id}", async (Guid id, ApplicationDbContext db) =>
    await db.UserProfiles.FindAsync(id)
        is UserProfile profile
            ? Results.Ok(profile)
            : Results.NotFound());

app.MapPost("/api/profiles", async (UserProfile profile, ApplicationDbContext db) =>
{
    db.UserProfiles.Add(profile);
    await db.SaveChangesAsync();
    return Results.Created($"/api/profiles/{profile.Id}", profile);
});

app.MapPut("/api/profiles/{id}", async (Guid id, UserProfile inputProfile, ApplicationDbContext db) =>
{
    var profile = await db.UserProfiles.FindAsync(id);
    if (profile is null) return Results.NotFound();

    profile.FullName = inputProfile.FullName;
    profile.Email = inputProfile.Email;
    profile.SerializedResumeData = inputProfile.SerializedResumeData;
    profile.LinkedInUrl = inputProfile.LinkedInUrl;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

// JobApplication Endpoints
app.MapGet("/api/applications", async (ApplicationDbContext db) =>
    await db.JobApplications.Include(a => a.Logs).ToListAsync());

app.MapGet("/api/applications/{id}", async (Guid id, ApplicationDbContext db) =>
    await db.JobApplications.Include(a => a.Logs).FirstOrDefaultAsync(a => a.Id == id)
        is JobApplication application
            ? Results.Ok(application)
            : Results.NotFound());

app.MapPost("/api/applications", async (JobApplication application, ApplicationDbContext db) =>
{
    db.JobApplications.Add(application);
    await db.SaveChangesAsync();
    return Results.Created($"/api/applications/{application.Id}", application);
});

app.MapPut("/api/applications/{id}", async (Guid id, JobApplication inputApp, ApplicationDbContext db) =>
{
    var application = await db.JobApplications.FindAsync(id);
    if (application is null) return Results.NotFound();

    application.CompanyName = inputApp.CompanyName;
    application.JobTitle = inputApp.JobTitle;
    application.JobPostingUrl = inputApp.JobPostingUrl;
    application.State = inputApp.State;
    application.SubmittedAt = inputApp.SubmittedAt;

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapPost("/api/applications/{id}/process", async (Guid id, IApplicationQueue queue, ApplicationDbContext db) =>
{
    var application = await db.JobApplications.FindAsync(id);
    if (application is null) return Results.NotFound();

    await queue.EnqueueAsync(id);
    return Results.Accepted();
});

app.MapPost("/api/applications/{id}/approve", async (Guid id, IBrowserSessionManager sessionManager, ApplicationDbContext db, Microsoft.AspNetCore.SignalR.IHubContext<ApplicationHub> hubContext) =>
{
    var application = await db.JobApplications.FindAsync(id);
    if (application is null) return Results.NotFound();

    var session = sessionManager.GetSession(id);
    if (session == null) return Results.BadRequest("Automation session expired or not found. Please try manual submission.");

    try
    {
        var (playwright, context, page) = session.Value;

        // 1. Execute final submit
        // Logic depends on the site, but usually we click the primary submit button
        // For MVP, we attempt a generic locator click
        var submitButton = await page.QuerySelectorAsync("button[type='submit'], button:has-text('Submit'), button:has-text('Apply')");
        if (submitButton != null)
        {
            await submitButton.ClickAsync();
            await Task.Delay(5000); // Wait for "Thank You" page
        }

        // 2. Capture SUCCESS screenshot
        string successFileName = $"{application.Id}_success.png";
        string successPath = Path.Combine(app.Services.GetRequiredService<IWebHostEnvironment>().WebRootPath, "screenshots", successFileName);
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = successPath, FullPage = true });

        application.State = ApplicationState.Submitted;
        application.SubmittedAt = DateTime.UtcNow;
        
        db.ApplicationLogs.Add(new ApplicationLog
        {
            JobApplicationId = application.Id,
            Message = "Application submitted and success page captured.",
            ScreenshotLocalPath = $"/screenshots/{successFileName}",
            Level = LogLevel.Information
        });

        await db.SaveChangesAsync();
        await sessionManager.RemoveSessionAsync(id);
        await hubContext.Clients.All.SendAsync("ApplicationUpdated", application.Id);

        return Results.Ok();
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// JobApplication Endpoints
// ... (omitted)

// SearchCriteria Endpoints
app.MapGet("/api/criteria", async (ApplicationDbContext db) =>
    await db.SearchCriteria.ToListAsync());

app.MapPost("/api/criteria", async (SearchCriteria criteria, ApplicationDbContext db) =>
{
    db.SearchCriteria.Add(criteria);
    await db.SaveChangesAsync();
    return Results.Created($"/api/criteria/{criteria.Id}", criteria);
});

app.MapDelete("/api/criteria/{id}", async (Guid id, ApplicationDbContext db) =>
{
    var criteria = await db.SearchCriteria.FindAsync(id);
    if (criteria is null) return Results.NotFound();

    db.SearchCriteria.Remove(criteria);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.MapPost("/api/criteria/run-scout", async (IScoutService scoutService, ApplicationDbContext db) =>
{
    var criteria = await db.SearchCriteria.Where(c => c.IsActive).ToListAsync();
    var userProfile = await db.UserProfiles.FirstOrDefaultAsync();
    if (userProfile == null) return Results.BadRequest("User profile not found.");

    foreach (var c in criteria)
    {
        var newJobs = await scoutService.ScoutNewJobsAsync(c);
        foreach (var job in newJobs)
        {
            if (!await db.JobApplications.AnyAsync(a => a.JobPostingUrl == job.JobPostingUrl))
            {
                job.UserProfileId = userProfile.Id;
                db.JobApplications.Add(job);
            }
        }
        c.LastRunAt = DateTime.UtcNow;
    }
    await db.SaveChangesAsync();
    return Results.Ok();
});

app.MapGet("/health", () => Results.Ok(new { Status = "Healthy", Time = DateTime.UtcNow }));

app.Run();
