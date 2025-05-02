using System;

namespace Worker.Features.DocumentIntelligence;

public class DocumentAnalysisProcessor(IDocIntelligenceRepository docIntelligenceRepository, ILogger<DocumentAnalysisProcessor> logger, IBedrockService bedrockService, IDocumentTextract documentTextract) : IDocumentAnalysisProcessor
{


    public async ValueTask BuildWorkItemAsync(BenefitDocMetadata benefitDocMetadata, CancellationToken cancellationToken)
    {
        try
        {
            benefitDocMetadata.DocumentProcessStatus = DocumentProcessStatus.Processing;
            var result = await docIntelligenceRepository.UpdateBenefitDocMetadataAsync(benefitDocMetadata, cancellationToken);
            if (!result)
            {
                logger.LogWarning($"Document {benefitDocMetadata.DocumentName} is already processing. Movint to next document.");
            }

            var benefitDocContent = await documentTextract.ExtractText(benefitDocMetadata.DocumentName, cancellationToken);

            if (benefitDocContent is null)
            {
                logger.LogError($"Document {benefitDocMetadata.DocumentName} analysis failed");
                benefitDocMetadata.DocumentProcessStatus = DocumentProcessStatus.Failed;
                await docIntelligenceRepository.UpdateBenefitDocMetadataAsync(benefitDocMetadata, cancellationToken);
                return;

            }
            var identifiedBenefits = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
            identifiedBenefits.UnionWith(await bedrockService.GetBenefitNames(benefitDocContent.ToString(), cancellationToken, null));

            if (identifiedBenefits.Count == 0)
            {
                logger.LogError($"Document {benefitDocMetadata.DocumentName} is not a benefit document");
                benefitDocMetadata.DocumentProcessStatus = DocumentProcessStatus.Failed;
                await docIntelligenceRepository.UpdateBenefitDocMetadataAsync(benefitDocMetadata, cancellationToken);

            }

            var benefitsWithConditions = new List<ExtractedBenefitResponse>();
            foreach (var benefit in identifiedBenefits)
            {
                logger.LogInformation($"Extracted benefit - {benefit}");
                var extractedBenefitWithConditions = await bedrockService.GetBenefitWithConditions(benefit, benefitDocContent.ToString(), cancellationToken);
                if (extractedBenefitWithConditions is not null)
                {
                    benefitsWithConditions.Add(extractedBenefitWithConditions);
                }
            }

            //Insert the extracted benefits to benefit store.
            await docIntelligenceRepository.InsertBenefitsAsync(new BenefitsStore
            {
                DocumentName = benefitDocMetadata.DocumentName,
                ExtractedBenefitResponses = benefitsWithConditions
            }, cancellationToken);

            var testCaseStore = new TestCaseStore
            {
                DocumentName = benefitDocMetadata.DocumentName
            };

            foreach (var benefitWithCondition in benefitsWithConditions)
            {
                var generatedTestCases = await bedrockService.GenerateTestCases(benefitWithCondition.ToString(), cancellationToken);
                if (generatedTestCases.Count > 0)
                {
                    if (testCaseStore.GeneratedTestCases.TryGetValue(benefitWithCondition.ToString(), out var existingList))
                    {
                        existingList.AddRange(generatedTestCases);
                    }
                    else
                    {
                        testCaseStore.GeneratedTestCases[benefitWithCondition.ToString()] = generatedTestCases;
                    }

                }
            }

            await docIntelligenceRepository.InsertTestCaseStoreAsync(testCaseStore, cancellationToken);

            benefitDocMetadata.DocumentProcessStatus = DocumentProcessStatus.Success;
            await docIntelligenceRepository.UpdateBenefitDocMetadataAsync(benefitDocMetadata, cancellationToken);
            logger.LogInformation("Successfully generated test cases for document - {documentName}", benefitDocMetadata.DocumentName);

        }
        catch (Exception e)
        {
            logger.LogError($"Error processing document {benefitDocMetadata.DocumentName} with error - {e.Message}");
            benefitDocMetadata.DocumentProcessStatus = DocumentProcessStatus.Failed;
            await docIntelligenceRepository.UpdateBenefitDocMetadataAsync(benefitDocMetadata, cancellationToken);
        }
    }


}
