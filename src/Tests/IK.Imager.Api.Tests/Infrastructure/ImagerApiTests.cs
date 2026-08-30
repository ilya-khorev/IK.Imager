using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace IK.Imager.Api.Tests.Infrastructure;

/// <summary>
/// Base class of every API test: the shared host, a client over it, and the two things nearly all of them
/// need - a private tenant, and a way to read a validation failure.
/// </summary>
[Collection(ImagerApiCollection.Name)]
public abstract class ImagerApiTests(ImagerApiFixture fixture)
{
    protected ImagerApiFixture Fixture { get; } = fixture;

    protected ImagerApiClient Api { get; } = new(fixture.Client);

    /// <summary>
    /// A tenant no other test writes into. The host is shared for the whole assembly, and the tenant is the
    /// first level of the Cosmos partition key, so this is what keeps one test's lookups blind to another's
    /// images. Lowercase hex, so it satisfies the identifier charset.
    /// </summary>
    protected static string NewTenantId() => $"t{Guid.NewGuid():N}";

    /// <summary>
    /// A collection name within the 3 to 30 characters the validators demand.
    /// </summary>
    protected static string NewCollection() => $"c{Guid.NewGuid():N}"[..21];

    /// <summary>
    /// Asserts the response is the 400 ValidationProblemDetails that ValidationEndpointFilter produces and
    /// returns it, so the caller can assert on which property failed.
    /// </summary>
    protected static async Task<HttpValidationProblemDetails> ReadValidationProblem(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 400 but got {(int)response.StatusCode}: {body}");

        return await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>()
               ?? throw new InvalidOperationException($"Could not read a validation problem from: {body}");
    }
}
