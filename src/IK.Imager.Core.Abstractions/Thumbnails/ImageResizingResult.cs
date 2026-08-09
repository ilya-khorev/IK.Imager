using System.IO;
using IK.Imager.Core.Abstractions.Models;

namespace IK.Imager.Core.Abstractions.Thumbnails;

/// <summary>
/// A resized image and its new size. The caller owns <paramref name="Image"/> and disposes it.
/// </summary>
public record ImageResizingResult(MemoryStream Image, ImageSize Size);
