# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

All projects are .NET 6; the solution is `src/IK.Imager.sln`.

```powershell
dotnet build src\IK.Imager.sln --configuration Release   # what CI runs (.github/workflows/dotnetcore.yml)
dotnet test src\IK.Imager.sln                            # all tests (integration tests need emulators, see below)
dotnet test src\Tests\IK.Imager.Core.Tests\IK.Imager.Core.Tests.csproj   # unit tests only — no emulators needed
dotnet test src\Tests\IK.Imager.Core.Tests --filter "FullyQualifiedName~ThumbnailsGeneratingTests"
dotnet test src\Tests\IK.Imager.Core.Tests --filter "DisplayName~CreateThumbnails_ImageMetadataNotFound"
dotnet run --project src\IK.Imager.Api                   # http://localhost:5000, Swagger UI at the root path
```

`Scripts\AzureResources.ps1` provisions the Azure resources; `Scripts\DockerUpload.ps1` builds/pushes the Docker images.

### Test prerequisites

- `IK.Imager.Core.Tests` — xUnit + Moq + AutoFixture, fully in-memory (`Tests/IK.Imager.Core.Tests/Mocks/`). Sample images under `Images/` are copied to the output dir.
- `IK.Imager.ImageBlobStorage.AzureFiles.Tests` — requires the **Azure Storage Emulator / Azurite** on the default endpoint.
- `IK.Imager.ImageMetadataStorage.CosmosDB.Tests` — requires the **Cosmos DB Emulator** at `https://localhost:8081`.
  Both use the well-known emulator credentials hardcoded in `Tests/IK.Imager.TestsBase/Constants.cs`, create their own `Test*` containers via an `IClassFixture`, and drop them on dispose.

Test naming convention (stated in `ImageAzureBlobRepositoryEmulatorTests`): `MethodUnderTest_Scenario_ExpectedBehavior`.

## Architecture

A single ASP.NET Core service (`IK.Imager.Api`) that both serves the HTTP API **and** consumes its own integration events off Azure Service Bus. An earlier `IK.Imager.BackgroundService` microservice was folded into the API — `Scripts/DockerUpload.ps1` and `docs/Architecture.svg` still refer to it.

### Request flow

Everything goes through MediatR. Controller actions are thin: they build a command/query, `_mediator.Send(...)`, and AutoMapper-map the core model to the `IK.Imager.Api.Contract` model.

Upload → `UploadImageCommandHandler` (validate format/size → blob storage → metadata) → publishes the **domain** event `ImageUploadedDomainEvent` → `ImageUploadedDomainEventHandler` (in the Api project) republishes it as the **integration** event `OriginalImageUploadedIntegrationEvent` on Service Bus → `CreateThumbnailsHandler` consumes it and sends `CreateThumbnailsCommand`. Hence the ~2s delay before thumbnails appear in search results.

Delete → `DeleteImageMetadataCommandHandler` removes only the metadata (image disappears from search immediately) → `ImageMetadataDeletedDomainEvent` → `ImageMetadataDeletedIntegrationEvent` → `RemoveImageFilesHandler` → `DeleteImageCommand` deletes the original blob and thumbnail blobs.

This **domain event (MediatR `INotification`) → integration event (MassTransit) → command** relay is the core pattern. Domain events stay in `IK.Imager.Core`; the translation to Service Bus lives entirely in `IK.Imager.Api/DomainEventHandlers` and `IK.Imager.Api/IntegrationEvents`, so `IK.Imager.Core` has no messaging dependency.

CDN rewriting is applied outside handlers, via MediatR `IRequestPostProcessor` implementations in `IK.Imager.Core/Cdn/CdnPostProcessor.cs` — a handler always returns the raw blob URL, and the post-processor swaps in the CDN host when `Cdn:Uri` is configured. Add a post-processor there rather than touching handlers when a new response needs URL rewriting.

### Project layout

`IK.Imager.Api` (host, controllers, validators, event translation) → `IK.Imager.Core` (all handlers, thumbnail resizing via ImageSharp, validation, CDN) → `IK.Imager.Core.Abstractions` / `IK.Imager.Storage.Abstractions` (interfaces + models, no dependencies). Storage implementations (`ImageBlobStorage.AzureFiles`, `ImageMetadataStorage.CosmosDB`) depend only on the storage abstractions and are bound in `Startup.ConfigureServices`. `IK.Imager.Api.Contract` is the only `netstandard2.1` project — it's the public DTO contract, kept separately so clients can reference it.

Core services register themselves through `IK.Imager.Core.ServiceCollectionExtensions.RegisterCoreServices`, which also binds the `Cdn`, `Thumbnails`, and `ImageLimitations` config sections. Add new core registrations there, not in `Startup`.

### Storage model

- **Blobs**: two Azure containers, originals (`AzureStorage:ImagesContainerName`) and thumbnails (`AzureStorage:ThumbnailsContainerName`), selected by `ImageSizeType`. Blob names are random GUID-derived (`ImageIdentifierProvider`) because blobs are publicly reachable by URL.
- **Metadata**: one Cosmos container partitioned on `/ImageGroup`. `ImageGroup` is the partition key the README calls "partition key" — it is required on upload and optional-but-recommended on search/delete. Thumbnails are stored as a nested list on the parent `ImageMetadata` document, so thumbnail generation is an upsert of the whole document.

### Validation is two-layered

FluentValidation validators in `IK.Imager.Api/Validations` check the *request shape* (URL well-formed, `ImageGroup` length 3–30, ≤200 image ids) and surface into Swagger. `IK.Imager.Core.Validation.ImageValidator` checks the *image itself* (format/size/dimensions/aspect ratio) against the `ImageLimitations` config section. Note the handlers currently throw `ValidationException` on failure (there are `//todo`s about returning an error model instead); `GlobalExceptionFilter` maps it to a 400.

## Configuration

`src/IK.Imager.Api/appsettings.json` is the full parameter list. Environment variables override it with `__` as the section separator (`ServiceBus__ConnectionString`, `AzureStorage__ConnectionString`, `Logging__LogLevel__Default`). Defaults point at the local emulators, so the API starts against Azurite + Cosmos Emulator without changes — except `ServiceBus:ConnectionString`, which has no emulator and must be a real namespace or MassTransit startup fails.

Health endpoints: `/hc` (Cosmos + blob storage) and `/liveness` (self only).
