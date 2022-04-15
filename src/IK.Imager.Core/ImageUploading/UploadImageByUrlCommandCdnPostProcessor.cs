using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Abstractions.ImageUploading;
using IK.Imager.Core.Abstractions.Models;
using MediatR.Pipeline;

namespace IK.Imager.Core.ImageUploading;

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