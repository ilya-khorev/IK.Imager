#pragma warning disable CS1591
namespace IK.Imager.Api.IntegrationEvents.Events;

/// <summary>
/// Raised once the blobs are gone, so that anything caching them can drop its copies.
/// Carries names rather than urls: the url of an image depends on configuration that may have changed
/// between publishing and consuming.
/// </summary>
public record ImageFilesDeletedIntegrationEvent(string ImageId, string ImageName, string[] ThumbnailNames);
