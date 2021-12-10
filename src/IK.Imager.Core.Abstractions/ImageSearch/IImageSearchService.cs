using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Models;

namespace IK.Imager.Core.Abstractions.ImageSearch;

public interface IImageSearchService
{
    Task<ImagesSearchResult> Search(string[] imageIds, string imageGroup);
}