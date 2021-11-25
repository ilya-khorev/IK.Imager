namespace IK.Imager.Api.Contract
{
    /// <summary>
    /// Model that represent a request for uploading a new image
    /// </summary>
    public abstract class UploadImageRequestBase
    {
        /// <summary>
        /// Image group represents a logical group to which this image belong.
        /// It's recommended to use meaningful values, such as userId, businessUnitId, or combination of multiple parameters.
        /// For example, "user_1435", "unit_48", "products_store_11"
        /// 
        /// Image group is also used as partition to evenly spread data, and to make search requests more efficient.
        /// </summary>
        //todo [Required]
        //todo[StringLength(30, MinimumLength = 3)]
        public string ImageGroup { get; set; }
        
        //todo optional image name
        
        /// <summary>
        /// Limitations, which prevent to upload a new image if the actual image parameters do not meet these values.
        /// Setting specified here override the default system limitations.
        /// If the nested object, representing a particular parameter (e.g. height) is passed as null,
        /// the system keeps default limitation settings of this parameter.
        /// </summary>
        public ImageLimitationSettingsRequest LimitationSettings { get; set; }
    }
}