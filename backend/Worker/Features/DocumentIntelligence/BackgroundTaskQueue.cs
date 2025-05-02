using System;
using System.Threading.Channels;

namespace Worker.Features.DocumentIntelligence;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<BenefitDocMetadata, CancellationToken, ValueTask>> _queue;

    public BackgroundTaskQueue(int capacity)
    {
        BoundedChannelOptions options = new(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };

        _queue = Channel.CreateBounded<Func<BenefitDocMetadata, CancellationToken, ValueTask>>(options);

    }
    public async ValueTask<Func<BenefitDocMetadata, CancellationToken, ValueTask>> DequeueAsync(BenefitDocMetadata benefitDocMetadata, CancellationToken cancellationToken)
    {

        if (_queue.Reader.Count == 0)
        {
            return async (metadata, token) => await Task.Delay(5000, token);

        }
        var workItem = await _queue.Reader.ReadAsync(cancellationToken);

        return workItem;
    }

    public async ValueTask QueueBackgroundWorkItemAsync(Func<BenefitDocMetadata, CancellationToken, ValueTask> workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);

        await _queue.Writer.WriteAsync(workItem);
    }
}
