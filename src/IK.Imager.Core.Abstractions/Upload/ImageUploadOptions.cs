namespace IK.Imager.Core.Abstractions.Upload;

/// <summary>
/// The parts of an upload the caller gets to choose. Grouped rather than passed one by one, because
/// they are all optional and all shape the same thing - where the image ends up and what its url looks
/// like. This is not a command object routed through a handler; the uploader still takes plain arguments.
/// </summary>
/// <param name="Collection">
/// Optional label grouping images within a tenant. Not part of the image identity: two images in
/// different collections still cannot share an id.
/// </param>
public record ImageUploadOptions(string? Collection = null);
