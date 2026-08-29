using System;

namespace IK.Imager.Storage.Abstractions.Repositories
{
    /// <summary>
    /// An image with the same id already exists in the same tenant.
    ///
    /// Declared here rather than surfacing the storage provider's own conflict type, so that
    /// the core services and the host can react to it without referencing Cosmos DB.
    /// </summary>
    public class ImageAlreadyExistsException : Exception
    {
        public string TenantId { get; }
        public string ImageId { get; }

        public ImageAlreadyExistsException(string tenantId, string imageId, Exception? innerException = null)
            : base($"Image '{imageId}' already exists.", innerException)
        {
            TenantId = tenantId;
            ImageId = imageId;
        }
    }
}
