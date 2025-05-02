using System;
using Api.Infra;
using MongoDB.Driver;
using NPOI.SS.Formula.Functions;

namespace Api.Features.DocumentIntelligence;

public class MongoDbRepository : IMongoDbRepository
{

    private IMongoDatabase? _docIntellidatabase = null;
    private IMongoCollection<BenefitDocMetadata> _benefitDocMetadataCollection;
    private IMongoCollection<TestCaseStore> _testCaseStoreCollection;
    public MongoDbRepository(IMongoDbClientProvider mongoDbClientProvider)
    {
        _docIntellidatabase = mongoDbClientProvider.GetMongoDatabase("DocumentIntelligence");
        _benefitDocMetadataCollection = _docIntellidatabase.GetCollection<BenefitDocMetadata>("BenefitDocMetaData");
        _testCaseStoreCollection = _docIntellidatabase.GetCollection<TestCaseStore>("TestCaseStore");
    }

    public async Task<Dictionary<string, List<GeneratedTestCase>>> GetGeneratedTestCasesAsync(string documentName, CancellationToken cancellationToken)
    {
        var filter = Builders<TestCaseStore>.Filter.Eq(doc => doc.DocumentName, documentName);
        var testCaseStoreCursor = await _testCaseStoreCollection.FindAsync(filter, null, cancellationToken).ConfigureAwait(false);
        return (await testCaseStoreCursor.FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false)).GeneratedTestCases;
    }

    public async Task<List<BenefitDocMetadata>> GetAllUploadedDocumetsAsync(CancellationToken cancellationToken)
    {
        var docs = await _benefitDocMetadataCollection
                        .Find(Builders<BenefitDocMetadata>.Filter.Empty)
                        .ToListAsync(cancellationToken)
                        .ConfigureAwait(false);

        return docs ?? new List<BenefitDocMetadata>();

    }

    public async Task UploadBenefitDocMetaDataAsync(BenefitDocMetadata benefitDocMetadata, CancellationToken cancellationToken)
    {
        await _benefitDocMetadataCollection.InsertOneAsync(benefitDocMetadata, new InsertOneOptions
        {
            Comment = "Uploaded for text extraction and test case generation",
            BypassDocumentValidation = true
        }, cancellationToken);
    }
}
