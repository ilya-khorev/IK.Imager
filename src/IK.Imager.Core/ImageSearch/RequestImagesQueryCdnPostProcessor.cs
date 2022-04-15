using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Abstractions.Models;
using MediatR.Pipeline;

namespace IK.Imager.Core.ImageSearch;

public class RequestImagesQueryCdnPostProcessor: IRequestPostProcessor<RequestImagesQuery, ImagesSearchResult>
{
    private readonly ICdnService _cdnService;

    public RequestImagesQueryCdnPostProcessor(ICdnService cdnService)
    {
        _cdnService = cdnService;
    }
    
    public Task Process(RequestImagesQuery request, ImagesSearchResult response, CancellationToken cancellationToken)
    {
        if (response == null || !response.Images.Any())
            return Task.CompletedTask;

        foreach (var image in response.Images)
        {
            image.Url = _cdnService.TryTransformToCdnUri(image.Url);
            foreach (var thumbnail in image.Thumbnails)
            {
                thumbnail.Url = _cdnService.TryTransformToCdnUri(thumbnail.Url);
            }
        }
        
        return Task.CompletedTask;
    }
}