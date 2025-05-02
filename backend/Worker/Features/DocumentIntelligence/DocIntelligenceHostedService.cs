using System;

namespace Worker.Features.DocumentIntelligence;

public class DocIntelligenceHostedService(IBackgroundTaskQueue backgroundTaskQueue, ILogger<DocIntelligenceHostedService> logger, IDocIntelligenceRepository docIntelligenceRepository) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var document = await docIntelligenceRepository.GetBenefitDocMetadataAsync(DocumentProcessStatus.Processing, stoppingToken);
            if (document is null)
            {
                await ProcessTaskQueueAsync(stoppingToken);

            }

        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopped");
        return base.StopAsync(cancellationToken);
    }

    private async Task ProcessTaskQueueAsync(CancellationToken cancellationToken)
    {
        try
        {
            var document = await docIntelligenceRepository.GetBenefitDocMetadataAsync(DocumentProcessStatus.InQueue, cancellationToken);
            if (document is null)
            {
                return;

            }
            var workItem = await backgroundTaskQueue.DequeueAsync(document, cancellationToken);

            await workItem(document, cancellationToken);

        }
        catch (Exception ex)
        {
            logger.LogError("Error processing task in queue : {error}", ex.Message);
        }
    }
}

