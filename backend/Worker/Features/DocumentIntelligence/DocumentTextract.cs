using System.Text;
using Amazon.Textract;
using Amazon.Textract.Model;


namespace Worker.Features.DocumentIntelligence;

public class DocumentTextract(IConfiguration configuration, ILogger<DocumentTextract> logger) : IDocumentTextract
{

    public async Task<StringBuilder> ExtractText(string documentName, CancellationToken cancellationToken)
    {

        var option = configuration.GetAWSOptions();
        var textractClient = option.CreateServiceClient<IAmazonTextract>();
        var bucketName = configuration.GetValue<string>("AwsS3BucketName");

        //Start document text detection
        var startRequest = new StartDocumentTextDetectionRequest
        {
            DocumentLocation = new DocumentLocation
            {
                S3Object = new S3Object
                {
                    Bucket = bucketName,
                    Name = documentName
                }
            }

        };

        var startRespose = await textractClient.StartDocumentTextDetectionAsync(startRequest, cancellationToken);

        var jobId = startRespose.JobId;

        //Poll until the job is done
        GetDocumentTextDetectionResponse result;
        do
        {
            await Task.Delay(5000);
            result = await textractClient.GetDocumentTextDetectionAsync(new GetDocumentTextDetectionRequest
            {
                JobId = jobId
            });
            logger.LogInformation("Job status for {jobId}: {status}", jobId, result.JobStatus);

        } while (result.JobStatus == JobStatus.IN_PROGRESS);

        //Now go thru all pages as results are paginated for multiple pages.
        string? nextToken = null;
        var allBlocks = new List<Block>();
        do
        {
            var response = await textractClient.GetDocumentTextDetectionAsync(new GetDocumentTextDetectionRequest
            {
                JobId = jobId,
                NextToken = nextToken
            });

            allBlocks.AddRange(response.Blocks);

            nextToken = response.NextToken;

        } while (!string.IsNullOrEmpty(nextToken));


        var sb = new StringBuilder();
        // Extract text
        foreach (var block in allBlocks.Where(b => b.BlockType == BlockType.LINE))
        {
            sb.AppendLine(block.Text);
        }

        return sb;

    }

}
