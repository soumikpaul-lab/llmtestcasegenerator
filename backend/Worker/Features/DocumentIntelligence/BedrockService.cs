using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Polly;

namespace Worker.Features.DocumentIntelligence;

public class BedrockService : IBedrockService
{

    private readonly IAmazonBedrockRuntime _amazonBedrockRuntime;
    private readonly string _modelId;

    private readonly ILogger<BedrockService> _logger;

    public BedrockService(IAmazonBedrockRuntime amazonBedrockRuntime, IConfiguration configuration, ILogger<BedrockService> logger)
    {
        _amazonBedrockRuntime = amazonBedrockRuntime;
        _modelId = configuration.GetSection("Bedrock:ModelId").Get<string>() ?? throw new ArgumentNullException("Model Id is not supplied");
        _logger = logger;

    }

    public async Task<ExtractedBenefitResponse> GetBenefitWithConditions(string benefitName, string relevantSegment, CancellationToken token)
    {
        var requestBody = JsonSerializer.Serialize(new
        {
            anthropic_version = "bedrock-2023-05-31",
            max_tokens = 4096,
            temperature = 0.0,
            messages = new[]
            {
                new {role = "assistant", content = "You are an AI Assitant"},
                new {role = "user", content = BuildPromptToExtractBenefitConditions(benefitName,relevantSegment) }
            }
        });

        var request = new InvokeModelRequest
        {
            ModelId = _modelId,
            Body = new MemoryStream(Encoding.UTF8.GetBytes(requestBody)),
            ContentType = "application/json",
            Accept = "application/json"
        };

        try
        {
            var retryPolicy = Policy
                   .Handle<Exception>()
                   .WaitAndRetryAsync(
                       retryCount: 5,
                       sleepDurationProvider: attempt => TimeSpan.FromMinutes(Math.Pow(2, attempt)),
                       onRetry: (exception, delay, attempt, context) =>
                       {
                           _logger.LogError($"Retry {attempt}: waiting {delay.TotalSeconds}s after error: {exception.Message}");
                       }

                   );
            var respose = await retryPolicy.ExecuteAsync(async () => await _amazonBedrockRuntime.InvokeModelAsync(request, token));
            //_amazonBedrockRuntime.InvokeModelAsync(request, token);
            //Decode the response body
            var modelResponse = await JsonNode.ParseAsync(respose.Body);
            var responseJsonNode = modelResponse?["content"]?[0]?["text"] ?? "";
            var responseText = JsonSerializer.Deserialize<string>(responseJsonNode.ToJsonString());
            ArgumentException.ThrowIfNullOrEmpty(responseText);
            var benefitResponse = JsonSerializer.Deserialize<ExtractedBenefitResponse>(responseText, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true
            });

            return benefitResponse;
        }
        catch (AmazonBedrockRuntimeException e)
        {
            _logger.LogError($"ERROR: Can't invoke '{_modelId}'. Reason: {e.Message}");
            throw;
        }

    }

    public async Task<List<GeneratedTestCase>> GenerateTestCases(string benefitPrompt, CancellationToken cancellationToken)
    {
        var requestBody = JsonSerializer.Serialize(new
        {
            anthropic_version = "bedrock-2023-05-31",
            max_tokens = 4096,
            temperature = 0.0,
            messages = new[]
            {
                new {role = "assistant", content = "You are an AI Assitant"},
                new {role = "user", content = BuildPromptToGenerateTestCase(benefitPrompt) }
            }
        });

        var request = new InvokeModelRequest
        {
            ModelId = _modelId,
            Body = new MemoryStream(Encoding.UTF8.GetBytes(requestBody)),
            ContentType = "application/json",
            Accept = "application/json"
        };

        try
        {
            var retryPolicy = Policy
                   .Handle<Exception>()
                   .WaitAndRetryAsync(
                       retryCount: 5,
                       sleepDurationProvider: attempt => TimeSpan.FromMinutes(Math.Pow(2, attempt)),
                       onRetry: (exception, delay, attempt, context) =>
                       {
                           _logger.LogError($"Retry {attempt}: waiting {delay.TotalSeconds}s after error: {exception.Message}");
                       }

                   );
            var respose = await retryPolicy.ExecuteAsync(async () => await _amazonBedrockRuntime.InvokeModelAsync(request, cancellationToken));
            var modelResponse = await JsonNode.ParseAsync(respose.Body);
            var responseJsonNode = modelResponse?["content"]?[0]?["text"] ?? "";
            var responseText = JsonSerializer.Deserialize<string>(responseJsonNode.ToJsonString());
            ArgumentException.ThrowIfNullOrEmpty(responseText);
            var generatedTestCases = JsonSerializer.Deserialize<List<GeneratedTestCase>>(responseText, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true
            });



            return generatedTestCases is null ? [] : generatedTestCases;

        }
        catch (AmazonBedrockRuntimeException e)
        {
            _logger.LogError($"Error while generating test cases for benefit {benefitPrompt} with error {e.Message}", e);
            return [];

        }
        catch (Exception e)
        {
            _logger.LogError($"Error while generating test cases for benefit {benefitPrompt} with error {e.Message}", e);
            return [];
        }
    }

    public async Task<HashSet<string>> GetBenefitNames(string segment, CancellationToken token, HashSet<string>? identifiedBenefits = null)
    {
        var requestBody = JsonSerializer.Serialize(new
        {
            anthropic_version = "bedrock-2023-05-31",
            max_tokens = 4096,
            temperature = 0.0,
            messages = new[]
            {
                new {role = "assistant", content = "You are an AI Assitant"},
                new {role = "user", content = BuildPromptToExtractBenefitNames(segment, identifiedBenefits) }
            }
        });

        var request = new InvokeModelRequest
        {
            ModelId = _modelId,
            Body = new MemoryStream(Encoding.UTF8.GetBytes(requestBody)),
            ContentType = "application/json",
            Accept = "application/json"
        };

        try
        {
            var retryPolicy = Policy
                   .Handle<Exception>()
                   .WaitAndRetryAsync(
                       retryCount: 5,
                       sleepDurationProvider: attempt => TimeSpan.FromMinutes(Math.Pow(2, attempt)),
                       onRetry: (exception, delay, attempt, context) =>
                       {
                           _logger.LogError($"Retry {attempt}: waiting {delay.TotalSeconds}s after error: {exception.Message}");
                       }

                   );
            var respose = await retryPolicy.ExecuteAsync(async () => await _amazonBedrockRuntime.InvokeModelAsync(request, token));
            //Decode the response body
            var modelResponse = await JsonNode.ParseAsync(respose.Body);
            var responseJsonNode = modelResponse?["content"]?[0]?["text"] ?? "";
            var responseText = JsonSerializer.Deserialize<string>(responseJsonNode.ToJsonString());
            ArgumentException.ThrowIfNullOrEmpty(responseText);
            var benefitResponse = JsonSerializer.Deserialize<BenefitNameResponse>(responseText, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                PropertyNameCaseInsensitive = true
            });


            return benefitResponse is null ? [] : new HashSet<string>(benefitResponse.Benefits.Select(b => b.Name), StringComparer.CurrentCultureIgnoreCase);

        }
        catch (AmazonBedrockRuntimeException e)
        {
            _logger.LogError($"ERROR: Can't invoke '{_modelId}'. Reason: {e.Message}");
            throw;
        }

    }

    private static string BuildPromptToExtractBenefitNames(string segment, HashSet<string>? identifiedBenefits = null)
    {
        identifiedBenefits ??= new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
        var previouslyIdentified = identifiedBenefits.Count > 0
                    ? "Previously identified benefits: " + string.Join(", ", identifiedBenefits)
                    : "No previously identified benefits";

        return $@"You are an AI assistant acting as a US health plan configuration QA tester.
                Objective: Your task is to interpret and extract each and every covered and non-covered medical benefits from a US health plan benefit summary, evidence of coverage, or summary of coverage content. 
                Extraction Requirements: 
                    Benefit Identification: Identify each and every mentioned medical benefit listed in the provided document. If the benefit services under grouped together, please create each benefit appending the group name.
                                            For example, if ""Inpatient Services"" is grouped under ""Mental Health & Substance Use Disorder"" please create a benefit as ""Inpatient Services for Mental Health & Substance Use Disorder"". 
                                            You will add context of the benefit. For example, if the radiology comes under ""Preventive Services"" and also under ""Inpatient Hospital Services"" then name the
                                            benefit as ""Radiology services under Preventive services"" and ""Radiology services under Inpatient Hospital Services"".
                    Separation of Combined Services: If medical services are combined in a single entry, split them into individual benefits. For example, if ""Routine Exam"" includes ""Immunizations, Vision and Hearing Exams, Lab tests,"" treat each event as a separate benefit.
                    Exclusions:Do not extract any dental and vision benefits.Output Format: Return json objects with benefits listed in alphabetical order of extracted information in the following JSON format, Only respond with a JSON array of such test cases. Do not include any explanation, commentary, or text outside the JSON. Ensure the output is valid JSON and nothing else.Do not include the code block:
                    ### Example Output Format ###
                    {{
                     ""benefits"" : [
                      {{
                        ""name"" : """"
                      }}
                     ]
                    }} 
                            Only return newly found benefits that have not been previously identified. 
                    {previouslyIdentified}
                    Text Segment:
                    --------------------------
                    {segment}
                    --------------------------  
                    Identify any new medical benefits in the above text that are not in the previously identified list.If no new benefits are found, return empty benefits array in the following JSON format,Only respond with a JSON array of such test cases. Do not include any explanation, commentary, or text outside the JSON. Ensure the output is valid JSON and nothing else. Do not include the code block:
                    ### Example Output Format ###
                    {{
                        ""benefits"" : []
                    }}";
    }

    private static string BuildPromptToExtractBenefitConditions(string benefit, string relevantSegment)
    {
        return $@"";
    }

    private static string BuildPromptToGenerateTestCase(string benefitPrompt)
    {
        return $@"";
    }


}
