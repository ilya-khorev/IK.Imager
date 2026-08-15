using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using IK.Imager.Api.Contract;
using IK.Imager.Api.Contract.ImageLookup;
using Xunit;

namespace IK.Imager.Api.Tests;

/// <summary>
/// The four routes of the service, as a test reads them.
///
/// Every call comes in two shapes: a raw one taking whatever body the test wants to send - which is what the
/// validation tests need, since an invalid request is by definition one the contract type cannot express -
/// and a typed one that asserts success and hands back the deserialized contract model.
/// </summary>
public sealed class ImagerApiClient(HttpClient httpClient)
{
    private const string UploadRoute = "/Images/Upload";
    private const string UploadByUrlRoute = "/Images/UploadByUrl";
    private const string LookupRoute = "/Images/Lookup";
    private const string DeleteRoute = "/Images";

    //minimal APIs serialize with the web defaults, so the responses are camelCase
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<HttpResponseMessage> PostUpload(string? fileName, string? imageGroup)
    {
        using var content = new MultipartFormDataContent();

        if (fileName != null)
        {
            var file = new StreamContent(File.OpenRead(TestImages.PathOf(fileName)));
            file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Add(file, "File", fileName);
        }

        if (imageGroup != null)
            content.Add(new StringContent(imageGroup), "ImageGroup");

        return await httpClient.PostAsync(UploadRoute, content);
    }

    public Task<HttpResponseMessage> PostUploadByUrl(object request) =>
        httpClient.PostAsJsonAsync(UploadByUrlRoute, request);

    public Task<HttpResponseMessage> PostLookup(object request) =>
        httpClient.PostAsJsonAsync(LookupRoute, request);

    //DELETE with a body is not something HttpClient has a shorthand for
    public Task<HttpResponseMessage> SendDelete(object request) =>
        httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Delete, DeleteRoute)
        {
            Content = JsonContent.Create(request)
        });

    public async Task<ImageInfo> Upload(string fileName, string imageGroup) =>
        await ReadContract<ImageInfo>(await PostUpload(fileName, imageGroup));

    public async Task<ImageInfo> UploadByUrl(string imageUrl, string imageGroup) =>
        await ReadContract<ImageInfo>(await PostUploadByUrl(new { ImageUrl = imageUrl, ImageGroup = imageGroup }));

    public async Task<ImageLookupResult> Lookup(string[] imageIds, string? imageGroup) =>
        await ReadContract<ImageLookupResult>(await PostLookup(new { ImageIds = imageIds, ImageGroup = imageGroup }));

    /// <summary>
    /// Looks the image up and asserts it is there - for the many assertions that only make sense on a
    /// single image that is known to exist.
    /// </summary>
    public async Task<ImageFullInfoWithThumbnails> LookupSingle(string imageId, string? imageGroup)
    {
        var result = await Lookup([imageId], imageGroup);

        return Assert.Single(result.Images);
    }

    public async Task Delete(string imageId, string? imageGroup)
    {
        var response = await SendDelete(new { ImageId = imageId, ImageGroup = imageGroup });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
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
