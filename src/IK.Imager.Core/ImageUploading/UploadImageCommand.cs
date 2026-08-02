using System.IO;

#pragma warning disable 1591

namespace IK.Imager.Core.ImageUploading;

public record UploadImageCommand(Stream ImageStream, string ImageGroup);