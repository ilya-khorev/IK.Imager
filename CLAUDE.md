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

### Build configuration

Shared MSBuild settings live outside the individual `.csproj` files, so most of them now contain nothing but references.

- `src/Directory.Build.props` — imported by every project under `src/`. Sets `TargetFramework` (`net10.0`), `LangVersion` (`latest`), `Nullable` (`enable`), transitive `NuGetAudit`, and `ContinuousIntegrationBuild` when `CI=true`. A property set in a `.csproj` still wins, which is how `IK.Imager.Api.Contract` keeps `netstandard2.1`.
- `src/Directory.Packages.props` — Central Package Management. `PackageReference` items carry **no `Version`**; add or bump a version here instead. This is what keeps `Azure.Storage.Blobs` / `Microsoft.Azure.Cosmos` identical between a production project and the test project that drives its emulator.
- `.editorconfig` (repo root) — codifies the existing style (4-space indent, file-scoped namespaces, `_camelCase` private fields, `var` everywhere). All style rules are `suggestion`; `EnforceCodeStyleInBuild` is deliberately **not** set, so none of them can fail a build. Apply with `dotnet format src\IK.Imager.sln`.
- `IK.Imager.sln.DotSettings` is still required alongside it: it holds the ReSharper/Rider naming-abbreviation list (`JPEG`, `WEBP`, …) and the spell-check user dictionary, neither of which has a standard `.editorconfig` equivalent.
- `global.json` pins the SDK to 10.0.x (`rollForward: latestFeature`); `nuget.config` clears inherited feeds so restore only ever sees nuget.org.

Nullable reference types are on everywhere. Config-bound options classes and models populated by deserialization use `= null!` rather than `required`, which keeps today's behaviour (a missing value fails at first use) and keeps `IK.Imager.Api.Contract` usable from `netstandard2.1`, where `required` does not exist.

### Test prerequisites

