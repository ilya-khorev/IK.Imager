using Xunit;

namespace IK.Imager.Storage.CosmosDb.Tests;

/// <summary>
/// Keeps every test in this assembly on a single Cosmos DB emulator container.
/// A class fixture would start one emulator per test class instead, and the emulator is by far
/// the most expensive thing in this test run.
/// </summary>
[CollectionDefinition(Name)]
public class CosmosDbCollection : ICollectionFixture<CosmosDbFixture>
{
    public const string Name = "CosmosDb";
}
