namespace IK.Imager.Core.Abstractions.Models;

/// <summary>
/// The format a stream was identified as. Read once off the stream and never changed afterwards.
/// </summary>
/// <param name="MimeType">
/// Standard that indicates the nature and format of a file.
/// E.g. 'image/jpeg', 'image/png', 'image/bmp', 'image/gif'
/// </param>
/// <param name="FileExtension">.bmp, jpg, .jpeg, .png, .gif, .bmp, etc.</param>
/// <param name="ImageType">The format as one of the types the system supports.</param>
public record ImageFormat(string MimeType, string FileExtension, ImageType ImageType);
