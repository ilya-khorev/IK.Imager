using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Abstractions.Lookup;

#pragma warning disable 1591

namespace IK.Imager.Core.Lookup;

/// <summary>
/// <see cref="ImageLookup"/> always returns raw blob urls; this is where the CDN host is swapped in
/// when <c>Cdn:Uri</c> is configured. Wired in AddImagerCore - the concrete service is registered by its
/// own type, and <see cref="IImageLookup"/> resolves to this decorator around it.
/// </summary>
public class CdnImageLookup(IImageLookup inner, ICdnUrlRewriter cdnService) : IImageLookup
{
    public async Task<ImageLookupResult> LookupByIds(string[] imageIds, string? imageGroup, CancellationToken cancellationToken)
    {
        var response = await inner.LookupByIds(imageIds, imageGroup, cancellationToken);

        if (!response.Images.Any())
            return response;

        foreach (var image in response.Images)
        {
            image.Url = cdnService.Rewrite(image.Url);
            foreach (var thumbnail in image.Thumbnails)
                thumbnail.Url = cdnService.Rewrite(thumbnail.Url);
        }

        return response;
    }
}
