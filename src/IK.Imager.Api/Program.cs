using System;
using System.IO;
using IK.Imager.Api.Extensions;
using IK.Imager.Core;
using IK.Imager.ImageBlobStorage.AzureFiles;
using IK.Imager.ImageMetadataStorage.CosmosDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;

#pragma warning disable 1591

var builder = WebApplication.CreateBuilder(args);

//CreateBuilder already registers appsettings.json, but as optional - and this service cannot start
//without it. Re-adding it with optional:false would append it *after* the environment variable
//provider, which then silently loses every ServiceBus__/AzureStorage__ override, so assert on the
//file instead of touching the provider chain.
var appSettingsPath = Path.Combine(builder.Environment.ContentRootPath, "appsettings.json");
if (!File.Exists(appSettingsPath))
    throw new FileNotFoundException($"{appSettingsPath} is required to start this service.", appSettingsPath);

builder.Logging.AddImagerLogging();

//ValidateScopes is development-only by default; with no tests over the container this is the
//cheapest way to catch a captive dependency or a dropped registration at startup instead of at runtime
builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

//Every module registers its own services - see the ServiceCollectionExtensions of each project
builder.Services
    .AddApiServices()
    .AddSwaggerDocumentation()
    .AddImagerCore(builder.Configuration, httpClient => httpClient.AddTransientHttpErrorPolicy(p =>
        p.WaitAndRetryAsync(3, _ => TimeSpan.FromMilliseconds(500))))
    .AddAzureImageBlobStorage(builder.Configuration)
    .AddCosmosImageMetadataStorage(builder.Configuration)
    .AddIntegrationEventMessaging(builder.Configuration)
    .AddObservability(builder.Configuration);

var app = builder.Build();

app.UseImagerPipeline();

await app.RunAsync();
