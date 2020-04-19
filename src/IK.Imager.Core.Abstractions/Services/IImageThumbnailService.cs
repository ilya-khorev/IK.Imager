using System.Collections.Generic;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Models;

namespace IK.Imager.Core.Abstractions.Services
{
    public interface IImageThumbnailService
    {
        Task<List<ImageThumbnailGeneratingResult>> GenerateThumbnails(string imageId, string partitionKey);
    }
}