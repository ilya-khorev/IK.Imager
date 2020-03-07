﻿using System;

namespace IK.Imager.Storage.Abstractions.Models
{
    public interface IImageBasicDetails
    {
        /// <summary>
        /// Unique identifier of an image
        /// </summary>
        string Id { get; set; }
        
        /// <summary>
        /// Image size in bytes
        /// </summary>
        int Size { get; set; }
        
        /// <summary>
        /// MD5 hash of an image
        /// </summary>
        string MD5Hash { get; set; }
        
        /// <summary>
        /// Image width in pixels
        /// </summary>
        int Width { get; set; }
        
        /// <summary>
        /// Image height in pixels
        /// </summary>
        int Height { get; set; }
        
        /// <summary>
        /// Date when an image was added to storage
        /// </summary>
        DateTime DateAddedUtc { get; set; }
    }
}