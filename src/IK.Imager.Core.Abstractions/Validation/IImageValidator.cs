using System.ComponentModel.DataAnnotations;
using IK.Imager.Core.Abstractions.Models;

namespace IK.Imager.Core.Abstractions.Validation
{
    public interface IImageValidator
    {
        /// <summary>
        /// Makes sure that the image format fits the system settings
        /// </summary>
        /// <param name="imageFormat">A given image format</param>
        /// <exception cref="ValidationException">Throws when a give image format does not meet system requirements</exception>
        void CheckFormat(ImageFormat imageFormat);

        /// <summary>
        /// Checks whether the image size fits the the system settings
        /// </summary>
        /// <param name="imageSize">A given image size</param>
        /// <exception cref="ValidationException">Throws when a give image size does not meet system requirements</exception>
        void CheckSize(ImageSize imageSize);
    }
}