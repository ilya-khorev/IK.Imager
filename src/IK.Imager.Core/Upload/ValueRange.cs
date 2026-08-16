using System;

namespace IK.Imager.Core.Upload;

/// <summary>
/// An inclusive min/max pair, as the ImageLimitations configuration section expresses each threshold.
/// Named ValueRange rather than Range so that it does not shadow <see cref="System.Range"/>.
/// </summary>
public class ValueRange<T> where T : IComparable<T>
{
    /// <summary>
    /// Min value
    /// </summary>
    public T Min { get; set; } = default!;

    /// <summary>
    /// Max value
    /// </summary>
    public T Max { get; set; } = default!;
}
