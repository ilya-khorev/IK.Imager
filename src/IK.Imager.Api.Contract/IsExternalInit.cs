// ReSharper disable once CheckNamespace
namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// The compiler emits a reference to this type for every <c>init</c> accessor, and .NET only ships it
    /// from .NET 5 onwards - this project is netstandard2.1 so that clients on any runtime can reference it,
    /// and therefore has to declare it. Internal, so it never collides with the real one in a consumer.
    /// </summary>
    internal static class IsExternalInit
    {
    }
}
