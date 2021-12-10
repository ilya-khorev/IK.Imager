using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Models;

namespace IK.Imager.Core.Abstractions.ImageRemoving;

public interface IImageDeleteService
{
    Task<ImageShortInfo> DeleteImageMetadata(string imageId, string imageGroup);
        
    Task DeleteImageAndThumbnails(ImageShortInfo imageShortInfo);
}