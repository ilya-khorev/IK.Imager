using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using IK.Imager.Api.Contract;
using IK.Imager.Core.Abstractions;
using MediatR;

namespace IK.Imager.Api.Commands
{
    public class UploadImageCommandHandler: IRequestHandler<UploadImageCommand, ImageInfo>
    {
        private readonly IImageUploadService _imageUploadService;
        private readonly IMapper _mapper;

        public UploadImageCommandHandler(IImageUploadService imageUploadService, IMapper mapper)
        {
            _imageUploadService = imageUploadService;
            _mapper = mapper;
        }
        
        public async Task<ImageInfo> Handle(UploadImageCommand request, CancellationToken cancellationToken)
        {
            var uploadImageResult = await _imageUploadService.UploadImage(request.ImageStream, request.ImageGroup);
            return _mapper.Map<ImageInfo>(uploadImageResult);
        }
    }
}