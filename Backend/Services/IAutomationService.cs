namespace JobApplicationAssistant.Api.Services;

public interface IAutomationService
{
    Task ProcessApplicationAsync(Guid applicationId, CancellationToken cancellationToken = default);
}
