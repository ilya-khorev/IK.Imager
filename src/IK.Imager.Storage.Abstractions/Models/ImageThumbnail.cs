using System;
// ReSharper disable NonReadonlyMemberInGetHashCode

namespace IK.Imager.Storage.Abstractions.Models
{
    public class ImageThumbnail : IStoredImage, IEquatable<ImageThumbnail>
    {
        public string Id { get; set; } = null!;
        public string BlobPath { get; set; } = null!;
        public long SizeBytes { get; set; }
        public string MD5Hash { get; set; } = null!;
        public int Width { get; set; }
        public int Height { get; set; }
        public DateTime DateAddedUtc { get; set; }
        public string MimeType { get; set; } = null!;

        public bool Equals(ImageThumbnail? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;

            return Id == other.Id
                   && SizeBytes == other.SizeBytes
                   && MD5Hash == other.MD5Hash
                   && Width == other.Width
                   && Height == other.Height
                   && BlobPath == other.BlobPath
                   && MimeType == other.MimeType
                   && DateAddedUtc.Equals(other.DateAddedUtc);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((ImageThumbnail)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id != null ? Id.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (BlobPath != null ? BlobPath.GetHashCode() : 0);
                hashCode = (int)((hashCode * 397) ^ SizeBytes);
                hashCode = (hashCode * 397) ^ (MD5Hash != null ? MD5Hash.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (MimeType != null ? MimeType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Width;
                hashCode = (hashCode * 397) ^ Height;
                hashCode = (hashCode * 397) ^ DateAddedUtc.GetHashCode();
                return hashCode;
            }
        }
    }
}
