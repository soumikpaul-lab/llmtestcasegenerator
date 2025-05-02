namespace Worker.Features.DocumentIntelligence;

public class DocIntelligenceMonitorLoop(IBackgroundTaskQueue backgroundTaskQueue, ILogger<DocIntelligenceMonitorLoop> logger, IHostApplicationLifetime hostApplicationLifetime, IDocIntelligenceRepository docIntelligenceRepository, IDocumentAnalysisProcessor documentAnalysisProcessor)
{
    public void MonitorLoop()
    {
        logger.LogInformation("Document processor loop has started");
        Task.Run(async () => await MonitorAsync());
    }

    private readonly CancellationToken _cancellation = hostApplicationLifetime.ApplicationStopping;
    private async ValueTask MonitorAsync()
    {
        while (!_cancellation.IsCancellationRequested)
        {
            try
            {
                var documentToQueue = await docIntelligenceRepository.GetBenefitDocMetadataAsync(DocumentProcessStatus.NotStarted, _cancellation);
                //If document is uploaded in less than 1 minute then skip
                if (documentToQueue != null)
                {
                    var timeDifference = DateTime.UtcNow - documentToQueue.UploadDateTime;
                    if (timeDifference.TotalMinutes < 1)
                    {
                        await Task.Delay(3000);
                        continue;
                    }
                }
                if (documentToQueue is null)
                {
                    await Task.Delay(3000);
                    continue;
                }
                //Update the status to InQueue
                documentToQueue.DocumentProcessStatus = DocumentProcessStatus.InQueue;
                var result = await docIntelligenceRepository.UpdateBenefitDocMetadataAsync(documentToQueue, _cancellation);

                logger.LogInformation($"Document {documentToQueue.DocumentName} has been queued");
                await backgroundTaskQueue.QueueBackgroundWorkItemAsync(documentAnalysisProcessor.BuildWorkItemAsync);

            }
            catch (Exception e)
            {
                logger.LogError($"Error in monitoring loop. Error : {e.Message}");
                throw;
            }

        }



    }

}
