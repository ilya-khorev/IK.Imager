using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Abstractions.Models;

#pragma warning disable 1591

namespace IK.Imager.Core.Cdn;

/// <summary>
/// The services always return the raw blob url. These decorators are the single place where the blob host
/// is swapped for the CDN host, so a new response needing url rewriting gets a decorator here
/// rather than a change inside a service.
/// </summary>
public class CdnImageLookup : IImageLookup
{
    private readonly IImageLookup _inner;
    private readonly ICdnService _cdnService;

    public CdnImageLookup(IImageLookup inner, ICdnService cdnService)
    {
        _inner = inner;
        _cdnService = cdnService;
    }

    public async Task<ImageLookupResult> LookupByIds(string[] imageIds, string? imageGroup, CancellationToken cancellationToken)
    {
        var response = await _inner.LookupByIds(imageIds, imageGroup, cancellationToken);

        if (!response.Images.Any())
            return response;

        foreach (var image in response.Images)
        {
            image.Url = _cdnService.TryTransformToCdnUri(image.Url);
            foreach (var thumbnail in image.Thumbnails)
                thumbnail.Url = _cdnService.TryTransformToCdnUri(thumbnail.Url);
        }

        return response;
    }
}

public class CdnImageUploader : IImageUploader
{
    private readonly IImageUploader _inner;
    private readonly ICdnService _cdnService;

    public CdnImageUploader(IImageUploader inner, ICdnService cdnService)
    {
        _inner = inner;
        _cdnService = cdnService;
    }

    public async Task<ImageInfo> Upload(Stream imageStream, string imageGroup, CancellationToken cancellationToken)
    {
        var response = await _inner.Upload(imageStream, imageGroup, cancellationToken);
        response.Url = _cdnService.TryTransformToCdnUri(response.Url);

        return response;
    }

    public async Task<ImageInfo> UploadByUrl(string imageUrl, string imageGroup, CancellationToken cancellationToken)
    {
        var response = await _inner.UploadByUrl(imageUrl, imageGroup, cancellationToken);
        response.Url = _cdnService.TryTransformToCdnUri(response.Url);

        return response;
    }
}
