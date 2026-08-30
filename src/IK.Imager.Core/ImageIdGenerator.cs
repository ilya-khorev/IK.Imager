using System.Security.Cryptography;
using IK.Imager.Core.Abstractions;

namespace IK.Imager.Core;

/// <inheritdoc cref="IImageIdGenerator" />
public class ImageIdGenerator : IImageIdGenerator
{
    /// <summary>
    /// 128 bits. Both values are the only access control there is on a publicly readable blob, so both
    /// are the same strength.
    /// </summary>
    private const int HexLength = 32;

    /// <inheritdoc />
    public string NewImageId() => RandomHex();

    /// <inheritdoc />
    public string NewUniquePrefix() => RandomHex();

    //RandomNumberGenerator rather than Guid, because being unguessable is the whole job of these values and
    //that should be visible in the code rather than inferred from how Guid.NewGuid happens to be implemented
    private static string RandomHex() => RandomNumberGenerator.GetHexString(HexLength, lowercase: true);
}
