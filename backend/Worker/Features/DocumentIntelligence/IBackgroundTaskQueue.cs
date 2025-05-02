using System;

namespace Worker.Features.DocumentIntelligence;

public interface IBackgroundTaskQueue
{
    ValueTask QueueBackgroundWorkItemAsync(Func<BenefitDocMetadata, CancellationToken, ValueTask> workItem);
    ValueTask<Func<BenefitDocMetadata, CancellationToken, ValueTask>> DequeueAsync(BenefitDocMetadata benefitDocMetadata, CancellationToken cancellationToken);
}
