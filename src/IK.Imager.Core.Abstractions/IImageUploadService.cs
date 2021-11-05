using System.IO;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Models;

namespace IK.Imager.Core.Abstractions
{
    public interface IImageUploadService
    {
        Task<ImageInfo> UploadImage(Stream imageStream, string imageGroup);
    }
}