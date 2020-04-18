using System;
using IK.Imager.Core.Abstractions;

namespace IK.Imager.Core
{
    public class ImageIdentifierProvider: IImageIdentifierProvider
    {
        public string GenerateUniqueId()
        {
            //since all images are publicly available by url, image path must be random and big enough
            //for simplicity just concatenating 2 system guids
            return (Guid.NewGuid() + Guid.NewGuid().ToString()).Replace("-", "");
        }

        public string GetImageName(string imageId, string extension)
        {
            return imageId + extension;
        }
    }
}