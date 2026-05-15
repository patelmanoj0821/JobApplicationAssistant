namespace JobApplicationAssistant.Api.Services;

public interface IAIService
{
    Task<string> ExtractJobDescriptionAsync(string pageContent, CancellationToken ct = default);
    Task<(string CoverLetter, string TailoredResumeTips)> TailorApplicationAsync(string jobDescription, string baseResume, CancellationToken ct = default);
}
