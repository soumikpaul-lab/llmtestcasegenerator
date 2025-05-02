using Api.Infra;
using Api.Features.DocumentIntelligence;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddCarter();

builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDbSettings"));

builder.Services.AddSingleton<IMongoDbClientProvider, MongoDbClientProvider>();

builder.Services.AddTransient<IMongoDbRepository, MongoDbRepository>();

//builder.Services.AddAutoMapper(typeof(TestCasesToExportProfile).Assembly);
builder.Services.AddAutoMapper(typeof(TestCasesToExportProfile).Assembly);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//app.UseHttpsRedirection();

app.MapCarter();

app.Run();

