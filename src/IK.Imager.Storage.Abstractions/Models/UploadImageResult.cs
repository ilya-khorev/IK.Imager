using System;

namespace IK.Imager.Storage.Abstractions.Models
{
    public class UploadImageResult
    {
        public string MD5Hash { get; set; }
        public DateTimeOffset DateAdded { get; set; }
        public Uri Url { get; set; }
    }
}