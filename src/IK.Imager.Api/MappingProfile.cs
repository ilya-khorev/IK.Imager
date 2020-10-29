using AutoMapper;
using IK.Imager.Core.Abstractions.Models;

namespace IK.Imager.Api
{
    /// <inheritdoc />
    public class MappingProfile : Profile
    {
        /// <inheritdoc />
        public MappingProfile()
        {
            CreateMap<ImageInfo, IK.Imager.Api.Contract.ImageInfo>();
            CreateMap<ImageFullInfoWithThumbnails, IK.Imager.Api.Contract.ImageFullInfoWithThumbnails>();
            CreateMap<ImagesSearchResult, IK.Imager.Api.Contract.ImagesSearchResult>();
        }
    }
}