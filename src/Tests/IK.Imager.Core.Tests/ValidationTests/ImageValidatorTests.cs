using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        public void CheckFormatSucceeded()
        {
            var optionsMock = new Mock<IOptionsSnapshot<ImageLimitationSettings>>();
            optionsMock.Setup(x => x.Value).Returns(new ImageLimitationSettings()
            {
                Types = new List<string> { "PNG", "BMP", "JPEG", "GIF"}
            });
            
            var imageValidator = new ImageValidator(optionsMock.Object);
            imageValidator.CheckFormat(new ImageFormat("image/jpeg", ".jpg", ImageType.JPEG));
        }
        
        [Fact]
        public void ShouldThrowIfFormatIsNull()
        {
            var optionsMock = new Mock<IOptionsSnapshot<ImageLimitationSettings>>();
            optionsMock.Setup(x => x.Value).Returns(new ImageLimitationSettings()
            {
                Types = new List<string> { "PNG", "BMP", "JPEG", "GIF"}
            });
            
            var imageValidator = new ImageValidator(optionsMock.Object);
            Assert.Throws<ValidationException>(() => imageValidator.CheckFormat(null));
        }
        
        [Fact]
        public void ShouldThrowIfFormatIsNotSupported()
        {
            var optionsMock = new Mock<IOptionsSnapshot<ImageLimitationSettings>>();
            optionsMock.Setup(x => x.Value).Returns(new ImageLimitationSettings()
            {
                Types = new List<string> { "PNG"}
            });
            
            var imageValidator = new ImageValidator(optionsMock.Object);
            Assert.Throws<ValidationException>(() => imageValidator.CheckFormat(new ImageFormat("image/jpeg", ".jpg", ImageType.JPEG)));
        }

        [Fact]
        public void CheckSizeSucceeded()
        {
            var imageValidator = new ImageValidator(GetSettings());
            var size = GetValidSize();
            imageValidator.CheckSize(size);
        }
        
        [Fact]
        public void ShouldThrowIfSizeIsGreaterThanThreshold()
        {
            var settings = GetSettings();
            var imageValidator = new ImageValidator(GetSettings());
            var size = GetValidSize();
            size.Bytes = settings.Value.SizeBytes.Max + 1;
            Assert.Throws<ValidationException>(() =>
            {
                imageValidator.CheckSize(size);
            });
        }
        
        [Fact]
        public void ShouldThrowIfSizeIsSmallerThanThreshold()
        {
            var settings = GetSettings();
            var imageValidator = new ImageValidator(GetSettings());
            var size = GetValidSize();
            size.Bytes = settings.Value.SizeBytes.Min - 1;
            Assert.Throws<ValidationException>(() =>
            {
                imageValidator.CheckSize(size);
            });
        }
        
        [Fact]
        public void ShouldThrowIfWidthIsGreaterThanThreshold()
        {
            var settings = GetSettings();
            var imageValidator = new ImageValidator(GetSettings());
            var size = GetValidSize();
            size.Width = settings.Value.Width.Max + 1;
            Assert.Throws<ValidationException>(() =>
            {
                imageValidator.CheckSize(size);
            });
        }
        
        [Fact]
        public void ShouldThrowIfWidthIsSmallerThanThreshold()
        {
            var settings = GetSettings();
            var imageValidator = new ImageValidator(GetSettings());
            var size = GetValidSize();
            size.Width = settings.Value.Width.Min - 1;
            Assert.Throws<ValidationException>(() =>
            {
                imageValidator.CheckSize(size);
            });
        }
        
        [Fact]
        public void ShouldThrowIfHeightIsGreaterThanThreshold()
        {
            var settings = GetSettings();
            var imageValidator = new ImageValidator(GetSettings());
            var size = GetValidSize();
            size.Height = settings.Value.Height.Max + 1;
            Assert.Throws<ValidationException>(() =>
            {
                imageValidator.CheckSize(size);
            });
        }
        
        [Fact]
        public void ShouldThrowIfHeightIsSmallerThanThreshold()
        {
            var settings = GetSettings();
            var imageValidator = new ImageValidator(GetSettings());
            var size = GetValidSize();
            size.Height = settings.Value.Height.Min - 1;
            Assert.Throws<ValidationException>(() =>
            {
                imageValidator.CheckSize(size);
            });
        }
        
        [Fact]
        public void ShouldThrowIfAspectRatioIsSmallerThanThreshold()
        {
            var settings = GetSettings();
            var imageValidator = new ImageValidator(GetSettings());
            var size = GetValidSize();
            
            size.Width = (int)(size.Height * settings.Value.AspectRatio.Min) - 5;
            Assert.Throws<ValidationException>(() =>
            {
                imageValidator.CheckSize(size);
            });
        }
        
        [Fact]
        public void ShouldThrowIfAspectRatioIsGreaterThanThreshold()
        {
            var settings = GetSettings();
            var imageValidator = new ImageValidator(GetSettings());
            var size = GetValidSize();
            
            size.Width = (int)(size.Height * settings.Value.AspectRatio.Max) + 5;
            Assert.Throws<ValidationException>(() =>
            {
                imageValidator.CheckSize(size);
            });
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