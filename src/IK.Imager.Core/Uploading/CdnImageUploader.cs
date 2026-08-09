using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Abstractions.Uploading;

#pragma warning disable 1591

namespace IK.Imager.Core.Uploading;

/// <summary>
/// <see cref="ImageUploader"/> always returns the raw blob url; this is where the CDN host is swapped in
/// when <c>Cdn:Uri</c> is configured. Wired in AddImagerCore - the concrete service is registered by its
/// own type, and <see cref="IImageUploader"/> resolves to this decorator around it.
/// </summary>
public class CdnImageUploader(IImageUploader inner, ICdnService cdnService) : IImageUploader
{
    public async Task<ImageInfo> Upload(Stream imageStream, string imageGroup, CancellationToken cancellationToken)
    {
        var response = await inner.Upload(imageStream, imageGroup, cancellationToken);
        response.Url = cdnService.TryTransformToCdnUri(response.Url);

        return response;
    }

    public async Task<ImageInfo> UploadByUrl(string imageUrl, string imageGroup, CancellationToken cancellationToken)
    {
        var response = await inner.UploadByUrl(imageUrl, imageGroup, cancellationToken);
        response.Url = cdnService.TryTransformToCdnUri(response.Url);

        return response;
    }
}
