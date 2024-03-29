using System.Reflection;
using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Extensions.Configuration.SystemsManager;
using Amazon.Extensions.NETCore.Setup;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Api.Extensions;
using Api.Infrastructure.Context;
using Api.Infrastructure.Middleware;
using Domain.Options;
using Domain.Repositories;
using Domain.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Configuration.AddSystemsManager(config =>
{
    config.Path = "/media-api";
    config.ParameterProcessor = new JsonParameterProcessor();
    config.ReloadAfter = TimeSpan.FromMinutes(5);
    config.Optional = true;
});

builder.Services.AddHttpContextAccessor();
builder.Services.Configure<UploadSettings>(builder.Configuration.GetSection("UploadSettings"));
builder.Services.Configure<EventBusSettings>(builder.Configuration.GetSection("EventBusSettings"));
builder.Services.Configure<ApiKeyValidationSettings>(builder.Configuration.GetSection("ApiKeyValidationSettings"));

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Logging.ClearProviders();
// Serilog configuration        
var logger = new LoggerConfiguration()
    .Enrich.WithProperty("Application", "Media")
    .WriteTo.Console(new JsonFormatter())
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("AWSSDK", LogEventLevel.Warning)
    .MinimumLevel.Override("System.", LogEventLevel.Warning)
    .CreateLogger();


builder.Services.AddScoped<IApiContext, ApiContext>();
builder.Services.AddScoped<IGalleryRepository, GalleryRepository>();
builder.Services.AddScoped<IEventBusManager, EventBusManager>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<ApiKeyValidatorMiddleware>();

builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddAWSService<IAmazonDynamoDB>();
builder.Services.AddAWSService<IAmazonSimpleNotificationService>();
builder.Services.AddAWSLambdaHosting(Environment.GetEnvironmentVariable("ApiGatewayType") == "RestApi" ? LambdaEventSource.RestApi : LambdaEventSource.HttpApi);
var option = builder.Configuration.GetAWSOptions();
builder.Services.AddDefaultAWSOptions(option);


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(exceptionHandlerApp => exceptionHandlerApp.Run(async context => await Results.Problem().ExecuteAsync(context)));
}
app.UseMiddleware<ApiKeyValidatorMiddleware>();
app.UseHttpsRedirection();
app.MapEndpointsCore(AppDomain.CurrentDomain.GetAssemblies());

app.Run();
