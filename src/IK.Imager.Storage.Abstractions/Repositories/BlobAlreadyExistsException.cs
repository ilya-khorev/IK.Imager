using System;

namespace IK.Imager.Storage.Abstractions.Repositories
{
    /// <summary>
    /// A blob already exists at that path, and the upload did not allow overwriting.
    ///
    /// Declared here rather than surfacing the storage provider's own conflict type, so that callers can
    /// react to it without referencing Azure.
    /// </summary>
    public class BlobAlreadyExistsException : Exception
    {
        public string BlobPath { get; }

        public BlobAlreadyExistsException(string blobPath, Exception? innerException = null)
            : base($"A blob already exists at '{blobPath}'.", innerException)
        {
            BlobPath = blobPath;
        }
    }
}
