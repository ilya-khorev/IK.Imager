using System;
// ReSharper disable NonReadonlyMemberInGetHashCode

namespace IK.Imager.Storage.Abstractions.Models
{
    public class ImageThumbnail: IImageBasicDetails, IEquatable<ImageThumbnail>
    {
        public string Id { get; set; }
        public int Size { get; set; }
        public string MD5Hash { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public DateTime DateAddedUtc { get; set; }

        public bool Equals(ImageThumbnail other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other)) 
                return true;

            return Id == other.Id
                   && Size == other.Size
                   && MD5Hash == other.MD5Hash
                   && Width == other.Width
                   && Height == other.Height;
            // && DateAddedUtc.Equals(other.DateAddedUtc);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) 
                return false;
            if (ReferenceEquals(this, obj)) 
                return true;
            if (obj.GetType() != GetType()) 
                return false;
            return Equals((ImageThumbnail) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id != null ? Id.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ Size;
                hashCode = (hashCode * 397) ^ (MD5Hash != null ? MD5Hash.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Width;
                hashCode = (hashCode * 397) ^ Height;
                hashCode = (hashCode * 397) ^ DateAddedUtc.GetHashCode();
                return hashCode;
            }
        }
    }
}