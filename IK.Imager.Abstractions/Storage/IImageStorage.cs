using System.IO;
using System.Threading.Tasks;

namespace IK.Imager.Abstractions.Storage
{
    /// <summary>
    /// Represents a set of methods for
    /// 1) uploading and storing a new image
    /// 2) downloading, or removing the previously saved images
    /// </summary>
    public interface IImageStorage
    {
        /// <summary>
        /// Upload and save a new image.
        /// Image identifier is generated and returned as a result of this method.
        /// </summary>
        /// <param name="imageStream">image stream</param>
        /// <returns>image identifier</returns>
        Task<string> UploadImage(Stream imageStream);
        
        /// <summary>
        /// Upload and save a new image
        /// </summary>
        /// <param name="id">unique identifier of an image</param>
        /// <param name="imageStream">image stream</param>
        /// <returns></returns>
        Task UploadImage(string id, Stream imageStream);
        
        /// <summary>
        /// Download an image stream for a given image id
        /// </summary>
        /// <param name="id">image identifier</param>
        /// <returns>image stream or null if such image was not found</returns>
        Task<Stream> DownloadImage(string id);
        
        /// <summary>
        /// Attempt to delete an image by a given image id 
        /// </summary>
        /// <param name="id">image identifier</param>
        /// <returns>Returns true if an image was found and removed.
        /// Returns false if an image was not found.</returns>
        Task<bool> TryDeleteImage(string id);
    }
}