using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Repositories;

namespace IK.Imager.Core.Tests.Mocks
{
    public class InMemoryMockedImageBlobRepository: IImageBlobRepository
    {
        private readonly Dictionary<string, MemoryStream> _imagesDictionary = new Dictionary<string, MemoryStream>();
        
        public async Task<UploadImageResult> UploadImage(string imageName, Stream imageStream, ImageSizeType imageSizeType, string contentType,
            CancellationToken cancellationToken)
        {
            imageStream.Position = 0;
            var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0L;
            _imagesDictionary.TryAdd(imageName, memoryStream);
            
            return new UploadImageResult
            {
                Url = GetImageUri(imageName, imageSizeType),
                DateAdded = DateTimeOffset.Now,
                Hash = Guid.NewGuid().ToString()
            };
        }

        public async Task<MemoryStream> DownloadImage(string imageName, ImageSizeType imageSizeType, CancellationToken cancellationToken)
        {
            if (_imagesDictionary.TryGetValue(imageName, out var value))
            {
                MemoryStream outMemoryStream = new MemoryStream();
                await value.CopyToAsync(outMemoryStream, cancellationToken);
                outMemoryStream.Position = 0L;
                return outMemoryStream;
            }

            return null;
        }

        public Task<bool> TryDeleteImage(string imageName, ImageSizeType imageSizeType, CancellationToken cancellationToken)
        {
            if (!_imagesDictionary.ContainsKey(imageName))
                return Task.FromResult(false);

            _imagesDictionary.Remove(imageName);
            return Task.FromResult(true);
        }

        public Uri GetImageUri(string imageName, ImageSizeType imageSizeType)
        {
            return new Uri("http://test.com/" + HttpUtility.UrlEncode(imageSizeType.ToString()) + "/" +
                           HttpUtility.UrlEncode(imageName));
        }

        public Task<bool> ImageExists(string imageName, ImageSizeType imageSizeType, CancellationToken cancellationToken)
        {
            return Task.FromResult(_imagesDictionary.ContainsKey(imageName));
        }
    }
}