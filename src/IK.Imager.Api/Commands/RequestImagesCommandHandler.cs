using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using IK.Imager.Api.Contract;
using IK.Imager.Core.Abstractions;
using MediatR;

namespace IK.Imager.Api.Commands
{
    public class RequestImagesCommandHandler: IRequestHandler<RequestImagesCommand, ImagesSearchResult>
    {
        private readonly IImageSearchService _imageSearchService;
        private readonly IMapper _mapper;

        public RequestImagesCommandHandler(IImageSearchService imageSearchService, IMapper mapper)
        {
            _imageSearchService = imageSearchService;
            _mapper = mapper;
        }
        
        public async Task<ImagesSearchResult> Handle(RequestImagesCommand request, CancellationToken cancellationToken)
        {
            var searchResult = await _imageSearchService.Search(request.ImageIds,
                request.ImageGroup);

            return _mapper.Map<ImagesSearchResult>(searchResult);
        }
    }
}