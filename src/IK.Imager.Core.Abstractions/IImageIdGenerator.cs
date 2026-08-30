namespace IK.Imager.Core.Abstractions;

/// <summary>
/// Mints the random parts of an image url.
///
/// This is the only part of naming an image that is not a pure function of what the caller asked for,
/// which is why it is the only part behind an interface.
/// </summary>
public interface IImageIdGenerator
{
    /// <summary>
    /// A random id, for an upload that did not name one. Long enough that the url it produces cannot be
    /// guessed, which is the only thing keeping a publicly readable blob private.
    /// </summary>
    string NewImageId();

    /// <summary>
    /// A random path segment that makes a url unguessable even when the id is not. This is what lets a
    /// caller keep a readable id, such as a product code, without publishing a url anyone can construct.
    /// </summary>
    string NewUniquePrefix();
}
