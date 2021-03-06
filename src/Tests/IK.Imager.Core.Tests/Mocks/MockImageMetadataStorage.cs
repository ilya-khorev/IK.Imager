﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IK.Imager.Storage.Abstractions.Models;
using IK.Imager.Storage.Abstractions.Storage;

namespace IK.Imager.Core.Tests.Mocks
{
    public class MockImageMetadataStorage: IImageMetadataStorage
    {
        readonly Dictionary<string, ImageMetadata> _dictionary = new Dictionary<string, ImageMetadata>();
        
        public Task SetMetadata(ImageMetadata metadata, CancellationToken cancellationToken)
        {
            _dictionary.TryAdd(metadata.Id, metadata);
            return Task.CompletedTask;
        }

        public Task<List<ImageMetadata>> GetMetadata(ICollection<string> imageIds, string partitionKey, CancellationToken cancellationToken)
        {
            List<ImageMetadata> result = new List<ImageMetadata>();
            foreach (var imageId in imageIds)
            {
                if (_dictionary.TryGetValue(imageId, out var value))
                    result.Add(value);
            }
            return Task.FromResult(result);
        }

        public Task<List<ImageMetadata>> GetMetadata(ICollection<string> imageIds, CancellationToken cancellationToken)
        {
            return GetMetadata(imageIds, "", cancellationToken);
        }

        public Task<bool> RemoveMetadata(string imageId, string partitionKey, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
}