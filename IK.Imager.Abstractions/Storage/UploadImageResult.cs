using System;

namespace IK.Imager.Abstractions.Storage
{
    public class UploadImageResult
    {
        public string Id { get; set; }
        public string MD5Hash { get; set; }
        public DateTimeOffset DateAdded { get; set; }
    }
}