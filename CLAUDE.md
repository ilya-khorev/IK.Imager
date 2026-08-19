# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

All projects are .NET 10 (except `IK.Imager.Api.Contract`, see below); the solution is `src/IK.Imager.sln`.

```powershell
dotnet build src\IK.Imager.sln --configuration Release   # what CI builds (.github/workflows/dotnetcore.yml)
dotnet test src\IK.Imager.sln                            # all tests (integration tests need Docker, see below)
dotnet test src\IK.Imager.sln --filter "Category!=Integration"           # everything that runs without Docker
dotnet test src\Tests\IK.Imager.Core.Tests\IK.Imager.Core.Tests.csproj   # unit tests only — no Docker needed
dotnet test src\Tests\IK.Imager.Api.Tests\IK.Imager.Api.Tests.csproj     # the API end to end (needs Docker), ~20s
dotnet test src\Tests\IK.Imager.Core.Tests --filter "FullyQualifiedName~ThumbnailGeneratorTests"
dotnet test src\Tests\IK.Imager.Core.Tests --filter "DisplayName~Generate_ImageMetadataNotFound"
dotnet run --project src\IK.Imager.Api                   # http://localhost:5000, Swagger UI at the root path
```

`Scripts\AzureResources.ps1` provisions the Azure resources; `Scripts\DockerUpload.ps1` builds/pushes the Docker images.

### Before committing

Always run both of these before a commit, and report what they said. CI runs neither — `dotnetcore.yml` builds without `-warnaserror` and has no format step — so this is a local gate or it is nothing.

```powershell
dotnet build src\IK.Imager.sln --configuration Release -warnaserror   # must end in "0 Warning(s)"
dotnet format src\IK.Imager.sln --verify-no-changes                   # must exit 0
```

Both pass on a clean checkout. Either one failing is your change, not pre-existing debt — the repo was swept clean in one commit of its own.

**1. No new build warnings.** `-warnaserror` is the check rather than eyeballing the log, because a warning scrolls past in an 11-project build. It also promotes the transitive `NuGetAudit` advisories (NU1901–NU1904) to errors — that is intended, and a new one means bumping the package in `Directory.Packages.props`, not suppressing the code. Never silence a warning with `#pragma warning disable` or `<NoWarn>` to get the build green; fix the cause or say why it cannot be fixed.

**2. Formatting, via `.editorconfig`.** At its default severity `dotnet format` checks indentation and blank lines, trailing whitespace, the final newline, the UTF-8 charset (i.e. no BOM) and `using` ordering — which is what formatting means here. Run it without `--verify-no-changes` to apply the fixes.

**Do not add `--severity info` to that gate**, tempting as it looks given every rule in `.editorconfig` is `suggestion`. It reports 267 diagnostics across the solution — `IDE0161` (file-scoped namespaces), `IDE0007` (`var`), `IDE0011` (braces), `IDE0290` (primary constructors), plus the `CA` analyzers — in the older files that predate those conventions. It also cannot be applied: `dotnet format style --severity info` dies with `NotSupportedException: Changing document properties is not supported`, because the `IDE1006` naming fixer does not support Fix All. Treat that list as a backlog to work through by hand, file by file, not as a commit gate.

Two related traps:

- **`dotnet format` cannot strip a UTF-8 BOM.** The charset fixer hits the same `Changing document properties` crash, which aborts the whole run and writes *nothing* — including the whitespace fixes it had already computed. If `CHARSET` errors appear, rewrite those files byte-wise without the leading `EF BB BF` first, then re-run.
- **Line endings are `.gitattributes`' job, not `.editorconfig`'s.** `end_of_line` is deliberately absent from `[*]`; see the *Build configuration* notes.

Never run a repo-wide `dotnet format` and commit the result alongside a behaviour change — it buries the real diff. A sweep is welcome, as its own commit with nothing else in it.

### Build configuration

Shared MSBuild settings live outside the individual `.csproj` files, so most of them now contain nothing but references.

