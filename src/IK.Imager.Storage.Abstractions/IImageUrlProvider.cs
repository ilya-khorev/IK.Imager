using System;

namespace IK.Imager.Storage.Abstractions
{
    public interface IImageUrlProvider
    {
        Uri GetUri(string imageId);
    }
}