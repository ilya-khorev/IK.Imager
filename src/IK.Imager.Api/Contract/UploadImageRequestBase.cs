using System.ComponentModel.DataAnnotations;

namespace IK.Imager.Api.Contract
{
    /// <summary>
    /// Model that represent a request for uploading a new image
    /// </summary>
    public abstract class UploadImageRequestBase
    {
        /// <summary>
        /// Partition key is used to make search requests more efficient
        /// Search requests within one partition will be much faster and less resource intensive 
        /// It's recommended to use meaningful values, such as userId, businessUnitId, or combination of multiple parameters
        /// </summary>
        [Required]
        [StringLength(30, MinimumLength = 3)]
        public string PartitionKey { get; set; }
        
        /// <summary>
        /// Limitations, which prevent to upload a new image
        /// Override the default system limitations
        /// </summary>
        public ImageLimitationSettingsRequest LimitationSettings { get; set; }
    }
}