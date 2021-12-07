using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using IK.Imager.Api.Contract;
using IK.Imager.Core.Abstractions;
using MediatR;
#pragma warning disable 1591

namespace IK.Imager.Api.Queries
{
    public class RequestImagesQueryHandler: IRequestHandler<RequestImagesQuery, ImagesSearchResult>
    {
        private readonly IImageSearchService _imageSearchService;
        private readonly IMapper _mapper;

        public RequestImagesQueryHandler(IImageSearchService imageSearchService, IMapper mapper)
        {
            _imageSearchService = imageSearchService;
            _mapper = mapper;
        }
        
        public async Task<ImagesSearchResult> Handle(RequestImagesQuery request, CancellationToken cancellationToken)
        {
            var searchResult = await _imageSearchService.Search(request.ImageIds,
                request.ImageGroup);

            return _mapper.Map<ImagesSearchResult>(searchResult);
        }
    }
}