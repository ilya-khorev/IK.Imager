# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

All projects are .NET 10 (except `IK.Imager.Api.Contract`, see below); the solution is `src/IK.Imager.sln`.

```powershell
dotnet build src\IK.Imager.sln --configuration Release   # what CI builds (.github/workflows/dotnetcore.yml)
dotnet test src\IK.Imager.sln                            # all tests (integration tests need Docker, see below)
dotnet test src\IK.Imager.sln --filter "Category!=Integration"           # everything that runs without Docker
dotnet test src\Tests\IK.Imager.Core.Tests\IK.Imager.Core.Tests.csproj   # unit tests only — no Docker needed
dotnet test src\Tests\IK.Imager.Core.Tests --filter "FullyQualifiedName~ThumbnailsGeneratingTests"
dotnet test src\Tests\IK.Imager.Core.Tests --filter "DisplayName~CreateThumbnails_ImageMetadataNotFound"
dotnet run --project src\IK.Imager.Api                   # http://localhost:5000, Swagger UI at the root path
```

`Scripts\AzureResources.ps1` provisions the Azure resources; `Scripts\DockerUpload.ps1` builds/pushes the Docker images.

### Test prerequisites

- `IK.Imager.Core.Tests` — xUnit + Moq + AutoFixture, fully in-memory (`Tests/IK.Imager.Core.Tests/Mocks/`). Sample images under `Images/` are copied to the output dir. **No Docker needed.**
- `IK.Imager.ImageBlobStorage.AzureFiles.Tests` and `IK.Imager.ImageMetadataStorage.CosmosDB.Tests` — **require a running Docker daemon**, and nothing else. [Testcontainers](https://dotnet.testcontainers.org/) starts [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) and the [Linux Cosmos DB emulator](https://learn.microsoft.com/azure/cosmos-db/emulator-linux) on randomly mapped host ports and tears them down afterwards; both classes are tagged `[Trait("Category", "Integration")]`.
  - Image tags are pinned in `Tests/IK.Imager.TestsBase/Constants.cs`, which now holds only names and tags — connection strings exist only at runtime, built from the mapped ports.
  - Each project starts **one** container per test assembly via an `ICollectionFixture` (`AzuriteCollection` / `CosmosDbCollection`); the fixtures are `IAsyncLifetime`. Neither drops its blob/Cosmos containers — the emulator itself is thrown away.
  - Azurite runs with `--skipApiVersionCheck` because `Azure.Storage.Blobs` sends a newer `x-ms-version` than Azurite recognises.
  - The Cosmos emulator serves plain HTTP in gateway mode and advertises its container-internal endpoint, so `CosmosDbClient` takes an **optional `CosmosClientOptions`** purely so the fixture can pass `ConnectionMode.Gateway` + `LimitToEndpoint` + the module's URI-rewriting `HttpClientFactory`. Production passes nothing and keeps the SDK defaults (hence the explicit factory registration in `Startup`). Do not set `Serializer` — `ImageMetadata.Id` relies on Newtonsoft's `[JsonProperty("id")]`.
  - The Cosmos emulator image is ~1 GB; the first run pulls it. Pre-pull with `docker pull` to keep it out of the Testcontainers startup timeout.

CI (`.github/workflows/dotnetcore.yml`) runs on `ubuntu-latest` and now builds **and** tests — the hosted Windows runners cannot run Linux containers.

Test naming convention (stated in `ImageAzureBlobRepositoryEmulatorTests`): `MethodUnderTest_Scenario_ExpectedBehavior`.

## Architecture

A single ASP.NET Core service (`IK.Imager.Api`) that both serves the HTTP API **and** consumes its own integration events off Azure Service Bus. An earlier `IK.Imager.BackgroundService` microservice was folded into the API — `Scripts/DockerUpload.ps1` and `docs/Architecture.svg` still refer to it.

### Request flow

Dispatch is hand-rolled rather than via a mediator library. `IK.Imager.Core.Abstractions/Messaging` defines `ICommandHandler<TCommand>`, `ICommandHandler<TCommand, TResult>`, `IQueryHandler<TQuery, TResult>`, and `IDomainEvent` / `IDomainEventHandler<T>` / `IDomainEventDispatcher`. Controller actions are thin: they inject the handler interface they need, call `Handle(...)`, and map the core model to the `IK.Imager.Api.Contract` model with the `ToContract()` extensions in `IK.Imager.Api/Mapping/ContractMappingExtensions.cs` (hand-written — there is no AutoMapper).

Upload → `UploadImageCommandHandler` (validate format/size → blob storage → metadata) → publishes the **domain** event `ImageUploadedDomainEvent` → `ImageUploadedDomainEventHandler` (in the Api project) republishes it as the **integration** event `OriginalImageUploadedIntegrationEvent` on Service Bus → `CreateThumbnailsHandler` consumes it and sends `CreateThumbnailsCommand`. Hence the ~2s delay before thumbnails appear in search results.

Delete → `DeleteImageMetadataCommandHandler` removes only the metadata (image disappears from search immediately) → `ImageMetadataDeletedDomainEvent` → `ImageMetadataDeletedIntegrationEvent` → `RemoveImageFilesHandler` → `DeleteImageCommand` deletes the original blob and thumbnail blobs.

This **domain event (`IDomainEvent`, dispatched in-process) → integration event (MassTransit) → command** relay is the core pattern. Domain events stay in `IK.Imager.Core`; the translation to Service Bus lives entirely in `IK.Imager.Api/DomainEventHandlers` and `IK.Imager.Api/IntegrationEvents`, so `IK.Imager.Core` has no messaging dependency. `DomainEventDispatcher` (in `IK.Imager.Core/Messaging`) just resolves every `IDomainEventHandler<T>` from the container and awaits them in turn; handlers are registered in `Startup.ConfigureServices`.

CDN rewriting is applied outside handlers, via decorators in `IK.Imager.Core/Cdn/CdnDecorators.cs` — a handler always returns the raw blob URL, and the decorator swaps in the CDN host when `Cdn:Uri` is configured. The decorators are wired in `RegisterCoreServices`: the concrete handler is registered by its own type, and the handler *interface* resolves to the decorator wrapping it. Add a decorator there rather than touching handlers when a new response needs URL rewriting.

### Project layout

`IK.Imager.Api` (host, controllers, validators, event translation) → `IK.Imager.Core` (all handlers, thumbnail resizing via ImageSharp, validation, CDN) → `IK.Imager.Core.Abstractions` / `IK.Imager.Storage.Abstractions` (interfaces + models, no dependencies). Storage implementations (`ImageBlobStorage.AzureFiles`, `ImageMetadataStorage.CosmosDB`) depend only on the storage abstractions and are bound in `Startup.ConfigureServices`. `IK.Imager.Api.Contract` is the only `netstandard2.1` project — it's the public DTO contract, kept separately so clients can reference it.

Core services register themselves through `IK.Imager.Core.ServiceCollectionExtensions.RegisterCoreServices`, which also binds the `Cdn`, `Thumbnails`, and `ImageLimitations` config sections. Add new core registrations there, not in `Startup`.

### Storage model

- **Blobs**: two Azure containers, originals (`AzureStorage:ImagesContainerName`) and thumbnails (`AzureStorage:ThumbnailsContainerName`), selected by `ImageSizeType`. Blob names are random GUID-derived (`ImageIdentifierProvider`) because blobs are publicly reachable by URL.
- **Metadata**: one Cosmos container partitioned on `/ImageGroup`. `ImageGroup` is the partition key the README calls "partition key" — it is required on upload and optional-but-recommended on search/delete. Thumbnails are stored as a nested list on the parent `ImageMetadata` document, so thumbnail generation is an upsert of the whole document.

### Validation is two-layered

FluentValidation validators in `IK.Imager.Api/Validations` check the *request shape* (URL well-formed, `ImageGroup` length 3–30, ≤200 image ids) and surface into Swagger. FluentValidation 12 dropped built-in ASP.NET Core auto-validation, so `IK.Imager.Api/Filters/FluentValidationActionFilter.cs` runs the validators over the action arguments and short-circuits with a 400 `ValidationProblemDetails`. `IK.Imager.Core.Validation.ImageValidator` checks the *image itself* (format/size/dimensions/aspect ratio) against the `ImageLimitations` config section. Note the handlers currently throw `ValidationException` on failure (there are `//todo`s about returning an error model instead); `GlobalExceptionFilter` maps it to a 400.

## Configuration

`src/IK.Imager.Api/appsettings.json` is the full parameter list. Environment variables override it with `__` as the section separator (`ServiceBus__ConnectionString`, `AzureStorage__ConnectionString`, `Logging__LogLevel__Default`). Defaults point at the local emulators, so the API starts against Azurite + Cosmos Emulator without changes — except `ServiceBus:ConnectionString`, which has no emulator and must be a real namespace or MassTransit startup fails.

Health endpoints: `/hc` (Cosmos + blob storage) and `/liveness` (self only).
