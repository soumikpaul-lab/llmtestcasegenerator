using System;

namespace Worker.Features.DocumentIntelligence;

public interface IBedrockService
{
    Task<HashSet<string>> GetBenefitNames(string segment, CancellationToken token, HashSet<string>? identifiedBenefits = null);
    Task<ExtractedBenefitResponse> GetBenefitWithConditions(string benefitName, string relevantSegment, CancellationToken token);

    Task<List<GeneratedTestCase>> GenerateTestCases(string benefitPrompt, CancellationToken cancellationToken);

}
