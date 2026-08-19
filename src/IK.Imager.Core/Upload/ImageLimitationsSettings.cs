using System;
using System.Collections.Generic;
using System.Linq;

namespace IK.Imager.Core.Upload;

/// <summary>
/// Image threshold values
/// </summary>
public class ImageLimitationsSettings
{
    /// <summary>
    /// Max and min image width in pixels 
    /// </summary>
    public ValueRange<int> Width { get; set; } = null!;

    /// <summary>
    /// Max and min image height in pixels
    /// </summary>
    public ValueRange<int> Height { get; set; } = null!;

    /// <summary>
    /// Max and min image size in bytes
    /// </summary>
    public ValueRange<int> SizeBytes { get; set; } = null!;

    /// <summary>
    /// Max and min image aspect ratio
    /// Aspect ratio - the ratio of its width to its height
    /// </summary>
    public ValueRange<double> AspectRatio { get; set; } = null!;

    /// <summary>
    /// Supported image types
    /// </summary>
    public List<string> Types { get; set; } = null!;

    public void MergeWith(ImageLimitationsSettings settings)
    {
        if (settings == null)
            return;

        if (settings.Height != null)
        {
            Height = new ValueRange<int>
            {
                Min = settings.Height.Min,
                Max = settings.Height.Max
            };
        }

        if (settings.Width != null)
        {
            Width = new ValueRange<int>
            {
                Min = settings.Width.Min,
                Max = settings.Width.Max
            };
        }

        if (settings.SizeBytes != null)
        {
            SizeBytes = new ValueRange<int>
            {
                Min = settings.SizeBytes.Min,
                Max = settings.SizeBytes.Max
            };
        }

        if (settings.AspectRatio != null)
        {
            AspectRatio = new ValueRange<double>
            {
                Min = settings.AspectRatio.Min,
                Max = settings.AspectRatio.Max
            };
        }

        if (settings.Types != null)
            Types = settings.Types.ToList();
    }
}
