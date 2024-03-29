﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;
// ReSharper disable NonReadonlyMemberInGetHashCode
// ReSharper disable RedundantAssignment

namespace IK.Imager.Storage.Abstractions.Models
{
    public class ImageMetadata: IImageBasicDetails, IEquatable<ImageMetadata>
    {
        /// <summary>
        /// Image group which also used to partition data
        /// </summary>
        public string ImageGroup { get; set; }
        
        /// <summary>
        /// Image id
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }
        public long SizeBytes { get; set; }
        public string MD5Hash { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public DateTime DateAddedUtc { get; set; }

        /// <summary>
        /// Image name
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Standard that indicates the nature and format of a file.
        /// E.g. 'image/jpeg', 'image/png', 'image/bmp', 'image/gif'
        /// </summary>
        public string MimeType { get; set; } 
        
        /// <summary>
        /// Image type
        /// </summary>
        public ImageType ImageType { get; set; }
        
        /// <summary>
        /// File extensions, e.g. '.jpeg', '.png', etc
        /// </summary>
        public string FileExtension { get; set; }
        
        /// <summary>
        /// Additional information associated with an image in arbitrary form of key-value dictionary
        /// </summary>
        public IDictionary<string, string> Tags { get; set; }
        
        /// <summary>
        /// Thumbnails of an image.
        /// Sorted by dimensions descending, so that the biggest thumbnail is the last element in the array.
        /// Optional property: sometimes an image either doesn't have any thumbnails at all or they are not prepared yet.
        /// </summary>
        public List<ImageThumbnail> Thumbnails { get; set; }
        
        public bool Equals(ImageMetadata other)
        {
            if (ReferenceEquals(null, other)) 
                return false;
            if (ReferenceEquals(this, other))
                return true;

            bool primitivePropertiesEqual = ImageGroup == other.ImageGroup
                                            && Id == other.Id
                                            && SizeBytes == other.SizeBytes
                                            && MD5Hash == other.MD5Hash
                                            && Width == other.Width
                                            && Height == other.Height
                                            && DateAddedUtc.Equals(other.DateAddedUtc)
                                            && Name == other.Name
                                            && MimeType == other.MimeType
                                            && ImageType == other.ImageType
                                            && FileExtension == other.FileExtension;

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
            if (Thumbnails != null && other.Thumbnails != null && Thumbnails.Count == other.Thumbnails.Count)
            {
                for (var i = 0; i < Thumbnails.Count; i++)
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
                var hashCode = ImageGroup != null ? ImageGroup.GetHashCode() : 0;
                hashCode = (hashCode * 397) ^ (Id != null ? Id.GetHashCode() : 0);
                hashCode = (int) ((hashCode * 397) ^ SizeBytes);
                hashCode = (hashCode * 397) ^ (MD5Hash != null ? MD5Hash.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Width;
                hashCode = (hashCode * 397) ^ Height;
                hashCode = (hashCode * 397) ^ DateAddedUtc.GetHashCode();
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (MimeType != null ? MimeType.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (FileExtension != null ? FileExtension.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Tags != null ? Tags.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Thumbnails != null ? Thumbnails.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) ImageType;

                return hashCode;
            }
        }
    }
}