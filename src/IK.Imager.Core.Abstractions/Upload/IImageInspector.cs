using System.ComponentModel.DataAnnotations;
using System.IO;
using IK.Imager.Core.Abstractions.Models;

namespace IK.Imager.Core.Abstractions.Upload;

/// <summary>
/// Reads what an image is off its stream and checks it against the configured limits.
/// </summary>
public interface IImageInspector
{
    /// <summary>
    /// Reads the format and the size of the image and checks both.
    /// </summary>
    /// <exception cref="ValidationException">The image was rejected. The message carries the reasons.</exception>
    (ImageFormat Format, ImageSize Size) Inspect(Stream imageStream);
}
