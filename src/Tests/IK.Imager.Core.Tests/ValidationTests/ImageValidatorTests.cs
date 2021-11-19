using System;
using System.Collections.Generic;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.Settings;
using IK.Imager.Core.Validation;
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
                Types = new List<string> { "PNG", "BMP", "JPEG", "GIF"}
            });
            
            var imageValidator = new ImageValidator(optionsMock.Object);
            var validationResult = imageValidator.CheckFormat(new ImageFormat("image/jpeg", ".jpg", ImageType.JPEG));
            Assert.True(validationResult.IsValid);
        }
        
        [Fact]
        public void CheckFormat_NullArgumentException()
        {
            var optionsMock = new Mock<IOptionsSnapshot<ImageLimitationSettings>>();
            optionsMock.Setup(x => x.Value).Returns(new ImageLimitationSettings()
            {
                Types = new List<string> { "PNG", "BMP", "JPEG", "GIF"}
            });
            
            var imageValidator = new ImageValidator(optionsMock.Object);
            Assert.Throws<ArgumentNullException>(() => imageValidator.CheckFormat(null));
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
            var size = GetValidSize();
            size.Bytes = settings.Value.SizeBytes.Max + 1;
            var validationResult = imageValidator.CheckSize(size);
            Assert.False(validationResult.IsValid);
            Assert.Equal(ImageValidator.IncorrectSizeKey, validationResult.ValidationErrors[0].Key);
        }
        
        [Fact]
        public void CheckSize_SizeIsSmallerThanThreshold_InvalidResult()
        {
            var settings = GetSettings();
            var imageValidator = new ImageValidator(GetSettings());
            var size = GetValidSize();
            size.Bytes = settings.Value.SizeBytes.Min - 1;
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
            var size = GetValidSize();
            size.Width = diffWidth switch
            {
                > 0 => settings.Value.Width.Max + diffWidth,
                < 0 => settings.Value.Width.Min + diffWidth,
                _ => size.Width
            };

            size.Height = diffHeight switch
            {
                > 0 => settings.Value.Height.Max + diffHeight,
                < 0 => settings.Value.Height.Min + diffHeight,
                _ => size.Height
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
            var size = GetValidSize();
            
            size.Width = (int)(size.Height * settings.Value.AspectRatio.Min) - 5;
            var validationResult = imageValidator.CheckSize(size);
            Assert.False(validationResult.IsValid);
            Assert.Equal(ImageValidator.IncorrectAspectRatioKey, validationResult.ValidationErrors[0].Key);
        }
        
        [Fact]
        public void CheckSize_AspectRatioIsGreaterThanThreshold_InvalidResult()
        {
            var settings = GetSettings();
            var imageValidator = new ImageValidator(GetSettings());
            var size = GetValidSize();
            
            size.Width = (int)(size.Height * settings.Value.AspectRatio.Max) + 5;
            var validationResult = imageValidator.CheckSize(size);
            Assert.False(validationResult.IsValid);
            Assert.Equal(ImageValidator.IncorrectAspectRatioKey, validationResult.ValidationErrors[0].Key);
        }

        private ImageSize GetValidSize()
        {
            return new ImageSize
            {
                Bytes = 500000,
                Height = 300,
                Width = 300
            };
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