namespace IK.Imager.TestsBase
{
    public static class Constants
    {
        public static class AzureBlobStorage
        {
            public const string ConnectionString = "UseDevelopmentStorage=true";
            public const string ImagesContainerName = "testimages";
            public const string ThumbnailsContainerName = "testimagethumbnails";
        }

        public static class CosmosDb
        {
            public const string ConnectionString =
                "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
            public const string ContainerId = "TestImageMetadataContainer";
            public const int ContainerThroughPutOnCreation = 1000;
            public const string DatabaseId = "TestImageMetadataDb";
        }
    }
}