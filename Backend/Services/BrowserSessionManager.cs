using Microsoft.Playwright;

namespace JobApplicationAssistant.Api.Services;

public interface IBrowserSessionManager
{
    void RegisterSession(Guid applicationId, IPlaywright playwright, IBrowserContext context, IPage page);
    (IPlaywright Playwright, IBrowserContext Context, IPage Page)? GetSession(Guid applicationId);
    Task RemoveSessionAsync(Guid applicationId);
}

public class BrowserSessionManager : IBrowserSessionManager
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, (IPlaywright Playwright, IBrowserContext Context, IPage Page)> _sessions = new();

    public void RegisterSession(Guid applicationId, IPlaywright playwright, IBrowserContext context, IPage page)
    {
        _sessions.TryAdd(applicationId, (playwright, context, page));
    }

    public (IPlaywright Playwright, IBrowserContext Context, IPage Page)? GetSession(Guid applicationId)
    {
        return _sessions.TryGetValue(applicationId, out var session) ? session : null;
    }

    public async Task RemoveSessionAsync(Guid applicationId)
    {
        if (_sessions.TryRemove(applicationId, out var session))
        {
            try
            {
                // In persistent mode, we close the context
                await session.Context.CloseAsync();
                session.Playwright.Dispose();
            }
            catch { }
        }
    }
}
