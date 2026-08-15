using Xunit;

namespace IK.Imager.Api.Tests.Infrastructure;

/// <summary>
/// One API host and one pair of emulators for the whole assembly - starting the Cosmos emulator per test
/// class would dominate the run. Every test therefore works against shared storage and picks its own image
/// group, so that a lookup can never see another test's images.
/// </summary>
[CollectionDefinition(Name)]
public class ImagerApiCollection : ICollectionFixture<ImagerApiFixture>
{
    public const string Name = "Imager API";
}