- `IK.Imager.Core.Tests` — xUnit + Moq + AutoFixture, fully in-memory (`Tests/IK.Imager.Core.Tests/Mocks/`). Sample images under `Images/` are copied to the output dir. **No Docker needed.**
- `IK.Imager.ImageBlobStorage.AzureFiles.Tests` and `IK.Imager.ImageMetadataStorage.CosmosDB.Tests` — **require a running Docker daemon**, and nothing else. [Testcontainers](https://dotnet.testcontainers.org/) starts [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) and the [Linux Cosmos DB emulator](https://learn.microsoft.com/azure/cosmos-db/emulator-linux) on randomly mapped host ports and tears them down afterwards; both classes are tagged `[Trait("Category", "Integration")]`.
  - Image tags are pinned in `Tests/IK.Imager.TestsBase/Constants.cs`, which now holds only names and tags — connection strings exist only at runtime, built from the mapped ports.
  - Both test projects reference the storage SDK directly (`Azure.Storage.Blobs` / `Microsoft.Azure.Cosmos`) for their fixtures; Central Package Management keeps those at the same version as the production project automatically.
  - Each project starts **one** container per test assembly via an `ICollectionFixture` (`AzuriteCollection` / `CosmosDbCollection`); the fixtures are `IAsyncLifetime`. Neither drops its blob/Cosmos containers — the emulator itself is thrown away.
  - Azurite runs with `--skipApiVersionCheck` because `Azure.Storage.Blobs` sends a newer `x-ms-version` than Azurite recognises.
  - The Cosmos emulator serves plain HTTP in gateway mode and advertises its container-internal endpoint, so `CosmosDbClient` takes an **optional `CosmosClientOptions`** purely so the fixture can pass `ConnectionMode.Gateway` + `LimitToEndpoint` + the module's URI-rewriting `HttpClientFactory`. Production passes nothing and keeps the SDK defaults (hence the explicit factory registration in `Program.cs`). Do not set `Serializer` — `ImageMetadata.Id` relies on Newtonsoft's `[JsonProperty("id")]`.
  - The Cosmos emulator image is ~1 GB; the first run pulls it. Pre-pull with `docker pull` to keep it out of the Testcontainers startup timeout.

CI (`.github/workflows/dotnetcore.yml`) runs on `ubuntu-latest` and now builds **and** tests — the hosted Windows runners cannot run Linux containers.

Test naming convention (stated in `ImageAzureBlobRepositoryEmulatorTests`): `MethodUnderTest_Scenario_ExpectedBehavior`.

## Architecture

A single ASP.NET Core service (`IK.Imager.Api`) that both serves the HTTP API **and** consumes its own integration events off Azure Service Bus. An earlier `IK.Imager.BackgroundService` microservice was folded into the API — `Scripts/DockerUpload.ps1` and `docs/Architecture.svg` still refer to it.

### Request flow

Dispatch is hand-rolled rather than via a mediator library. `IK.Imager.Core.Abstractions/Messaging` defines `ICommandHandler<TCommand>`, `ICommandHandler<TCommand, TResult>`, `IQueryHandler<TQuery, TResult>`, and `IDomainEvent` / `IDomainEventHandler<T>` / `IDomainEventDispatcher`. Endpoint handlers are thin: they take the handler interface they need as a parameter (resolved from DI), call `Handle(...)`, and map the core model to the `IK.Imager.Api.Contract` model with the `ToContract()` extensions in `IK.Imager.Api/Mapping/ContractMappingExtensions.cs` (hand-written — there is no AutoMapper).

### Endpoints are minimal APIs, grouped by feature

There are no controllers and no MVC services — `IK.Imager.Api/Features` holds one folder per feature (`ImageUpload`, `ImageLookup`, `ImageDeleting`), and a feature owns its routes, its request models and its FluentValidation validators. Each folder exposes a `Map…Endpoints(this IEndpointRouteBuilder)` extension; `Features/ImagerEndpoints.cs` creates the `/Images` group (tagged `Images`, which is what keeps the Swagger UI grouping the controller used to give) and calls them. Add an endpoint to the feature it belongs to, never to the aggregator.

Route paths are unchanged from the controller era: `POST /Images/Upload`, `POST /Images/UploadByUrl`, `POST /Images/Search`, `DELETE /Images`.

Handlers are named `internal static` methods rather than lambdas: the source generator in `Microsoft.AspNetCore.OpenApi` reads their XML documentation, so `<summary>`, `<param>` and `<response code="…">` land in the document exactly as the controller attributes used to. Return `TypedResults` (`Ok<T>`, `Results<NoContent, NotFound<string>>`) so the response types are inferred rather than declared twice.

Two things minimal APIs need that MVC did implicitly:
- `POST /Images/Upload` calls `.DisableAntiforgery()`. Form endpoints require an antiforgery token by default, which only makes sense for a cookie-authenticated browser form; `[ApiController]` never enforced it.
- `DELETE /Images` marks its request model `[FromBody]`. DELETE is one of the methods minimal APIs refuse to infer a body for, and the failure is a startup-time `InvalidOperationException` on first request rather than a compile error.

Upload → `UploadImageCommandHandler` (validate format/size → blob storage → metadata) → publishes the **domain** event `ImageUploadedDomainEvent` → `ImageUploadedDomainEventHandler` (in the Api project) republishes it as the **integration** event `OriginalImageUploadedIntegrationEvent` on Service Bus → `CreateThumbnailsHandler` consumes it and sends `CreateThumbnailsCommand`. Hence the ~2s delay before thumbnails appear in search results.

Delete → `DeleteImageMetadataCommandHandler` removes only the metadata (image disappears from search immediately) → `ImageMetadataDeletedDomainEvent` → `ImageMetadataDeletedIntegrationEvent` → `RemoveImageFilesHandler` → `DeleteImageCommand` deletes the original blob and thumbnail blobs.

This **domain event (`IDomainEvent`, dispatched in-process) → integration event (MassTransit) → command** relay is the core pattern. Domain events stay in `IK.Imager.Core`; the translation to Service Bus lives entirely in `IK.Imager.Api/DomainEventHandlers` and `IK.Imager.Api/IntegrationEvents`, so `IK.Imager.Core` has no messaging dependency. `DomainEventDispatcher` (in `IK.Imager.Core/Messaging`) just resolves every `IDomainEventHandler<T>` from the container and awaits them in turn; handlers are registered by `AddIntegrationEventMessaging` (`IK.Imager.Api/Extensions/MessagingServiceCollectionExtensions.cs`), alongside the bus they publish onto.

CDN rewriting is applied outside handlers, via decorators in `IK.Imager.Core/Cdn/CdnDecorators.cs` — a handler always returns the raw blob URL, and the decorator swaps in the CDN host when `Cdn:Uri` is configured. The decorators are wired in `AddImagerCore`: the concrete handler is registered by its own type, and the handler *interface* resolves to the decorator wrapping it. Add a decorator there rather than touching handlers when a new response needs URL rewriting.

### Project layout

`IK.Imager.Api` (host, feature endpoints, validators, event translation) → `IK.Imager.Core` (all handlers, thumbnail resizing via ImageSharp, validation, CDN) → `IK.Imager.Core.Abstractions` / `IK.Imager.Storage.Abstractions` (interfaces + models, no dependencies). Storage implementations (`ImageBlobStorage.AzureFiles`, `ImageMetadataStorage.CosmosDB`) depend only on the storage abstractions. `IK.Imager.Api.Contract` is the only `netstandard2.1` project — it's the public DTO contract, kept separately so clients can reference it.

### DI registration

**Every module registers its own services** through a `ServiceCollectionExtensions` class in its own project. Add a new registration to the module it belongs to, never to `Program.cs`.

| Method | Project | Binds |
|---|---|---|
| `AddImagerCore(IConfiguration, Action<IHttpClientBuilder>?)` | `IK.Imager.Core` | `Cdn`, `Thumbnails`, `ImageLimitations` |
| `AddAzureImageBlobStorage(IConfiguration)` | `IK.Imager.ImageBlobStorage.AzureFiles` | `AzureStorage` |
| `AddCosmosImageMetadataStorage(IConfiguration)` | `IK.Imager.ImageMetadataStorage.CosmosDB` | `CosmosDb` |
| `AddApiServices()` / `AddOpenApiDocumentation()` / `AddIntegrationEventMessaging(IConfiguration)` / `AddObservability(IConfiguration)` | `IK.Imager.Api/Extensions` | `Topics` |

The convention: each method takes the **configuration root** and owns its section name as a `public const SectionName` next to the settings class it binds — so the magic string never appears at the call site. All of them return `IServiceCollection` and chain.

`AddImagerCore` takes an optional `Action<IHttpClientBuilder>` for the `ImageDownloadClient` typed client: Core owns *what* the client is, the host owns HTTP resilience, so `Microsoft.Extensions.Http.Polly` stays out of Core.

Health checks stay in the host (`Extensions/ObservabilityExtensions.cs`) rather than moving into the storage modules — `AspNetCore.HealthChecks.*` is versioned independently and which endpoints get probed is an operational decision. They read the storage connection settings through `IOptions<T>` so a probe can never target a different database or container than the repositories do.

`AddApiServices()` is what the endpoints need rather than an MVC layer: the `GlobalExceptionHandler`, `AddProblemDetails()`, and every validator in the assembly. `UseImagerPipeline()` (`Extensions/WebApplicationExtensions.cs`) puts `UseExceptionHandler()` outermost and then maps the OpenAPI document, the Service Fabric middleware, `MapImagerEndpoints()` and the health endpoints.

The host uses minimal hosting: `IK.Imager.Api/Program.cs` is a top-level-statements file that only wires configuration and logging, then calls the module extensions and `UseImagerPipeline()`. There is no `Startup` class. It turns on `ValidateScopes` + `ValidateOnBuild` in every environment — nothing tests the container, so a captive dependency or a dropped registration should fail at `builder.Build()` rather than on the first request.

### API documentation

The OpenAPI document is generated by **ASP.NET Core itself** (`Microsoft.AspNetCore.OpenApi`), served at `/openapi/v1.json` as OpenAPI 3.1. Swashbuckle is reduced to `Swashbuckle.AspNetCore.SwaggerUI` — the UI only, hosted at the root path; there is no `SwaggerGen` and no `UseSwagger()`. Both halves are wired in `IK.Imager.Api/Extensions/OpenApiServiceCollectionExtensions.cs`.

Endpoint and schema descriptions come from the XML documentation of `IK.Imager.Api` and `IK.Imager.Api.Contract`, which the source generator inside `Microsoft.AspNetCore.OpenApi` bakes into the assembly at compile time. `GenerateDocumentationFile` must stay on in both projects — the generator is what carries the endpoint summaries and `<response>` codes, and nothing reads the `.xml` files at runtime.

`IK.Imager.Api/OpenApi` replaces MicroElements.Swashbuckle.FluentValidation, which only plugs into Swashbuckle's schema generator: `FluentValidationRules` maps a property validator onto a schema (`NotEmpty`/`NotNull` → `required` plus `minLength`/`minItems`, `Length` → `minLength`/`maxLength`, `Matches` → `pattern`). `FluentValidationSchemaTransformer` applies it to every request model. Rules expressed as `Must(...)` predicates are invisible to it, exactly as they were to MicroElements.

The `[FromForm]` model of `POST /Images/Upload` needs nothing special. A minimal API endpoint keeps it a type all the way into the document (`multipart/form-data` → `$ref: UploadImageFileRequest`), so it picks up its validator constraints and its XML summaries through the ordinary schema transformer. Under MVC it arrived flattened into one `ApiParameterDescription` per property and both had to be matched back onto the fields by hand — the operation transformer that did so is gone.

`Microsoft.OpenApi` is referenced directly: the transformers work against its schema model, and the pin lifts the vulnerable 2.0.0 that `Microsoft.AspNetCore.OpenApi` 10.0.10 would otherwise bring in (NU1903 / GHSA-v5pm-xwqc-g5wc, patched in 2.7.5).

### Storage model

- **Blobs**: two Azure containers, originals (`AzureStorage:ImagesContainerName`) and thumbnails (`AzureStorage:ThumbnailsContainerName`), selected by `ImageSizeType`. Blob names are random GUID-derived (`ImageIdentifierProvider`) because blobs are publicly reachable by URL.
- **Metadata**: one Cosmos container partitioned on `/ImageGroup`. `ImageGroup` is the partition key the README calls "partition key" — it is required on upload and optional-but-recommended on search/delete. Thumbnails are stored as a nested list on the parent `ImageMetadata` document, so thumbnail generation is an upsert of the whole document.

### Validation is two-layered

FluentValidation validators live next to the feature they guard under `IK.Imager.Api/Features` and check the *request shape* (URL well-formed, `ImageGroup` length 3–30, ≤200 image ids); they also surface into the OpenAPI document. FluentValidation 12 dropped built-in ASP.NET Core auto-validation, so `IK.Imager.Api/Validation/ValidationEndpointFilter.cs` runs the validator over the argument and short-circuits with a 400 `ValidationProblemDetails`. It is attached per endpoint with `.WithValidation<TRequest>()`, which also declares the 400 in the document — an endpoint that takes a validated model and forgets the call silently accepts anything, so add it alongside the `Map…` line. `IK.Imager.Core.Validation.ImageValidator` checks the *image itself* (format/size/dimensions/aspect ratio) against the `ImageLimitations` config section. Note the handlers currently throw `ValidationException` on failure (there are `//todo`s about returning an error model instead); `IK.Imager.Api/ExceptionHandling/GlobalExceptionHandler.cs` maps it to a 400 and everything else to a 500 (with the full exception in `developerMessage` in Development). It is an `IExceptionHandler` registered on the pipeline rather than an MVC filter, because an endpoint filter cannot see an exception thrown by another filter; there is no developer exception page, since the MVC filter suppressed it for every action exception anyway.

## Configuration

`src/IK.Imager.Api/appsettings.json` is the full parameter list. Environment variables override it with `__` as the section separator (`ServiceBus__ConnectionString`, `AzureStorage__ConnectionString`, `Logging__LogLevel__Default`). Defaults point at the local emulators, so the API starts against Azurite + Cosmos Emulator without changes — except `ServiceBus:ConnectionString`, which has no emulator and must be a real namespace or MassTransit startup fails.

**Do not re-register `appsettings.json` on `builder.Configuration`.** `WebApplication.CreateBuilder` already adds it (as *optional*) and then adds the environment variable provider on top. Calling `AddJsonFile("appsettings.json", optional: false)` afterwards appends a *second* JSON source at the end of the chain, where it wins over the env vars — which silently disables every `__` override for any key that exists in the file. `Program.cs` therefore asserts the file exists instead of re-adding it. Note that flipping `Optional` on the source `CreateBuilder` registered does not work either: `ConfigurationManager` builds its providers eagerly, so the mutation is a no-op.

Health endpoints: `/hc` (Cosmos + blob storage + the MassTransit bus) and `/liveness` (self only).
