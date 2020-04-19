using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Models;

namespace IK.Imager.Core.Abstractions.Services
{
    public interface IImageDeleteService
    {
        Task<ImageShortInfo> DeleteOriginalImageMetadata(string imageId, string partitionKey);
        
        Task DeleteImagesWithMetadata(ImageShortInfo imageShortInfo);
    }
}