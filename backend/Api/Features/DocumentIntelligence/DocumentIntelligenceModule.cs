using System.Text;
using System.Text.Json;
using Amazon;
using Amazon.S3;
using Amazon.S3.Transfer;
using Amazon.Textract;
using Amazon.Textract.Model;
using AutoMapper;
using Ganss.Excel;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using DocumentFormat.OpenXml.Spreadsheet;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.SS.Util;

namespace Api.Features.DocumentIntelligence;

public class DocumentIntelligenceModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/extract", Extract);
        app.MapPost("/api/benefit/doc/upload", Upload).DisableAntiforgery();
        app.MapGet("/api/benefit/docs", GetAllBenefitDocs);
        app.MapGet("/api/benefit/testcase/download", DownloadTestCases);
    }


    internal async Task<IResult> DownloadTestCases(string fileName, IMongoDbRepository mongoDbRepository, IMapper mapper, IWebHostEnvironment webHostEnvironment, CancellationToken cancellationToken)
    {
        try
        {
            ArgumentNullException.ThrowIfNullOrEmpty(fileName);
            var generatedTestCases = await mongoDbRepository.GetGeneratedTestCasesAsync(fileName, cancellationToken);
            var testCaseseToExport = new List<TestCasesToExport>();
            foreach (var key in generatedTestCases.Keys)
            {
                var testCasesToExportPerKey = mapper.Map<List<TestCasesToExport>>(generatedTestCases[key]);
                var benefit = JsonSerializer.Deserialize<ExtractedBenefitResponse>(key, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                foreach (var testcase in testCasesToExportPerKey)
                {
                    testcase.Benefit = $"Benefit - {benefit?.Benefit}, In-Network Conditions - {benefit?.InNetworkConditions}, Out-of-Network Conditions - {benefit?.OutOfNetworkConditions}, Limitations - {benefit?.Limitations}";

                }
                testCaseseToExport.AddRange(testCasesToExportPerKey);
            }

            // var filePath = Path.Combine(webHostEnvironment.ContentRootPath, $"{fileName}_{DateTime.UtcNow.ToString("MMddyyyyhhmmss")}");
            var excelMapper = new ExcelMapper();
            var memoryStream = new MemoryStream();
            excelMapper.Save(memoryStream, testCaseseToExport, sheetName: "GeneratedTestCases");
            memoryStream.Position = 0;

            IWorkbook workbook = new XSSFWorkbook(memoryStream);
            var sheet = workbook.GetSheet("GeneratedTestCases");

            var columnsToConfig = new Dictionary<int, int>
            {
                [0] = 50,   // column A → 50 characters
                [1] = 40,   // column B → 40 characters
                [2] = 50,    // column D → 50 characters
                [4] = 50,
                [5] = 30,
                [6] = 30,
                [7] = 30
            };

            foreach (var kv in columnsToConfig)
            {
                sheet.SetColumnWidth(kv.Key, kv.Value * 256);
            }

            // 6. Create a single wrap-style and apply to all cells in those columns
            var wrapStyle = workbook.CreateCellStyle();
            wrapStyle.WrapText = true;               // ← wrap text in cell :contentReference[oaicite:5]{index=5}

            for (int r = sheet.FirstRowNum; r <= sheet.LastRowNum; r++)
            {
                var row = sheet.GetRow(r);
                if (row == null) continue;

                foreach (var colIndex in columnsToConfig.Keys)
                {
                    var cell = row.GetCell(colIndex) ?? row.CreateCell(colIndex);
                    cell.CellStyle = wrapStyle;
                }
            }

            sheet.CreateFreezePane(0, 1);
            var headerRow = sheet.GetRow(sheet.FirstRowNum)!;
            // create header font
            var headerFont = workbook.CreateFont();
            headerFont.IsBold = true;
            headerFont.Color = NPOI.SS.UserModel.IndexedColors.White.Index;
            // create header style
            var headerStyle = workbook.CreateCellStyle();
            headerStyle.SetFont(headerFont);
            headerStyle.Alignment = HorizontalAlignment.Center;
            headerStyle.VerticalAlignment = VerticalAlignment.Center;
            headerStyle.FillForegroundColor = NPOI.SS.UserModel.IndexedColors.DarkBlue.Index;
            headerStyle.FillPattern = FillPattern.SolidForeground;
            // apply to each cell in header
            for (int c = 0; c < headerRow.LastCellNum; c++)
            {
                var cell = headerRow.GetCell(c) ?? headerRow.CreateCell(c);
                cell.CellStyle = headerStyle;
            }

            //Create the disclaimer row
            CreateDisclaimerRow(ref workbook, ref sheet, testCaseseToExport.Count());


            using var finalStream = new MemoryStream();
            workbook.Write(finalStream);
            byte[] fileContents = finalStream.ToArray();

            return Results.File(
            fileContents: fileContents,
            contentType: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileDownloadName: $"{Path.GetFileNameWithoutExtension(fileName)}_{DateTime.Now.ToShortTimeString()}.xlsx");


        }
        catch (Exception e)
        {
            return Results.Problem($"Error getting generated test cases. Error - {e.Message}", statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    internal async Task<IResult> GetAllBenefitDocs(IMongoDbRepository mongoDbRepository, CancellationToken cancellationToken)
    {
        try
        {
            var docs = await mongoDbRepository.GetAllUploadedDocumetsAsync(cancellationToken);
            return Results.Json(docs, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
        catch (Exception e)
        {
            return Results.Problem($"Error getting benefit documents from repository. Error - {e.Message}", statusCode: StatusCodes.Status500InternalServerError);
        }

    }

    internal async Task<IResult> Extract([FromQuery] string documentName, IConfiguration configuration)
    {

        var option = configuration.GetAWSOptions();

        var textractClient = option.CreateServiceClient<IAmazonTextract>();

        var bucketName = "pdf-textract-soumikp";

        // Start Document Text Detection
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

        var startResponse = await textractClient.StartDocumentTextDetectionAsync(startRequest);
        var jobId = startResponse.JobId;

        // Poll until job is done
        GetDocumentTextDetectionResponse result;
        do
        {
            await Task.Delay(5000);
            result = await textractClient.GetDocumentTextDetectionAsync(new GetDocumentTextDetectionRequest
            {
                JobId = jobId
            });

            Console.WriteLine($"Job Status: {result.JobStatus}");
        }
        while (result.JobStatus == JobStatus.IN_PROGRESS);

        // Fetch All Pages (Paginated)
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

        return Results.Ok(sb.ToString());

    }

    internal async Task<IResult> Upload(IFormFile file, IConfiguration configuration, IMongoDbRepository mongoDbRepository, CancellationToken cancellationToken, HttpContext httpContext)
    {

        var options = configuration.GetAWSOptions();

        var s3Client = options.CreateServiceClient<IAmazonS3>();

        var formFile = file;

        var key = $"{Guid.NewGuid()}_{formFile.FileName}";

        var bucketName = "pdf-textract-soumikp";

        using (var stream = formFile.OpenReadStream())
        {
            var request = new TransferUtilityUploadRequest()
            {
                InputStream = stream,
                Key = key,
                BucketName = bucketName,
                ContentType = "application/pdf",

            };
            var fileTransferUtility = new TransferUtility(s3Client);
            await fileTransferUtility.UploadAsync(request, cancellationToken);

            //Upload the meatadata to MongoDB
            var benefitDocMetadata = new BenefitDocMetadata
            {
                DocumentName = key,
                User = string.IsNullOrEmpty(httpContext.User?.Identity?.Name) ? "0" : httpContext.User?.Identity?.Name

            };

            await mongoDbRepository.UploadBenefitDocMetaDataAsync(benefitDocMetadata, cancellationToken);

        }

        return Results.Ok("File uploaded successfully");
    }

    private static void CreateDisclaimerRow(ref IWorkbook workbook, ref ISheet sheet, int testCasesCount)
    {
        // Add the disclaimer text
        var disclaimer = $"Disclaimer:\n\n" +
                            "The data and insights provided by this AI system are generated based on algorithms and models that may not always reflect the most current or accurate information. " +
                            "While we strive to ensure the reliability and accuracy of the data, it is important to understand that the " +
                            "AI-generated content should not be solely relied upon for critical decision-making processes.\n\n" +
                            "Users are strongly advised to:\n\n" +
                            "Validate the Information: Cross-check the AI-generated data with other trusted sources to confirm its accuracy and relevance.\n" +
                            "Consult Experts: Seek advice from qualified professionals or subject matter experts before making any decisions based on the provided data.\n" +
                            "Exercise Caution: Be aware of the potential limitations and biases inherent in AI systems, and use the data as a supplementary resource rather than a definitive guide.\n\n" +
                            "By using this AI-generated data, you acknowledge that you understand these limitations and agree to use the information responsibly.";

        //Create the disclaimer row
        var disclaimerRow = sheet.CreateRow(testCasesCount + 2);
        var disclaimerCell = disclaimerRow.CreateCell(0);
        disclaimerCell.SetCellValue(disclaimer);

        // Set the row height and merge cells for the disclaimer
        disclaimerRow.HeightInPoints = 200;
        sheet.AddMergedRegion(new CellRangeAddress(testCasesCount + 2, testCasesCount + 2, 0, 4));
        disclaimerCell.CellStyle = GetDisclaimerCellStyle(ref workbook);

    }
    private static ICellStyle GetDisclaimerCellStyle(ref IWorkbook workbook)
    {
        var disclaimerCellStyle = workbook.CreateCellStyle();
        disclaimerCellStyle.Alignment = HorizontalAlignment.Left;
        disclaimerCellStyle.WrapText = true;
        disclaimerCellStyle.BorderBottom = BorderStyle.Thin;
        disclaimerCellStyle.BorderTop = BorderStyle.Thin;
        disclaimerCellStyle.BorderLeft = BorderStyle.Thin;
        disclaimerCellStyle.BorderRight = BorderStyle.Thin;
        var disclaimerFont = workbook.CreateFont();
        disclaimerFont.FontHeightInPoints = 10;
        disclaimerFont.Color = NPOI.SS.UserModel.IndexedColors.Red.Index;
        disclaimerFont.IsBold = true;
        disclaimerCellStyle.SetFont(disclaimerFont);
        return disclaimerCellStyle;

    }
}

