using Microsoft.Playwright;
using JobApplicationAssistant.Api.Models;

namespace JobApplicationAssistant.Api.Services.Adapters;

public class GenericAdapter : IJobBoardAdapter
{
    public bool CanHandle(string url) => true; // Fallback

    public async Task<bool> FillFormAsync(IPage page, UserProfile profile, CancellationToken ct)
    {
        try
        {
            // Simple heuristics for common fields
            await FillFieldIfFound(page, "input[name*='name'], input[id*='name'], input[placeholder*='Name']", profile.FullName, ct);
            await FillFieldIfFound(page, "input[name*='email'], input[id*='email'], input[type='email']", profile.Email, ct);
            
            if (!string.IsNullOrEmpty(profile.LinkedInUrl))
            {
                await FillFieldIfFound(page, "input[name*='linkedin'], input[id*='linkedin'], input[placeholder*='LinkedIn']", profile.LinkedInUrl, ct);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task FillFieldIfFound(IPage page, string selector, string value, CancellationToken ct)
    {
        var element = await page.QuerySelectorAsync(selector);
        if (element != null)
        {
            await element.FillAsync(value);
        }
    }
}
