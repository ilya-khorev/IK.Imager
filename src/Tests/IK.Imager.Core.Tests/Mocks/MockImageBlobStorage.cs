using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Storage;

namespace IK.Imager.Core.Tests.Mocks
{
    public class MockImageBlobStorage: IImageBlobStorage
    {
        private readonly Dictionary<string, MemoryStream> _streamsDictionary = new Dictionary<string, MemoryStream>();
        
        public async Task<UploadImageResult> UploadImage(string imageName, Stream imageStream, ImageSizeType imageSizeType, string contentType,
            CancellationToken cancellationToken)
        {
            var memoryStream = new MemoryStream();
            await imageStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;
            _streamsDictionary.TryAdd(imageName, memoryStream);
            
            return new UploadImageResult
            {
                Url = new Uri("http://test.com"),
                DateAdded = DateTimeOffset.Now,
                MD5Hash = Guid.NewGuid().ToString()
            };
        }

        public async Task<MemoryStream> DownloadImage(string imageName, ImageSizeType imageSizeType, CancellationToken cancellationToken)
        {
            if (_streamsDictionary.TryGetValue(imageName, out var value))
            {
                MemoryStream outMemoryStream = new MemoryStream();
                await value.CopyToAsync(outMemoryStream, cancellationToken);
                outMemoryStream.Position = 0;
                return outMemoryStream;
            }

            return null;
        }

        public Task<bool> TryDeleteImage(string imageName, ImageSizeType imageSizeType, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Uri GetImageUri(string imageName, ImageSizeType imageSizeType)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ImageExists(string imageName, ImageSizeType imageSizeType, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}