- `src/Directory.Build.props` — imported by every project under `src/`. Sets `TargetFramework` (`net10.0`), `LangVersion` (`latest`), `Nullable` (`enable`), transitive `NuGetAudit`, and `ContinuousIntegrationBuild` when `CI=true`. A property set in a `.csproj` still wins, which is how `IK.Imager.Api.Contract` keeps `netstandard2.1`.
- `src/Directory.Packages.props` — Central Package Management. `PackageReference` items carry **no `Version`**; add or bump a version here instead. This is what keeps `Azure.Storage.Blobs` / `Microsoft.Azure.Cosmos` identical between a production project and the test project that drives its emulator.
- `.editorconfig` (repo root) — codifies the existing style (4-space indent, file-scoped namespaces, `_camelCase` private fields, `var` everywhere). All style rules are `suggestion`; `EnforceCodeStyleInBuild` is deliberately **not** set, so none of them can fail a build — which is exactly why the pre-commit check above is manual. Apply with `dotnet format src\IK.Imager.sln`, and see *Before committing* for why that gate stays at the default severity. `end_of_line` is deliberately absent from `[*]`: `.gitattributes` owns line endings, and declaring `lf` here made `dotnet format` report every line of every file as an `ENDOFLINE` violation on a Windows checkout (4174 of 4439 errors) while the index was already LF. The `crlf` pins that remain — `.sln`, `.DotSettings`, `.ps1` — match files `.gitattributes` checks out as CRLF on every platform.
- `IK.Imager.sln.DotSettings` is still required alongside it: it holds the ReSharper/Rider naming-abbreviation list (`JPEG`, `WEBP`, …) and the spell-check user dictionary, neither of which has a standard `.editorconfig` equivalent.
- `global.json` pins the SDK to 10.0.x (`rollForward: latestFeature`); `nuget.config` clears inherited feeds so restore only ever sees nuget.org.
- `.gitattributes` normalises text to LF in the index (`* text=auto`, with `eol=crlf` pinned for `.sln`/`.ps1`/`.DotSettings`). The index is already LF, so a Windows checkout being CRLF on disk is expected and is not a diff.

Nullable reference types are on everywhere. Config-bound options classes and models populated by deserialization use `= null!` rather than `required`, which keeps today's behaviour (a missing value fails at first use) and keeps `IK.Imager.Api.Contract` usable from `netstandard2.1`, where `required` does not exist.

### Records, and where they stop

A model is a `record` when it is built once and only read afterwards. Two shapes are in use, deliberately:

- **Positional**, for the small core value objects — `ImageFormat`, `ImageSize`, `ImageResizeResult`, and the two integration events. Everything is passed at construction, so there is nothing to say twice.
- **`init` properties, not positional**, for every `IK.Imager.Api.Contract` model. The OpenAPI schema descriptions come from the `<summary>` of each property, which a positional record would have to express as `<param>` on the primary constructor; keeping properties keeps the document identical and keeps the object-initializer call sites in the endpoint mappings readable. Model binding populates `init` properties fine — verified against a running host for JSON bodies, for a record inheriting `UploadImageRequestBase`, and for the multipart `UploadImageFileRequest` including its `IFormFile`.

`IK.Imager.Api.Contract` is `netstandard2.1`, which predates `System.Runtime.CompilerServices.IsExternalInit` — the type every `init` accessor compiles a reference to. `IsExternalInit.cs` declares it `internal` so it never collides with the real one in a consumer. Removing that file breaks every `init` in the project.

These stay classes, each for a reason worth not re-litigating:

- **`IK.Imager.Core.Abstractions` `ImageDetails` / `ImageDetailsWithThumbnails` / `ImageLookupResult`** — the CDN decorators rewrite `Url` in place, including on thumbnails nested in a list. Immutability would mean rebuilding those lists with `with`, which is more code than the assignment it replaces.
- **`ImageMetadata`** — hand-writes `IEquatable` with a *sequence* comparison of `Thumbnails`. A generated record equality would silently downgrade that to reference equality on the `List<>`. It is also Newtonsoft-bound for Cosmos and mutated in place before the upsert.
- **The options classes** (`CdnSettings`, `ImageThumbnailsSettings`, `ImageLimitationsSettings`, `TopicsSettings`) — `IOptions<T>` resolves through `Activator.CreateInstance<T>()` and needs a public parameterless constructor, which a positional record does not have. That failure appears at first resolve, not at compile time.

Note the value equality of `ImageLookupResult` and `ImageDetailsWithThumbnails` is only as deep as their `List<>` properties, i.e. reference equality for the collections. Do not lean on `==` for these.

### Test prerequisites

