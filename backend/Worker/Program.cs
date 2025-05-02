using System.Reflection.Metadata.Ecma335;
using Amazon.BedrockRuntime;
using Worker.Features.DocumentIntelligence;
using Worker.Infra;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker.Worker>();
builder.Services.AddAWSService<IAmazonBedrockRuntime>();
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));
builder.Services.AddSingleton<IMongoDbClientProvider, MongoDbClientProvider>();
builder.Services.AddTransient<IDocIntelligenceRepository, DocIntelligenceRepository>();
builder.Services.AddTransient<IBedrockService, BedrockService>();
builder.Services.AddTransient<IDocumentAnalysisProcessor, DocumentAnalysisProcessor>();
builder.Services.AddTransient<IDocumentTextract, DocumentTextract>();
builder.Services.AddSingleton<IBackgroundTaskQueue>(_ => new BackgroundTaskQueue(100));
builder.Services.AddSingleton<DocIntelligenceMonitorLoop>();
builder.Services.AddHostedService<DocIntelligenceHostedService>();
var host = builder.Build();


var monitorLoop = host.Services.GetRequiredService<DocIntelligenceMonitorLoop>();
monitorLoop.MonitorLoop();
await host.RunAsync();


