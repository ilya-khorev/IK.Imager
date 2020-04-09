using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace IK.Imager.Api.Contract
{
    /// <summary>
    /// Model that represent a request for uploading a new image
    /// </summary>
    public class UploadImageFileRequest 
    {
        /// <summary>
        /// File sent as a part of form
        /// </summary>
        public IFormFile File { get; set; }
        
        /// <summary>
        /// Partition key is used to make search requests more efficient
        /// Search requests within one partition will be much faster and less resource intensive 
        /// It's recommended to use meaningful values, such as userId, businessUnitId, or combination of multiple parameters
        /// </summary>
        [Required]
        [StringLength(30, MinimumLength = 1)]
        public string PartitionKey { get; set; }
    }
}