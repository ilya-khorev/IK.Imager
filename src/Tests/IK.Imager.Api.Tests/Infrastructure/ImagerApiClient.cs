using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using IK.Imager.Api.Contract;
using IK.Imager.Api.Contract.Lookup;
using Xunit;

namespace IK.Imager.Api.Tests.Infrastructure;

/// <summary>
/// The four routes of the service, as a test reads them.
///
/// Every call comes in two shapes: a raw one taking whatever body the test wants to send - which is what the
/// validation tests need, since an invalid request is by definition one the contract type cannot express -
/// and a typed one that asserts success and hands back the deserialized contract model.
///
/// The tenant travels in a header rather than in the body, so every request is built by hand here instead of
/// through the PostAsJsonAsync shortcuts. A null tenant sends no header at all, which is what the tests over
/// the missing-tenant rejection need.
/// </summary>
public sealed class ImagerApiClient(HttpClient httpClient)
{
    public const string TenantHeader = "X-Tenant-Id";

    private const string UploadRoute = "/images/upload";
    private const string UploadByUrlRoute = "/images/upload-by-url";
    private const string LookupRoute = "/images/lookup";
    private const string DeleteRoute = "/images";

    //minimal APIs serialize with the web defaults, so the responses are camelCase
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public Task<HttpResponseMessage> PostUpload(string? fileName, string? tenantId, string? collection = null,
        string? imageId = null, bool includeCollectionInPath = false, bool addUniquePrefix = false,
        int[]? thumbnailTargetWidths = null)
    {
        var content = new MultipartFormDataContent();

        if (fileName != null)
        {
            var file = new StreamContent(File.OpenRead(TestImages.PathOf(fileName)));
            file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(file, "File", fileName);
        }

        if (collection != null)
            content.Add(new StringContent(collection), "Collection");

        if (imageId != null)
            content.Add(new StringContent(imageId), "ImageId");

        if (includeCollectionInPath)
            content.Add(new StringContent("true"), "IncludeCollectionInPath");

        if (addUniquePrefix)
            content.Add(new StringContent("true"), "AddUniquePrefix");

        //a form has no arrays, so a repeated field is how a collection is sent - which is also what the
        //[FromForm] model binder reads it back as
        foreach (var targetWidth in thumbnailTargetWidths ?? [])
            content.Add(new StringContent(targetWidth.ToString()), "ThumbnailTargetWidths");

        return Send(HttpMethod.Post, UploadRoute, tenantId, content);
    }

    public Task<HttpResponseMessage> PostUploadByUrl(object request, string? tenantId) =>
        Send(HttpMethod.Post, UploadByUrlRoute, tenantId, JsonContent.Create(request));

    public Task<HttpResponseMessage> PostLookup(object request, string? tenantId) =>
        Send(HttpMethod.Post, LookupRoute, tenantId, JsonContent.Create(request));

    /// <summary>
    /// The image id is a route segment, so a delete is expressed as a url rather than a body. It is passed
    /// raw so that a test can send something the contract type could not express, such as a blank id.
    /// </summary>
    public Task<HttpResponseMessage> SendDelete(string imageId, string? tenantId) =>
        Send(HttpMethod.Delete, $"{DeleteRoute}/{Uri.EscapeDataString(imageId)}", tenantId, content: null);

    public async Task<ImageInfo> Upload(string fileName, string tenantId, string? collection = null,
        string? imageId = null, bool includeCollectionInPath = false, bool addUniquePrefix = false,
        int[]? thumbnailTargetWidths = null) =>
        await ReadContract<ImageInfo>(
            await PostUpload(fileName, tenantId, collection, imageId, includeCollectionInPath, addUniquePrefix,
                thumbnailTargetWidths));

    public async Task<ImageInfo> UploadByUrl(string imageUrl, string tenantId, string? collection = null,
        int[]? thumbnailTargetWidths = null) =>
        await ReadContract<ImageInfo>(
            await PostUploadByUrl(
                new { ImageUrl = imageUrl, Collection = collection, ThumbnailTargetWidths = thumbnailTargetWidths },
                tenantId));

    public async Task<LookupImagesResult> Lookup(string[] imageIds, string tenantId) =>
        await ReadContract<LookupImagesResult>(await PostLookup(new { ImageIds = imageIds }, tenantId));

    /// <summary>
    /// Looks the image up and asserts it is there - for the many assertions that only make sense on a
    /// single image that is known to exist.
    /// </summary>
    public async Task<ImageWithThumbnails> LookupSingle(string imageId, string tenantId)
    {
        var result = await Lookup([imageId], tenantId);

        return Assert.Single(result.Images);
    }

    public async Task Delete(string imageId, string tenantId)
    {
        var response = await SendDelete(imageId, tenantId);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private async Task<HttpResponseMessage> Send(HttpMethod method, string url, string? tenantId, HttpContent? content)
    {
        using var request = new HttpRequestMessage(method, url);

        if (tenantId != null)
            request.Headers.Add(TenantHeader, tenantId);

        request.Content = content;

        return await httpClient.SendAsync(request);
    }

    private static async Task<T> ReadContract<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();

        //the body carries the reason on a 400 or a 500, and a bare status code assertion would throw it away
        Assert.True(response.IsSuccessStatusCode, $"Expected a success status code but got {(int)response.StatusCode}: {body}");

        return JsonSerializer.Deserialize<T>(body, SerializerOptions)
               ?? throw new InvalidOperationException($"Could not deserialize a {typeof(T).Name} from: {body}");
    }
}
