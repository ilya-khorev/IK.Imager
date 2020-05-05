using System.ComponentModel.DataAnnotations;
using IK.Imager.Core.Abstractions.Models;

namespace IK.Imager.Core.Abstractions
{
    public interface IImageValidator
    {
        /// <summary>
        /// Makes sure that the image format fits the system settings
        /// </summary>
        /// <param name="imageFormat"></param>
        /// <exception cref="ValidationException"></exception>
        void CheckFormat(ImageFormat imageFormat);

        /// <summary>
        /// Checks whether the image size fits the system settings
        /// </summary>
        /// <param name="imageSize"></param>
        /// <exception cref="ValidationException"></exception>
        void CheckSize(ImageSize imageSize);
    }
}