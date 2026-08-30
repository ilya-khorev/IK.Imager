using System.Collections.Generic;

namespace IK.Imager.Core.Abstractions.Upload;

/// <summary>
/// The parts of an upload the caller gets to choose. Grouped rather than passed one by one, because they are
/// all optional and all shape the same thing - where the image ends up and what its url looks like. This is
/// not a command object routed through a handler; the uploader still takes plain arguments.
/// </summary>
/// <param name="ImageId">
/// Id of the new image, unique within the tenant. Generated when null.
/// </param>
/// <param name="Collection">
/// Optional label grouping images within a tenant. Not part of the image identity: two images in different
/// collections still cannot share an id.
/// </param>
/// <param name="IncludeCollectionInPath">
/// Whether <paramref name="Collection"/> also becomes a segment of the url.
/// </param>
/// <param name="AddUniquePrefix">
/// Whether a random segment is inserted before the id, so that the url cannot be guessed from the id.
/// </param>
/// <param name="ThumbnailTargetWidths">
/// The widths to generate thumbnails at, replacing the configured ones for this image alone.
/// Null means the configured widths are used.
/// </param>
public record ImageUploadOptions(
    string? ImageId = null,
    string? Collection = null,
    bool IncludeCollectionInPath = false,
    bool AddUniquePrefix = false,
    IReadOnlyList<int>? ThumbnailTargetWidths = null);
