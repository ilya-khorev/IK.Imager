using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.ImageSearch;
using IK.Imager.Core.Abstractions.Models;
using MediatR;

#pragma warning disable 1591

namespace IK.Imager.Core.ImageSearch;

public class RequestImagesQueryHandler: IRequestHandler<RequestImagesQuery, ImagesSearchResult>
{
    private readonly IImageSearchService _imageSearchService;

    public RequestImagesQueryHandler(IImageSearchService imageSearchService)
    {
        _imageSearchService = imageSearchService;
    }
        
    public async Task<ImagesSearchResult> Handle(RequestImagesQuery request, CancellationToken cancellationToken)
    {
        var searchResult = await _imageSearchService.Search(request.ImageIds, request.ImageGroup);

        return searchResult;
    }
}