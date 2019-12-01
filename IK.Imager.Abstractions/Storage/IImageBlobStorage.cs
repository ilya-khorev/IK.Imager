using System.IO;
using System.Threading.Tasks;

namespace IK.Imager.Abstractions.Storage
{
    public interface IImageBlobStorage
    {
        Task UploadImage(Stream imageStream);
        Task UploadImage(string id, Stream imageStream);
        Task<Stream> DownloadImage(string id);
        Task<bool> DeleteImage(string id);
    }
}