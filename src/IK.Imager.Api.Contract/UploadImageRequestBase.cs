namespace IK.Imager.Api.Contract;

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
    public string ImageGroup { get; set; }
        
    //todo optional image name
}