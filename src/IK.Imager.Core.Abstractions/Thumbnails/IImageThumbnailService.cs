using System.Collections.Generic;
using System.Threading.Tasks;

namespace IK.Imager.Core.Abstractions.Thumbnails
{
    public interface IImageThumbnailService
    {
        Task CreateThumbnails(string imageId, string imageGroup);
    }
}