- `IK.Imager.Core.Tests` — xUnit + Moq + AutoFixture, fully in-memory. Laid out to mirror `IK.Imager.Core`: `Upload/`, `Lookup/`, `Delete/`, `Thumbnails/`, `Cdn/`, with the sample-file paths in `Infrastructure/SampleImages.cs`. Sample images under `Images/`, plus `Files/not-an-image.txt` for the reject case, are copied to the output dir. **No Docker needed.**
- `IK.Imager.Storage.AzureBlobs.Tests` and `IK.Imager.Storage.CosmosDb.Tests` — **require a running Docker daemon**, and nothing else. [Testcontainers](https://dotnet.testcontainers.org/) starts [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) and the [Linux Cosmos DB emulator](https://learn.microsoft.com/azure/cosmos-db/emulator-linux) on randomly mapped host ports and tears them down afterwards; both classes are tagged `[Trait("Category", "Integration")]`.
  - Image tags are pinned in `Tests/IK.Imager.TestsBase/Constants.cs`, which now holds only names and tags — connection strings exist only at runtime, built from the mapped ports.
  - Both test projects reference the storage SDK directly (`Azure.Storage.Blobs` / `Microsoft.Azure.Cosmos`) for their fixtures; Central Package Management keeps those at the same version as the production project automatically.
  - Each project starts **one** container per test assembly via an `ICollectionFixture` (`AzuriteCollection` / `CosmosDbCollection`); the fixtures are `IAsyncLifetime`. Neither drops its blob/Cosmos containers — the emulator itself is thrown away.
  - Azurite runs with `--skipApiVersionCheck` because `Azure.Storage.Blobs` sends a newer `x-ms-version` than Azurite recognises.
  - The Cosmos emulator serves plain HTTP in gateway mode and advertises its container-internal endpoint, so `ImageContainerFactory` takes an **optional `CosmosClientOptions`** purely so the fixture can pass `ConnectionMode.Gateway` + `LimitToEndpoint` + the module's URI-rewriting `HttpClientFactory`. Production passes nothing and keeps the SDK defaults (hence the explicit factory registration in `Program.cs`). Do not set `Serializer` — `ImageMetadata.Id` relies on Newtonsoft's `[JsonProperty("id")]`.
  - The Cosmos emulator image is ~1 GB; the first run pulls it. Pre-pull with `docker pull` to keep it out of the Testcontainers startup timeout.
- `IK.Imager.Api.Tests` — the API end to end, also **Docker-only**. `ImagerApiFixture` starts Azurite and the Cosmos emulator, then boots `IK.Imager.Api` itself in-process with `WebApplicationFactory<Program>` and drives it over HTTP. One host and one pair of containers for the whole assembly (`ImagerApiCollection`); a run takes ~20s.
  - Laid out to mirror `IK.Imager.Api/Features` — `Features/Upload`, `Features/Lookup`, `Features/Delete`, and `Features/Thumbnails` for the generation that `CreateThumbnailsConsumer` drives — with namespaces to match, so an endpoint and the tests over it are found the same way from either side. `Infrastructure/` holds the shared harness: the fixture, the collection, the `ImagerApiTests` base class, `ImagerApiClient`, `ConsumedEventObserver` and `TestImages`. Add a test to the feature folder it belongs to.
  - `Program.cs` ends with `public partial class Program;` for exactly this — the class the compiler generates for top-level statements is internal, and `WebApplicationFactory` needs a public one.
  - Configuration reaches the host as **environment variables** (`AzureStorage__ConnectionString`, …), not `UseSetting`: with minimal hosting, `Program.cs` has already read `builder.Configuration` and registered every module before the factory can contribute anything, whereas `WebApplication.CreateBuilder` reads the environment on the way in. `ImagerApiFixture` restores the previous values on dispose.
  - The only registration overridden in the factory is `IImageContainerFactory`, to pass the same emulator `CosmosClientOptions` the Cosmos test fixture uses.
  - Tests wait on the bus, never on the clock. `ConsumedEventObserver` is an `IConsumeObserver` connected to `IBus`, so a test can await the moment `CreateThumbnailsConsumer` (or `RemoveImageFilesConsumer`) has actually finished with a given image id — which is also what makes "no thumbnails were generated" assertable rather than a race. A faulted consumer surfaces as the original exception on the awaiting test.
  - Sample images are **linked** from `IK.Imager.Core.Tests` by the `.csproj` rather than copied, and the widths in `TestImages` are chosen against `Thumbnails:TargetWidth` (`[200, 400, 1000]`) to land on three, two and no thumbnails.
  - Each test uses its own generated image group. The host is shared and the image group is the partition key, so that is what keeps one test's lookups blind to another's images.

**Azure Service Bus is not a container in these tests**, and the reason is version-specific. MassTransit drives topology through `ServiceBusAdministrationClient`; the Service Bus emulator only grew an administration API in 2026, and MassTransit only speaks to it from **v9**, which is commercially licensed — v8, which this repo pins, will not get it ([#5689](https://github.com/MassTransit/MassTransit/issues/5689)). So `AddIntegrationEventMessaging` takes a `ServiceBus:Transport` switch: anything other than `InMemory` (including the absent setting) keeps Azure Service Bus, and the fixture sets `ServiceBus__Transport=InMemory`. The consumers, the events and the asynchronous publish/consume path are the production ones either way; only the wire underneath them differs. The switch also makes the service runnable locally without a real namespace — but in-memory has no persistence and is never a deployment option.

CI (`.github/workflows/dotnetcore.yml`) runs on `ubuntu-latest` and now builds **and** tests — the hosted Windows runners cannot run Linux containers.

Test naming convention (stated in `AzureBlobImageRepositoryTests`): `MethodUnderTest_Scenario_ExpectedBehavior`.

## Code style

- Prefer clean, self-explanatory code over comments.
- Do not add comments that merely describe what the code does.
- Add comments only when they explain:
  - a non-obvious decision;
  - an important constraint;
  - a workaround;
  - behavior that would otherwise be surprising.
- Keep comments short and simple.
- Use plain English.
- Avoid complex words, idioms, and long sentences.
- Do not use comments as documentation for obvious methods or variables.
- Do not add comments like:
  - "Initialize the service"
  - "Get the user by ID"
  - "Check if the value is null"
  - "Return the result"
- Do not add comments describing changes you just made.
- Prefer descriptive names instead of explanatory comments.
- Do not add `// Arrange`, `// Act`, `// Assert` comments to tests unless
  they improve readability.

## Language

When writing code comments, documentation, commit messages, or developer-facing text:

- Use simple and direct English.
- Prefer short sentences.
- Avoid sophisticated or unusual vocabulary when a common word works.
- Avoid idioms and marketing-style language.
- Write as an experienced developer communicating with other developers.

## Architecture

A single ASP.NET Core service (`IK.Imager.Api`) that both serves the HTTP API **and** consumes its own integration events off Azure Service Bus. An earlier `IK.Imager.BackgroundService` microservice was folded into the API — `Scripts/DockerUpload.ps1` and `docs/Architecture.svg` still refer to it.

### Request flow

**There is no mediator and no command/handler abstraction.** The core is four ordinary services, one per feature, each behind a small interface in `IK.Imager.Core.Abstractions`:

| Interface | Implementation | Methods |
|---|---|---|
| `IImageUploader` | `Core/Upload/ImageUploader.cs` | `Upload(stream, group, ct)`, `UploadByUrl(url, group, ct)` |
| `IImageLookup` | `Core/Lookup/ImageLookup.cs` | `LookupByIds(ids, group, ct)` |
| `IImageDeleter` | `Core/Delete/ImageDeleter.cs` | `DeleteMetadata(id, group, ct) → bool`, `DeleteFiles(id, name, thumbnailNames, ct)` |
| `IThumbnailGenerator` | `Core/Thumbnails/ThumbnailGenerator.cs` | `Generate(imageId, group, ct)` |

An earlier version routed everything through `ICommandHandler<TCommand, TResult>` / `IQueryHandler<TQuery, TResult>` plus a command record per operation. The record existed only to be unpacked one line into the handler, and the generic interface made every registration and call site harder to read than the method it stood for — it bought pipeline behaviours nothing ever added. Pass arguments; add a method to the service that owns the feature.

`IK.Imager.Core` and `IK.Imager.Core.Abstractions` are both split into the same feature folders — `Upload/`, `Lookup/`, `Delete/`, `Thumbnails/`, plus `Cdn/`. Each holds the service and everything only that feature uses: `Upload/` also owns `ImageInspector`, `ImageDownloader`, `ImageValidator`, `ImageLimitationsSettings` and `ValueRange<T>`; `Thumbnails/` owns `ImageResizer` and `ImageThumbnailsSettings`. There is no `Settings/` or `Validation/` folder any more — a settings class lives next to the single class that reads it. Only `ImageNameGenerator` (upload **and** thumbnails) and `IImageEvents` stay at the project root.

**One vocabulary for a feature, everywhere: `Upload` / `Lookup` / `Delete`.** The same three folder names appear in `IK.Imager.Core`, `IK.Imager.Core.Abstractions`, `IK.Imager.Api/Features`, `IK.Imager.Api.Contract` and `IK.Imager.Api.Tests/Features`, with namespaces to match. Earlier passes left three spellings of the same three features (`Uploading/`, `ImageUpload/`, `ImageDeleting/`) — verb nouns matching the routes and the service methods replaced all of them.

**The folders deliberately drop the `Image` prefix.** `IK.Imager.Core.ImageLookup` would be a namespace wrapping a type of the same name, which the compiler rejects at every use site (*"'ImageLookup' is a type not a namespace"*). `IK.Imager.Core.Upload` also reads better than `…Core.ImageUpload` inside a project already called Imager.

Services use primary constructors, and they thread the `CancellationToken` they are given into every repository call — an earlier version accepted a token and then passed `CancellationToken.None` everywhere.

Endpoint handlers are thin: they take the service interface they need as a parameter (resolved from DI), call it, and map the core model to the `IK.Imager.Api.Contract` model with a private `ToContract()` in their own endpoint file (hand-written — there is no AutoMapper, and there is no shared mapping class).

### Endpoints are minimal APIs, grouped by feature

There are no controllers and no MVC services — `IK.Imager.Api/Features` holds one folder per feature (`Upload`, `Lookup`, `Delete`), and a feature owns its routes, its request models, its FluentValidation validators and its mapping onto the contract models. Each folder exposes a `Map…Endpoints(this IEndpointRouteBuilder)` extension; `Features/ImageEndpoints.cs` creates the `/images` group (tagged `Images`, which is what keeps the Swagger UI grouping the controller used to give) and calls them. Add an endpoint to the feature it belongs to, never to the aggregator.

A slice is kept self-contained rather than DRY: `Lookup` maps a thumbnail onto `Contract.ImageInfo` itself instead of reaching for the identical mapping in `Upload`. Eight assignments are not worth coupling two features together — factor a mapping out only once a third feature needs it.

`IK.Imager.Api.Contract` mirrors the same three folders and the same namespaces — `IK.Imager.Api.Contract.Upload` / `.Lookup` / `.Delete` — so a request model and the endpoint that binds it are found the same way from either side. `ImageInfo` is the exception: it is the response model both upload and lookup return, so it stays at the root in the flat `IK.Imager.Api.Contract`. `UploadImageFileRequest` is the other: it binds an `IFormFile`, which `netstandard2.1` cannot reference, so it lives in `IK.Imager.Api/Features/Upload` instead.

**Core and contract models are named apart on purpose.** The core returns `ImageDetails` / `ImageDetailsWithThumbnails` / `ImageLookupResult`; the contract returns `ImageInfo` / `ImageWithThumbnails` / `LookupImagesResult`. They used to share `ImageInfo` and `ImageFullInfoWithThumbnails`, which forced `CoreModels =` / `CoreLookup =` using-aliases at the top of every endpoint file. Both sets can now be imported into the same file plainly.

The routes: `POST /images/upload`, `POST /images/upload-by-url`, `POST /images/lookup`, `DELETE /images/{imageId}`.

**"Lookup", never "search".** The operation fetches images by their ids — there is no querying or filtering — so `search` was retired everywhere: `IImageLookup.LookupByIds` in the core, `LookupImagesRequest` / `LookupImagesResult` in the contract, `POST /images/lookup` on the wire.

Handlers are named `internal static` methods rather than lambdas: the source generator in `Microsoft.AspNetCore.OpenApi` reads their XML documentation, so `<summary>`, `<param>` and `<response code="…">` land in the document exactly as the controller attributes used to. Return `TypedResults` (`Ok<T>`, `Results<NoContent, NotFound<string>>`) so the response types are inferred rather than declared twice.

Two things minimal APIs need that MVC did implicitly:
- `POST /images/upload` calls `.DisableAntiforgery()`. Form endpoints require an antiforgery token by default, which only makes sense for a cookie-authenticated browser form; `[ApiController]` never enforced it.
- `DELETE /images/{imageId}` marks its request model `[AsParameters]`, binding `ImageId` from the route and `ImageGroup` from the query string. DELETE is one of the methods minimal APIs refuse to infer a **body** for, and that failure is an `InvalidOperationException` on the first request rather than a compile error — putting the id in the route avoids the shape entirely. Note an absent id then fails to match the route at all (a 404 from routing), so only a blank-but-present id reaches `DeleteImageRequestValidator`.

Upload → `ImageUploader.Upload` (validate format/size → blob storage → metadata) → `IImageEvents.ImageUploaded` → published as the integration event `OriginalImageUploadedIntegrationEvent` on Service Bus → `CreateThumbnailsConsumer` consumes it and calls `IThumbnailGenerator.Generate`. Hence the ~2s delay before thumbnails appear in lookup results.

Delete → `ImageDeleter.DeleteMetadata` removes only the metadata (image disappears from lookup results immediately) → `IImageEvents.ImageMetadataDeleted` → `ImageMetadataDeletedIntegrationEvent` → `RemoveImageFilesConsumer` → `ImageDeleter.DeleteFiles` deletes the original blob and thumbnail blobs, then `CdnImageDeleter` purges them from the CDN.

**`IImageEvents` is how the core reaches the bus without depending on it.** It is declared in `IK.Imager.Core.Abstractions` as two plain methods and implemented once, by `IK.Imager.Api/IntegrationEvents/ImageEventPublisher.cs`, which publishes over MassTransit; `AddIntegrationEventMessaging` registers it alongside the bus it publishes onto. It replaced an `IDomainEvent` / `IDomainEventHandler<T>` / `IDomainEventDispatcher` trio plus a dispatcher that resolved handlers out of the container — three interfaces, two event records and two handler classes, for two events that had exactly one handler each. If an event ever genuinely needs several independent reactions, add them on the bus side rather than reviving in-process dispatch.

CDN work is kept outside the services, in a decorator sitting next to the service it wraps — `Upload/CdnImageUploader.cs`, `Lookup/CdnImageLookup.cs` and `Delete/CdnImageDeleter.cs`. A service always returns the raw blob URL and the decorator swaps in the CDN host when `Cdn:Uri` is configured. This is the one reason those services have interfaces at all, and it is what the by-type-then-interface pair in `AddImagerCore` is for: `AddScoped<ImageLookup>()` registers the concrete service, and `AddScoped<IImageLookup>(…)` resolves to the decorator wrapping it. `IThumbnailGenerator` has no CDN concern and is registered plainly. Add a decorator in the feature folder rather than touching a service when a new response needs URL rewriting.

**`ICdnPurger` is the third decorator, and it is about deletion rather than URLs.** Removing a blob does not clear an edge cache, so a deleted image keeps being served until its TTL expires. `CdnImageDeleter` collects the CDN uris of the original and every thumbnail, lets `ImageDeleter` remove the blobs, and then purges — in that order, because purging first only makes the edge re-fetch blobs that are still there. One call covers the whole image; providers rate limit the request, not the uri.

`ICdnPurger` (`IK.Imager.Core.Abstractions/Cdn`) is deliberately provider-agnostic — it takes absolute uris already on the CDN host, since Cloudflare, Akamai and Fastly purge by full url while Azure Front Door and CloudFront want `Uri.AbsolutePath`. Core registers `NoOpCdnPurger` with `TryAddSingleton`, so **adding a CDN is one class plus one registration in its own module**, with no change to Core, and a deployment without a CDN keeps working untouched. Implementations throw on failure: the only caller is `RemoveImageFilesConsumer`, so an exception becomes a MassTransit retry and finally a dead letter, and the blob deletes it re-runs are idempotent. They should also return once the purge is *accepted* rather than propagated — a purge takes minutes, and blocking would hold the Service Bus message lock.

### Project layout

`IK.Imager.Api` (host, feature endpoints, validators, event translation) → `IK.Imager.Core` (all handlers, thumbnail resizing via ImageSharp, validation, CDN) → `IK.Imager.Core.Abstractions` → `IK.Imager.Storage.Abstractions` (interfaces + models; the latter has no project dependencies at all). Storage implementations (`Storage.AzureBlobs`, `Storage.CosmosDb`) depend only on the storage abstractions. `IK.Imager.Api.Contract` is the only `netstandard2.1` project — it's the public DTO contract, kept separately so clients can reference it.

**`IK.Imager.Core.Abstractions` references `IK.Imager.Storage.Abstractions` for exactly one type: `ImageType`.** The enum used to be declared identically in both, and `ImageUploader` and `ThumbnailGenerator` cast between the two copies numerically — reordering either one would have silently mis-stored every image, with nothing to catch it at compile time. Storage owns the single copy because `ImageMetadata.ImageType` is the persisted one. The cost is that Core.Abstractions now picks up `Newtonsoft.Json` transitively; the alternative, a new assembly holding one enum, is the mistake `IK.Imager.Utils` used to be.

**There is no `IK.Imager.Utils`.** It was an assembly holding a single `ArgumentHelper` that predated `ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrEmpty`. Use the BCL guards; they infer the parameter name from the caller expression, so they take no `nameof`.

### DI registration

**Every module registers its own services** through a `…ServiceCollectionExtensions` class in its own project (`CoreServiceCollectionExtensions`, `AzureBlobStorageServiceCollectionExtensions`, `CosmosDbServiceCollectionExtensions` — prefixed so the three do not read identically in search results). Add a new registration to the module it belongs to, never to `Program.cs`.

| Method | Project | Binds |
|---|---|---|
| `AddImagerCore(IConfiguration, Action<IHttpClientBuilder>?)` | `IK.Imager.Core` | `Cdn`, `Thumbnails`, `ImageLimitations` |
| `AddAzureImageBlobStorage(IConfiguration)` | `IK.Imager.Storage.AzureBlobs` | `AzureStorage` |
| `AddCosmosImageMetadataStorage(IConfiguration)` | `IK.Imager.Storage.CosmosDb` | `CosmosDb` |
| `AddApiServices()` / `AddOpenApiDocumentation()` / `AddIntegrationEventMessaging(IConfiguration)` / `AddObservability(IConfiguration)` | `IK.Imager.Api/Extensions` | `Topics` |

The convention: each method takes the **configuration root** and owns its section name as a `public const SectionName` next to the settings class it binds — so the magic string never appears at the call site. All of them return `IServiceCollection` and chain.

`AddImagerCore` takes an optional `Action<IHttpClientBuilder>` for the `ImageDownloader` typed client: Core owns *what* the client is, the host owns HTTP resilience, so `Microsoft.Extensions.Http.Polly` stays out of Core.

Health checks stay in the host (`Extensions/ObservabilityExtensions.cs`) rather than moving into the storage modules — `AspNetCore.HealthChecks.*` is versioned independently and which endpoints get probed is an operational decision. They read the storage connection settings through `IOptions<T>` so a probe can never target a different database or container than the repositories do.

`AddApiServices()` is what the endpoints need rather than an MVC layer: the `GlobalExceptionHandler`, `AddProblemDetails()`, and every validator in the assembly.

The host uses minimal hosting: `IK.Imager.Api/Program.cs` is a top-level-statements file that wires configuration and logging, calls the module extensions, and then builds the pipeline inline — `UseExceptionHandler()` outermost, the OpenAPI document and its Swagger UI, `MapImageEndpoints()`, `MapImagerHealthChecks()`. There is no `Startup` class and no `UseImagerPipeline()` extension: four lines are clearer where they run than behind a name. It turns on `ValidateScopes` + `ValidateOnBuild` in every environment — nothing tests the container, so a captive dependency or a dropped registration should fail at `builder.Build()` rather than on the first request.

### API documentation

The OpenAPI document is generated by **ASP.NET Core itself** (`Microsoft.AspNetCore.OpenApi`), served at `/openapi/v1.json` as OpenAPI 3.1. Swashbuckle is reduced to `Swashbuckle.AspNetCore.SwaggerUI` — the UI only, hosted at the root path; there is no `SwaggerGen` and no `UseSwagger()`. Both halves are wired in `IK.Imager.Api/Extensions/OpenApiServiceCollectionExtensions.cs`.

Endpoint and schema descriptions come from the XML documentation of `IK.Imager.Api` and `IK.Imager.Api.Contract`, which the source generator inside `Microsoft.AspNetCore.OpenApi` bakes into the assembly at compile time. `GenerateDocumentationFile` must stay on in both projects — the generator is what carries the endpoint summaries and `<response>` codes, and nothing reads the `.xml` files at runtime.

`IK.Imager.Api/OpenApi` replaces MicroElements.Swashbuckle.FluentValidation, which only plugs into Swashbuckle's schema generator: `FluentValidationRules` maps a property validator onto a schema (`NotEmpty`/`NotNull` → `required` plus `minLength`/`minItems`, `Length` → `minLength`/`maxLength`, `Matches` → `pattern`). `FluentValidationSchemaTransformer` applies it to every request model. Rules expressed as `Must(...)` predicates are invisible to it, exactly as they were to MicroElements.

The `[FromForm]` model of `POST /images/upload` needs nothing special. A minimal API endpoint keeps it a type all the way into the document (`multipart/form-data` → `$ref: UploadImageFileRequest`), so it picks up its validator constraints and its XML summaries through the ordinary schema transformer. Under MVC it arrived flattened into one `ApiParameterDescription` per property and both had to be matched back onto the fields by hand — the operation transformer that did so is gone.

`Microsoft.OpenApi` is referenced directly: the transformers work against its schema model, and the pin lifts the vulnerable 2.0.0 that `Microsoft.AspNetCore.OpenApi` 10.0.10 would otherwise bring in (NU1903 / GHSA-v5pm-xwqc-g5wc, patched in 2.7.5).

### Storage model

- **Blobs**: two Azure containers, originals (`AzureStorage:ImagesContainerName`) and thumbnails (`AzureStorage:ThumbnailsContainerName`), selected by `ImageVariant`. Blob names are random GUID-derived (`ImageNameGenerator`) because blobs are publicly reachable by URL.
- **Metadata**: one Cosmos container partitioned on `/ImageGroup`. `ImageGroup` is the partition key — it is required on upload and optional-but-recommended on lookup/delete. Thumbnails are stored as a nested list on the parent `ImageMetadata` document, so thumbnail generation is an upsert of the whole document.

### Validation is two-layered

FluentValidation validators live next to the feature they guard under `IK.Imager.Api/Features` and check the *request shape* (URL well-formed, `ImageGroup` length 3–30, ≤200 image ids); they also surface into the OpenAPI document. FluentValidation 12 dropped built-in ASP.NET Core auto-validation, so `IK.Imager.Api/Validation/ValidationEndpointFilter.cs` runs the validator over the argument and short-circuits with a 400 `ValidationProblemDetails`. It is attached per endpoint with `.WithValidation<TRequest>()`, which also declares the 400 in the document — an endpoint that takes a validated model and forgets the call silently accepts anything, so add it alongside the `Map…` line. `IK.Imager.Core.Upload.ImageValidator` checks the *image itself* (format/size/dimensions/aspect ratio) against the `ImageLimitations` config section. Note the core services currently throw `ValidationException` on failure (there are `//todo`s about returning an error model instead); `IK.Imager.Api/ExceptionHandling/GlobalExceptionHandler.cs` maps it to a 400 and everything else to a 500 (with the full exception in `developerMessage` in Development). It is an `IExceptionHandler` registered on the pipeline rather than an MVC filter, because an endpoint filter cannot see an exception thrown by another filter; there is no developer exception page, since the MVC filter suppressed it for every action exception anyway.

## Configuration

`src/IK.Imager.Api/appsettings.json` is the full parameter list. Environment variables override it with `__` as the section separator (`ServiceBus__ConnectionString`, `AzureStorage__ConnectionString`, `Logging__LogLevel__Default`). Defaults point at the local emulators, so the API starts against Azurite + Cosmos Emulator without changes — except `ServiceBus:ConnectionString`, which has no emulator MassTransit 8 can drive and must be a real namespace, *unless* `ServiceBus:Transport` is set to `InMemory`. See *Test prerequisites* for why that switch exists and why in-memory is a test and local-development option only.

**Do not re-register `appsettings.json` on `builder.Configuration`.** `WebApplication.CreateBuilder` already adds it (as *optional*) and then adds the environment variable provider on top. Calling `AddJsonFile("appsettings.json", optional: false)` afterwards appends a *second* JSON source at the end of the chain, where it wins over the env vars — which silently disables every `__` override for any key that exists in the file. `Program.cs` therefore asserts the file exists instead of re-adding it. Note that flipping `Optional` on the source `CreateBuilder` registered does not work either: `ConfigurationManager` builds its providers eagerly, so the mutation is a no-op.

Health endpoints: `/hc` (Cosmos + blob storage + the MassTransit bus) and `/liveness` (self only).
