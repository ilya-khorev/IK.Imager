using System;

namespace IK.Imager.Abstractions
{
    public interface IImageUrlProvider
    {
        Uri GetUri(string imageId);
    }
}