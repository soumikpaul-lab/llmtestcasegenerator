using System;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using MongoDB.Driver;
using SharpCompress.Archives;
using Worker.Infra;

namespace Worker.Features.DocumentIntelligence;

public class DocIntelligenceRepository : IDocIntelligenceRepository
{

    private IMongoCollection<BenefitDocMetadata> _benefitDocMetaDataCollection;
    private IMongoCollection<BenefitsStore> _benefitStoreCollection;

    private IMongoCollection<TestCaseStore> _testCaseStoreCollection;
    private IMongoDatabase _database;
    private ILogger<DocIntelligenceRepository> _logger;
    public DocIntelligenceRepository(IMongoDbClientProvider mongoDbClientProvider, ILogger<DocIntelligenceRepository> logger)
    {
        _database = mongoDbClientProvider.GetMongoDatabase("DocumentIntelligence");
        _benefitDocMetaDataCollection = _database.GetCollection<BenefitDocMetadata>("BenefitDocMetaData");
        _benefitStoreCollection = _database.GetCollection<BenefitsStore>("BenefitStore");
        _testCaseStoreCollection = _database.GetCollection<TestCaseStore>("TestCaseStore");
        _logger = logger;
    }

    public async Task<BenefitDocMetadata> GetBenefitDocMetadataAsync(DocumentProcessStatus documentProcessStatus, CancellationToken cancellationToken)
    {
        var filter = Builders<BenefitDocMetadata>.Filter.Eq(doc => doc.DocumentProcessStatus, documentProcessStatus);

        var sort = Builders<BenefitDocMetadata>.Sort.Ascending(doc => doc.UploadDateTime);

        return await _benefitDocMetaDataCollection.Find(filter).Sort(sort).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> UpdateBenefitDocMetadataAsync(BenefitDocMetadata benefitDocMetadata, CancellationToken cancellationToken)
    {
        var filter = Builders<BenefitDocMetadata>.Filter.Eq(doc => doc.DocumentName, benefitDocMetadata.DocumentName);

        var update = Builders<BenefitDocMetadata>.Update.Set(doc => doc.DocumentProcessStatus, benefitDocMetadata.DocumentProcessStatus);

        var result = await _benefitDocMetaDataCollection.UpdateOneAsync(filter, update, new UpdateOptions
        {
            BypassDocumentValidation = true
        }, cancellationToken);

        return result.ModifiedCount > 0;
    }

    public async Task InsertBenefitsAsync(BenefitsStore benefitsStore, CancellationToken token)
    {
        var filter = Builders<BenefitsStore>.Filter.Eq(doc => doc.DocumentName, benefitsStore.DocumentName);
        var options = new FindOneAndDeleteOptions<BenefitsStore, BenefitsStore>
        {
            Projection = Builders<BenefitsStore>.Projection
            .Include(doc => doc.DocumentName)
            ,
            Comment = $"Deleting the document {benefitsStore.DocumentName}"
        };
        var deleteDoc = await _benefitStoreCollection.FindOneAndDeleteAsync(filter, options, token);
        if (deleteDoc is not null)
        {
            _logger.LogInformation("Deleted benefit store with document - {documentName}", deleteDoc.DocumentName);
        }
        await _benefitStoreCollection.InsertOneAsync(benefitsStore, new InsertOneOptions
        {
            BypassDocumentValidation = true,
            Comment = $"Benefit store for document {benefitsStore.DocumentName} is inserted"
        }, token);

    }

    public async Task InsertTestCaseStoreAsync(TestCaseStore testCaseStore, CancellationToken cancellationToken)
    {
        var filter = Builders<TestCaseStore>.Filter.Eq(tcs => tcs.DocumentName, testCaseStore.DocumentName);
        var options = new FindOneAndDeleteOptions<TestCaseStore, TestCaseStore>
        {
            Projection = Builders<TestCaseStore>.Projection
            .Include(doc => doc.DocumentName)
            ,
            Comment = $"Deleting the document {testCaseStore.DocumentName}"
        };
        var deleteDoc = await _testCaseStoreCollection.FindOneAndDeleteAsync(filter, options, cancellationToken);
        if (deleteDoc is not null)
        {
            _logger.LogInformation("Deleted test case store with document - {documentName}", deleteDoc.DocumentName);
        }

        await _testCaseStoreCollection.InsertOneAsync(testCaseStore, new InsertOneOptions
        {
            BypassDocumentValidation = true,
            Comment = $"Test case store for document {testCaseStore.DocumentName} is inserted"
        }, cancellationToken);
    }
}
