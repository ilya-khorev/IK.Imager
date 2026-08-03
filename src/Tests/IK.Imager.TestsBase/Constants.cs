namespace IK.Imager.TestsBase
{
    /// <summary>
    /// Shared constants for the container-backed integration tests.
    ///
    /// Connection strings are deliberately not here: the emulators are started by Testcontainers
    /// on randomly mapped host ports, so the connection strings only exist at runtime.
    /// </summary>
    public static class Constants
    {
        public static class AzureBlobStorage
        {
            /// <summary>
            /// Pinned on purpose - 'latest' would silently change the emulated REST surface.
            /// </summary>
            public const string AzuriteImage = "mcr.microsoft.com/azure-storage/azurite:3.36.0";

            public const string ImagesContainerName = "testimages";
            public const string ThumbnailsContainerName = "testimagethumbnails";
        }

        public static class CosmosDb
        {
            /// <summary>
            /// The Linux ("vNext") Cosmos DB emulator - lightweight and fast to start, unlike the
            /// classic emulator image. Pinned to a dated tag so that builds are reproducible.
            /// https://learn.microsoft.com/azure/cosmos-db/emulator-linux
            /// </summary>
            public const string EmulatorImage =
                "mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-EN20260706";

            public const string ContainerId = "TestImageMetadataContainer";
            public const string DatabaseId = "TestImageMetadataDb";

            /// <summary>
            /// Accepted but ignored by the emulator (the offers endpoint is a no-op there).
            /// Kept so that the production code path is exercised as-is.
            /// </summary>
            public const int ContainerThroughPutOnCreation = 1000;
        }
    }
}
