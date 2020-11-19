using System;
using IK.Imager.Core.Abstractions;

namespace IK.Imager.Core
{
    public class ImageIdentifierProvider : IImageIdentifierProvider
    {
        public string GenerateUniqueId()
        {
            //since all images are publicly available by url, image path must be random and big enough
            //so, for simplicity just concatenating guid and part of another guid
            return (Guid.NewGuid() 
                    + Guid.NewGuid().ToString().Substring(0, 6))
                .Replace("-", "");
        }

        public string GetImageName(string imageId, string extension)
        {
            return string.IsNullOrWhiteSpace(extension) 
                ? imageId 
                : $"{imageId}.{extension}";
        }
    }
}