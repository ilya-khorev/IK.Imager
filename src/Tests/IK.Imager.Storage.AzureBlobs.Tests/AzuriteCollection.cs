using Xunit;

namespace IK.Imager.Storage.AzureBlobs.Tests;

/// <summary>
/// Keeps every test in this assembly on a single Azurite container.
/// A class fixture would start one container per test class instead.
/// </summary>
[CollectionDefinition(Name)]
public class AzuriteCollection : ICollectionFixture<AzureBlobStorageFixture>
{
    public const string Name = "Azurite";
}
