using System;
using System.Collections.Generic;
using Newtonsoft.Json;
// ReSharper disable NonReadonlyMemberInGetHashCode
// ReSharper disable RedundantAssignment

namespace IK.Imager.Storage.Abstractions.Models
{
    public class ImageMetadata: IImageBasicDetails, IEquatable<ImageMetadata>
    {
        public string PartitionKey { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }
        public int Size { get; set; }
        public string MD5Hash { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public DateTime DateAddedUtc { get; set; }

        /// <summary>
        /// Image name. Optional attribute
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Image format, e.g. png, jpg, ...
        /// </summary>
        public string Format { get; set; } 
        
        /// <summary>
        /// Additional information associated with an image in arbitrary form of key-value dictionary
        /// </summary>
        public IDictionary<string, string> Tags { get; set; }
        
        /// <summary>
        /// Thumbnails of an image.
        /// Sorted by dimensions descending, so that the biggest thumbnail is the last element in the array.
        /// Optional property: sometimes an image either doesn't have any thumbnails at all or they are not prepared yet.
        /// </summary>
        public ImageThumbnail[] Thumbnails { get; set; }
        
        /// <summary>
        /// This flag indicates that the image is removed, so that all the related resources should be cleared up asynchronously 
        /// </summary>
        public bool Deleted { get; set; }

        public bool Equals(ImageMetadata other)
        {
            if (ReferenceEquals(null, other)) 
                return false;
            if (ReferenceEquals(this, other))
                return true;
            
            bool primitivePropertiesEqual = PartitionKey == other.PartitionKey 
                   && Id == other.Id
                   && Size == other.Size 
                   && MD5Hash == other.MD5Hash 
                   && Width == other.Width 
                   && Height == other.Height 
                   && DateAddedUtc.Equals(other.DateAddedUtc)
                   && Name == other.Name 
                   && Format == other.Format
                   && Deleted == other.Deleted;

            bool tagsEqual = true;
            if (Tags != null && other.Tags != null && Tags.Count == other.Tags.Count)
            {
                foreach (var tag in Tags)
                {
                    if (!other.Tags.TryGetValue(tag.Key, out var value) || tag.Value != value)
                    {
                        tagsEqual = false;
                        break;
                    }
                }
            }
            else if (Tags == null && other.Tags == null)
                tagsEqual = true;
            else
                tagsEqual = false;

            bool thumbnailsEqual = true;
            if (Thumbnails != null && other.Thumbnails != null && Thumbnails.Length == other.Thumbnails.Length)
            {
                for (var i = 0; i < Thumbnails.Length; i++)
                {
                    var thumbnail = Thumbnails[i];
                    if (!thumbnail.Equals(other.Thumbnails[i]))
                    {
                        thumbnailsEqual = false;
                        break;
                    }
                }
            }
            else if (Thumbnails == null && other.Thumbnails == null)
                thumbnailsEqual = true;
            else
                thumbnailsEqual = false;

            return primitivePropertiesEqual
                   && tagsEqual
                   && thumbnailsEqual;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) 
                return false;
            if (ReferenceEquals(this, obj)) 
                return true;
            
            if (obj.GetType() != GetType())
                return false;
            
            return Equals((ImageMetadata) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = PartitionKey != null ? PartitionKey.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (Id != null ? Id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Size;
                hashCode = (hashCode * 397) ^ (MD5Hash != null ? MD5Hash.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Width;
                hashCode = (hashCode * 397) ^ Height;
                hashCode = (hashCode * 397) ^ DateAddedUtc.GetHashCode();
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Format != null ? Format.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Tags != null ? Tags.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Thumbnails != null ? Thumbnails.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Deleted.GetHashCode();
                return hashCode;
            }
        }
    }
}