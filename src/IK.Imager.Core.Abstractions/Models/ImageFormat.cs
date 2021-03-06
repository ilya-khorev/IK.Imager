﻿namespace IK.Imager.Core.Abstractions.Models
{
    public class ImageFormat
    {
        public ImageFormat(string mimeType, string fileExtension, ImageType imageType)
        {
            MimeType = mimeType;
            FileExtension = fileExtension;
            ImageType = imageType;
        }
        
        /// <summary>
        /// .bmp, jpg, .jpeg, .png, .gif, .bmp, etc.
        /// </summary>
        public string FileExtension { get; }
        
        /// <summary>
        /// Standard that indicates the nature and format of a file.
        /// E.g. 'image/jpeg', 'image/png', 'image/bmp', 'image/gif'
        /// </summary>
        public string MimeType { get; }
        
        public ImageType ImageType { get; }

        public override string ToString()
        {
            return $"FileExtension:{FileExtension}, MimeType:{MimeType}, ImageType:{MimeType}";
        }
    }
}