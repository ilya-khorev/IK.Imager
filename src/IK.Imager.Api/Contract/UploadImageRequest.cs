using System.ComponentModel.DataAnnotations;

namespace IK.Imager.Api.Contract
{
    /// <summary>
    /// Model that represent a request for uploading a new image
    /// </summary>
    public class UploadImageRequest: UploadImageRequestBase
    {
        /// <summary>
        /// Absolute image url
        /// </summary>
        [Required]
        public string ImageUrl { get; set; }
    }
}