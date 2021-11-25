namespace IK.Imager.Api.Contract
{
    /// <summary>
    /// Model with identifiers needed to search for images
    /// </summary>
    public class SearchImagesByIdRequest
    {
        /// <summary>
        /// Image identifiers to search.
        /// Maximum 100 identifiers are allowed within one request.
        /// </summary>
        //todo [Required]
        public string[] ImageIds { get; set; }
        
        /// <summary>
        /// Image group, which was specified when uploading these images.
        /// 
        /// If the images belong to different image groups, you may split this request into several requests with unique image group.
        /// This parameter is optional. However, specifying this value will increase this operation performance.
        /// </summary>
        public string ImageGroup { get; set; }
    }
}