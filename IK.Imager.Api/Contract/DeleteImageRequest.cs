using System.ComponentModel.DataAnnotations;

namespace IK.Imager.Api.Contract
{
    /// <summary>
    /// Model with identifiers needed to remove an image
    /// </summary>
    public class DeleteImageRequest
    {
        /// <summary>
        /// Image identifier
        /// </summary>
        [Required]
        public string ImageId { get; set; }
        
        /// <summary>
        /// Partition key, that was specified when creating the image.
        /// This parameter is optional, however, specifying this value will increase this operation performance.
        /// </summary>
        public string PartitionKey { get; set; }
    }
}