using System.Collections.Generic;
using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Models;

namespace IK.Imager.Storage.Abstractions.Storage
{
    public interface IImageMetadataStorage
    {
        Task SetMetadata(ImageMetadata metadata);
        Task<ImageMetadata> GetMetadata(ICollection<string> imageIds);
        Task SetMetadataForRemoval(string imageId);
        Task RemoveMetadata(string imageId);
    }
}