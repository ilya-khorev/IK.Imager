using System.Collections.Generic;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Uploading;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace IK.Imager.Core.Tests.ValidationTests
{
    public class ImageValidatorTests
    {
        [Fact]
        public void CheckFormat_ExpectedImageFormat_Success()
        {
            var optionsMock = new Mock<IOptionsSnapshot<ImageLimitationSettings>>();
            optionsMock.Setup(x => x.Value).Returns(new ImageLimitationSettings()
            {
                Types = new List<string> { "PNG", "BMP", "JPEG", "GIF" }
            });

            var imageValidator = new ImageValidator(optionsMock.Object);
            var validationResult = imageValidator.CheckFormat(new ImageFormat("image/jpeg", ".jpg", ImageType.JPEG));
            Assert.True(validationResult.IsValid);
        }

        [Fact]
        public void CheckFormat_Null_InvalidResult()
        {
            var optionsMock = new Mock<IOptionsSnapshot<ImageLimitationSettings>>();
            optionsMock.Setup(x => x.Value).Returns(new ImageLimitationSettings()
            {
                Types = new List<string> { "PNG", "BMP", "JPEG", "GIF" }
            });

            var imageValidator = new ImageValidator(optionsMock.Object);
            var validationResult = imageValidator.CheckFormat(null);
            Assert.False(validationResult.IsValid);
        }

        [Fact]
        public void CheckFormat_UnsupportedFormat_InvalidResult()
        {
            var optionsMock = new Mock<IOptionsSnapshot<ImageLimitationSettings>>();
            optionsMock.Setup(x => x.Value).Returns(new ImageLimitationSettings()
            {
                Types = new List<string> { "PNG" }
            });

            var imageValidator = new ImageValidator(optionsMock.Object);
            var validationResult = imageValidator.CheckFormat(new ImageFormat("image/jpeg", ".jpg", ImageType.JPEG));
            Assert.False(validationResult.IsValid);
            Assert.Equal(ImageValidator.UnsupportedFormatKey, validationResult.ValidationErrors[0].Key);
        }

        [Fact]
        public void CheckSize_Success()
        {
            var imageValidator = new ImageValidator(GetSettings());
            var size = GetValidSize();
            var validationResult = imageValidator.CheckSize(size);
            Assert.True(validationResult.IsValid);
        }

        [Fact]
        public void CheckSize_SizeIsGreaterThanThreshold_InvalidResult()
        {
            var settings = GetSettings();
            var imageValidator = new ImageValidator(GetSettings());
            var size = GetValidSize() with { Bytes = settings.Value.SizeBytes.Max + 1 };
            var validationResult = imageValidator.CheckSize(size);
            Assert.False(validationResult.IsValid);
            Assert.Equal(ImageValidator.IncorrectSizeKey, validationResult.ValidationErrors[0].Key);
        }

        [Fact]
        public void CheckSize_SizeIsSmallerThanThreshold_InvalidResult()
        {
            var settings = GetSettings();
            var imageValidator = new ImageValidator(GetSettings());
            var size = GetValidSize() with { Bytes = settings.Value.SizeBytes.Min - 1 };
            var validationResult = imageValidator.CheckSize(size);
            Assert.False(validationResult.IsValid);
            Assert.Equal(ImageValidator.IncorrectSizeKey, validationResult.ValidationErrors[0].Key);
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(-1, 0)]
        [InlineData(0, 1)]
        [InlineData(0, -1)]
        [InlineData(-1, -1)]
        [InlineData(1, 1)]
        public void CheckSize_IncorrectDimension_InvalidResult(int diffWidth, int diffHeight)
        {
            var settings = GetSettings();
            var imageValidator = new ImageValidator(GetSettings());
            var validSize = GetValidSize();
            var size = validSize with
            {
                Width = diffWidth switch
                {
                    > 0 => settings.Value.Width.Max + diffWidth,
                    < 0 => settings.Value.Width.Min + diffWidth,
                    _ => validSize.Width
                },
                Height = diffHeight switch
                {
                    > 0 => settings.Value.Height.Max + diffHeight,
                    < 0 => settings.Value.Height.Min + diffHeight,
                    _ => validSize.Height
                }
            };

            var validationResult = imageValidator.CheckSize(size);
            Assert.False(validationResult.IsValid);
            Assert.Equal(ImageValidator.IncorrectDimensionKey, validationResult.ValidationErrors[0].Key);
        }

        [Fact]
        public void CheckSize_AspectRatioIsSmallerThanThreshold_InvalidResult()
        {
            var settings = GetSettings();
            var imageValidator = new ImageValidator(GetSettings());
            var validSize = GetValidSize();
            var size = validSize with { Width = (int)(validSize.Height * settings.Value.AspectRatio.Min) - 5 };
            var validationResult = imageValidator.CheckSize(size);
            Assert.False(validationResult.IsValid);
            Assert.Equal(ImageValidator.IncorrectAspectRatioKey, validationResult.ValidationErrors[0].Key);
        }

        [Fact]
        public void CheckSize_AspectRatioIsGreaterThanThreshold_InvalidResult()
        {
            var settings = GetSettings();
            var imageValidator = new ImageValidator(GetSettings());
            var validSize = GetValidSize();
            var size = validSize with { Width = (int)(validSize.Height * settings.Value.AspectRatio.Max) + 5 };
            var validationResult = imageValidator.CheckSize(size);
            Assert.False(validationResult.IsValid);
            Assert.Equal(ImageValidator.IncorrectAspectRatioKey, validationResult.ValidationErrors[0].Key);
        }

        private ImageSize GetValidSize()
        {
            return new ImageSize(300, 300, 500000);
        }

        private IOptionsSnapshot<ImageLimitationSettings> GetSettings()
        {
            var optionsMock = new Mock<IOptionsSnapshot<ImageLimitationSettings>>();
            optionsMock.Setup(x => x.Value).Returns(new ImageLimitationSettings()
            {
                Height = new Range<int>
                {
                    Min = 10,
                    Max = 1000
                },
                Width = new Range<int>
                {
                    Min = 10,
                    Max = 1000
                },
                SizeBytes = new Range<int>
                {
                    Min = 1000,
                    Max = 10000000
                },
                AspectRatio = new Range<double>()
                {
                    Min = 0.5,
                    Max = 2
                }
            });

            return optionsMock.Object;
        }
    }
}
