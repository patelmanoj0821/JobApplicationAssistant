using System.Threading.Channels;

namespace JobApplicationAssistant.Api.Services;

public interface IApplicationQueue
{
    ValueTask EnqueueAsync(Guid applicationId, CancellationToken cancellationToken = default);
    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken = default);
}

public class ApplicationQueue : IApplicationQueue
{
    private readonly Channel<Guid> _queue;

    public ApplicationQueue()
    {
        // Unbounded channel for simplicity in MVP
        _queue = Channel.CreateUnbounded<Guid>();
    }

    public async ValueTask EnqueueAsync(Guid applicationId, CancellationToken cancellationToken = default)
    {
        await _queue.Writer.WriteAsync(applicationId, cancellationToken);
    }

    public async ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
