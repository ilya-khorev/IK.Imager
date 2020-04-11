namespace IK.Imager.TestsBase
{
    public static class Constants
    {
        public const string AzureConnectionString = "UseDevelopmentStorage=true";
        public const string ImagesContainerName = "images";
        public const string ThumbnailsContainerName = "thumbnails";
        public const string CosmosDbConnectionString =
            "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
        public const string ContainerId = "ImageMetadataContainer";
        public const int ContainerThroughPutOnCreation = 1000;
        public const string DatabaseId = "ImageMetadataDb";
    }
}