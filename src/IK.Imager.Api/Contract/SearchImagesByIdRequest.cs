using System.ComponentModel.DataAnnotations;

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
        [Required]
        public string[] ImageIds { get; set; }
        
        /// <summary>
        /// Partition key, that was specified when creating these images.
        /// If the images have different partition keys, split this request into several requests with unique partition key
        /// This parameter is optional, however, specifying this value will increase this operation performance.
        /// </summary>
        public string PartitionKey { get; set; }
    }
}