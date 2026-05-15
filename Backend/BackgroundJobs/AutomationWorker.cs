using JobApplicationAssistant.Api.Services;

namespace JobApplicationAssistant.Api.BackgroundJobs;

public class AutomationWorker : BackgroundService
{
    private readonly IApplicationQueue _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutomationWorker> _logger;

    public AutomationWorker(
        IApplicationQueue queue,
        IServiceProvider serviceProvider,
        ILogger<AutomationWorker> logger)
    {
        _queue = queue;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Automation Worker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var applicationId = await _queue.DequeueAsync(stoppingToken);

                _logger.LogInformation("Processing application {Id}", applicationId);

                using (var scope = _serviceProvider.CreateScope())
                {
                    var automationService = scope.ServiceProvider.GetRequiredService<IAutomationService>();
                    await automationService.ProcessApplicationAsync(applicationId, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing job application");
            }
        }

        _logger.LogInformation("Automation Worker is stopping.");
    }
}
