using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Abstractions.ImageUploading;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.ImageSearch;
using IK.Imager.Core.ImageUploading;
using MediatR.Pipeline;

namespace IK.Imager.Core.Cdn;

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
                thumbnail.Url = _cdnService.TryTransformToCdnUri(thumbnail.Url);
        }
        
        return Task.CompletedTask;
    }
}

public class UploadImageByUrlCommandCdnPostProcessor: IRequestPostProcessor<UploadImageByUrlCommand, ImageInfo>
{
    private readonly ICdnService _cdnService;

    public UploadImageByUrlCommandCdnPostProcessor(ICdnService cdnService)
    {
        _cdnService = cdnService;
    }
    
    public Task Process(UploadImageByUrlCommand request, ImageInfo response, CancellationToken cancellationToken)
    {
        response.Url = _cdnService.TryTransformToCdnUri(response.Url);
        return Task.CompletedTask;
    }
}

public class UploadImageCommandCdnPostProcessor: IRequestPostProcessor<UploadImageCommand, ImageInfo>
{
    private readonly ICdnService _cdnService;

    public UploadImageCommandCdnPostProcessor(ICdnService cdnService)
    {
        _cdnService = cdnService;
    }
    
    public Task Process(UploadImageCommand request, ImageInfo response, CancellationToken cancellationToken)
    {
        response.Url = _cdnService.TryTransformToCdnUri(response.Url);
        return Task.CompletedTask;
    }
}