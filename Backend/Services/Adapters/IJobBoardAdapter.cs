using Microsoft.Playwright;
using JobApplicationAssistant.Api.Models;

namespace JobApplicationAssistant.Api.Services.Adapters;

public interface IJobBoardAdapter
{
    bool CanHandle(string url);
    Task<bool> FillFormAsync(IPage page, UserProfile profile, CancellationToken ct);
}
