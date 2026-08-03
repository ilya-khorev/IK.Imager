using Xunit;

namespace IK.Imager.ImageStorage.AzureFiles.Tests;

/// <summary>
/// Keeps every test in this assembly on a single Azurite container.
/// A class fixture would start one container per test class instead.
/// </summary>
[CollectionDefinition(Name)]
public class AzuriteCollection : ICollectionFixture<ImageBlobsStorageFixture>
{
    public const string Name = "Azurite";
}
