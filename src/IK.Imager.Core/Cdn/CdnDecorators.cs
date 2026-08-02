using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Abstractions.Messaging;
using IK.Imager.Core.Abstractions.Models;
using IK.Imager.Core.ImageSearch;
using IK.Imager.Core.ImageUploading;

namespace IK.Imager.Core.Cdn;

/// <summary>
/// Handlers always return the raw blob url. These decorators are the single place where the blob host
/// is swapped for the CDN host, so a new response needing url rewriting gets a decorator here
/// rather than a change inside a handler.
/// </summary>
public class RequestImagesQueryCdnDecorator : IQueryHandler<RequestImagesQuery, ImagesSearchResult>
{
    private readonly IQueryHandler<RequestImagesQuery, ImagesSearchResult> _inner;
    private readonly ICdnService _cdnService;

    public RequestImagesQueryCdnDecorator(IQueryHandler<RequestImagesQuery, ImagesSearchResult> inner, ICdnService cdnService)
    {
        _inner = inner;
        _cdnService = cdnService;
    }

    public async Task<ImagesSearchResult> Handle(RequestImagesQuery query, CancellationToken cancellationToken)
    {
        var response = await _inner.Handle(query, cancellationToken);

        if (response == null || !response.Images.Any())
            return response;

        foreach (var image in response.Images)
        {
            image.Url = _cdnService.TryTransformToCdnUri(image.Url);
            foreach (var thumbnail in image.Thumbnails)
                thumbnail.Url = _cdnService.TryTransformToCdnUri(thumbnail.Url);
        }

        return response;
    }
}

public class UploadImageByUrlCommandCdnDecorator : ICommandHandler<UploadImageByUrlCommand, ImageInfo>
{
    private readonly ICommandHandler<UploadImageByUrlCommand, ImageInfo> _inner;
    private readonly ICdnService _cdnService;

    public UploadImageByUrlCommandCdnDecorator(ICommandHandler<UploadImageByUrlCommand, ImageInfo> inner, ICdnService cdnService)
    {
        _inner = inner;
        _cdnService = cdnService;
    }

    public async Task<ImageInfo> Handle(UploadImageByUrlCommand command, CancellationToken cancellationToken)
    {
        var response = await _inner.Handle(command, cancellationToken);
        if (response != null)
            response.Url = _cdnService.TryTransformToCdnUri(response.Url);

        return response;
    }
}

public class UploadImageCommandCdnDecorator : ICommandHandler<UploadImageCommand, ImageInfo>
{
    private readonly ICommandHandler<UploadImageCommand, ImageInfo> _inner;
    private readonly ICdnService _cdnService;

    public UploadImageCommandCdnDecorator(ICommandHandler<UploadImageCommand, ImageInfo> inner, ICdnService cdnService)
    {
        _inner = inner;
        _cdnService = cdnService;
    }

    public async Task<ImageInfo> Handle(UploadImageCommand command, CancellationToken cancellationToken)
    {
        var response = await _inner.Handle(command, cancellationToken);
        if (response != null)
            response.Url = _cdnService.TryTransformToCdnUri(response.Url);

        return response;
    }
}
