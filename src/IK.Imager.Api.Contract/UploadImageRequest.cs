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
        public string ImageUrl { get; set; }
    }
}