using System;

namespace Api.Features.DocumentIntelligence;

public interface IMongoDbRepository
{
    Task UploadBenefitDocMetaDataAsync(BenefitDocMetadata benefitDocMetadata, CancellationToken cancellationToken);

    Task<List<BenefitDocMetadata>> GetAllUploadedDocumetsAsync(CancellationToken cancellationToken);

    Task<Dictionary<string, List<GeneratedTestCase>>> GetGeneratedTestCasesAsync(string documentName, CancellationToken cancellationToken);

}
