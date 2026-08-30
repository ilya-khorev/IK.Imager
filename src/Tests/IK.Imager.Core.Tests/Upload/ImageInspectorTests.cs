using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using IK.Imager.Core.Tests.Infrastructure;
using IK.Imager.Core.Upload;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace IK.Imager.Core.Tests.Upload;

public class ImageInspectorTests(ITestOutputHelper output)
{
    private const string JpegImagePath = SampleImages.JpegImagesDirectory + "/1043-1200x900.jpg";

    [Fact]
    public void Inspect_SupportedImage_ReturnsFormatAndSize()
    {
        using var fileStream = SampleImages.OpenFileForReading(JpegImagePath);

        var (format, size) = CreateInspector().Inspect(fileStream);

        Assert.Equal("image/jpeg", format.MimeType);
        Assert.Equal(1200, size.Width);
        Assert.Equal(900, size.Height);
    }

    [Fact]
    public void Inspect_NotAnImage_Throws()
    {
        using var fileStream = SampleImages.OpenFileForReading(SampleImages.TextFilePath);

        Assert.Throws<ValidationException>(() => CreateInspector().Inspect(fileStream));
    }

    /// <summary>
    /// The reason the validator computed reaches the caller - it is what the 400 body says.
    /// </summary>
    [Fact]
    public void Inspect_UnsupportedFormat_ExceptionCarriesTheReason()
    {
        using var fileStream = SampleImages.OpenFileForReading(JpegImagePath);
        var inspector = CreateInspector(settings => settings.Types = ["PNG"]);

        var exception = Assert.Throws<ValidationException>(() => inspector.Inspect(fileStream));

        Assert.Contains("Unsupported image format", exception.Message);
    }

    [Fact]
    public void Inspect_ImageOverTheSizeLimit_ExceptionCarriesTheReason()
    {
        using var fileStream = SampleImages.OpenFileForReading(JpegImagePath);
        var inspector = CreateInspector(settings => settings.SizeBytes = new ValueRange<int> { Min = 0, Max = 1000 });

        var exception = Assert.Throws<ValidationException>(() => inspector.Inspect(fileStream));

        Assert.Contains("Image size must be between", exception.Message);
    }

    private ImageInspector CreateInspector(Action<ImageLimitationsSettings>? tighten = null)
    {
        var settings = new ImageLimitationsSettings
        {
            Width = new ValueRange<int> { Min = 10, Max = 2000 },
            Height = new ValueRange<int> { Min = 10, Max = 2000 },
            SizeBytes = new ValueRange<int> { Min = 1, Max = 10000000 },
            AspectRatio = new ValueRange<double> { Min = 0.1, Max = 10 },
            Types = new List<string> { "PNG", "BMP", "JPEG", "GIF" }
        };

        tighten?.Invoke(settings);

        var optionsMock = new Mock<IOptionsSnapshot<ImageLimitationsSettings>>();
        optionsMock.Setup(x => x.Value).Returns(settings);

        return new ImageInspector(new ImageValidator(optionsMock.Object), output.BuildLoggerFor<ImageInspector>());
    }
}
