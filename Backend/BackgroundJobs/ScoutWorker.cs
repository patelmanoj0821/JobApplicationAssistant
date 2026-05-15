using JobApplicationAssistant.Api.Data;
using JobApplicationAssistant.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace JobApplicationAssistant.Api.BackgroundJobs;

public class ScoutWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScoutWorker> _logger;

    public ScoutWorker(IServiceProvider serviceProvider, ILogger<ScoutWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scout Worker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var scoutService = scope.ServiceProvider.GetRequiredService<IScoutService>();
                    
                    var activeCriteria = await db.SearchCriteria
                        .Where(c => c.IsActive)
                        .ToListAsync(stoppingToken);

                    var userProfile = await db.UserProfiles.FirstOrDefaultAsync(stoppingToken);
                    if (userProfile == null) continue;

                    foreach (var criteria in activeCriteria)
                    {
                        var newJobs = await scoutService.ScoutNewJobsAsync(criteria, stoppingToken);
                        
                        foreach (var job in newJobs)
                        {
                            // Avoid duplicates
                            if (!await db.JobApplications.AnyAsync(a => a.JobPostingUrl == job.JobPostingUrl, stoppingToken))
                            {
                                job.UserProfileId = userProfile.Id;
                                db.JobApplications.Add(job);
                            }
                        }

                        criteria.LastRunAt = DateTime.UtcNow;
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }

                // Run every 24 hours to stay in free tier
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scouting process");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Retry sooner on error
            }
        }
    }
}
