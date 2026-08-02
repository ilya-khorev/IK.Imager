#pragma warning disable 1591

namespace IK.Imager.Core.ImageUploading;

public record UploadImageByUrlCommand(string ImageUrl, string ImageGroup);