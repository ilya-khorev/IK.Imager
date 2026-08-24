using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Abstractions.Upload;
using Microsoft.Extensions.Logging;

namespace IK.Imager.Core.Upload;

public class ImageInspector(ImageValidator imageValidator, ILogger<ImageInspector> logger) : IImageInspector
{
    private const string CouldNotReadImageSize = "The size of the image could not be read.";

    public (ImageFormat Format, ImageSize Size) Inspect(Stream imageStream)
    {
        logger.CheckingImage();

        var imageFormat = ImageFileReader.DetectFormat(imageStream);
        var formatResult = imageValidator.CheckFormat(imageFormat);
        //CheckFormat already reports a null format as invalid; the explicit null test is what tells the compiler so
        if (!formatResult.IsValid || imageFormat == null)
            throw Reject(formatResult);

        logger.ImageFormatDetected(imageFormat.MimeType, imageFormat.ImageType, imageFormat.FileExtension);

        //ReadSize only returns null for a stream ImageSharp cannot identify, which DetectFormat has just ruled out
        var imageSize = ImageFileReader.ReadSize(imageStream);
        if (imageSize == null)
            throw new ValidationException(CouldNotReadImageSize);

        var sizeResult = imageValidator.CheckSize(imageSize);
        if (!sizeResult.IsValid)
            throw Reject(sizeResult);

        logger.ImageSizeRead(imageSize.Width, imageSize.Height, imageSize.Bytes, imageSize.AspectRatio);

        return (imageFormat, imageSize);
    }

    //the keys are a bounded set, so they stay queryable as a log property; the messages are for the 400 body
    private ValidationException Reject(ImageValidationResult validationResult)
    {
        logger.ImageRejected(string.Join(", ", validationResult.ValidationErrors.Select(x => x.Key)));

        return new ValidationException(string.Join(" ", validationResult.ValidationErrors.Select(x => x.ErrorMessage)));
    }
}
