using IK.Imager.Core.Abstractions.Models;

namespace IK.Imager.Core.Abstractions.Validation
{
    public interface IImageValidator
    {
        /// <summary>
        /// Makes sure that the image format fits the system settings
        /// </summary>
        /// <param name="imageFormat">A given image format</param>
        /// <returns>validation result</returns>
        ValidationResult CheckFormat(ImageFormat imageFormat);

        /// <summary>
        /// Checks whether the image size fits the the system settings
        /// </summary>
        /// <param name="imageSize">A given image size</param>
        /// <returns>validation result</returns>
        ValidationResult CheckSize(ImageSize imageSize);
    }
}