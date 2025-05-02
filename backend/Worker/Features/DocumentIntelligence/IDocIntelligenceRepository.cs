using System;

namespace Worker.Features.DocumentIntelligence;

public interface IDocIntelligenceRepository
{
    Task<BenefitDocMetadata> GetBenefitDocMetadataAsync(DocumentProcessStatus documentProcessStatus, CancellationToken cancellationToken);

    Task<bool> UpdateBenefitDocMetadataAsync(BenefitDocMetadata benefitDocMetadata, CancellationToken cancellationToken);

    Task InsertBenefitsAsync(BenefitsStore benefitsStore, CancellationToken token);
    Task InsertTestCaseStoreAsync(TestCaseStore testCaseStore, CancellationToken cancellationToken);

}
