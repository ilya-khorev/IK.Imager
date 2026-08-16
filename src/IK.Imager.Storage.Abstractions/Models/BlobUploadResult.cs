using System;

namespace IK.Imager.Storage.Abstractions.Models
{
    public class BlobUploadResult
    {
        public string Hash { get; set; } = null!;
        public DateTimeOffset DateAdded { get; set; }
        public Uri Url { get; set; } = null!;
    }
}
