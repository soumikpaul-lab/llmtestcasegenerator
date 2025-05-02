using System;

namespace Worker.Features.DocumentIntelligence;

public interface IDocumentAnalysisProcessor
{
    ValueTask BuildWorkItemAsync(BenefitDocMetadata benefitDocMetadata, CancellationToken cancellationToken);
}
