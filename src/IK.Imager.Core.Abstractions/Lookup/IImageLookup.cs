using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Models;

namespace IK.Imager.Core.Abstractions.Lookup;

/// <summary>
/// Reads images that are already in the system.
/// </summary>
public interface IImageLookup
{
    /// <summary>
    /// Returns the images matching <paramref name="imageIds"/>, each with its thumbnails. An id with no image
    /// behind it is simply absent from the result. Ids are only ever resolved within
    /// <paramref name="tenantId"/>.
    /// </summary>
    Task<ImageLookupResult> LookupByIds(string[] imageIds, string tenantId, CancellationToken cancellationToken);
}
