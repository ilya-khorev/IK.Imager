namespace IK.Imager.Core.Abstractions.Models;

/// <summary>
/// The dimensions and weight of an image, read once off the stream.
/// </summary>
public record ImageSize(int Width, int Height, long Bytes)
{
    /// <summary>
    /// The ratio of the width to the height.
    /// </summary>
    public double AspectRatio => (double)Width / Height;

    //the generated ToString would not mention AspectRatio, and this one is written into the logs
    public override string ToString()
    {
        return $"Width:{Width}, Height:{Height}, Bytes:{Bytes}, AspectRatio:{AspectRatio}";
    }
}
