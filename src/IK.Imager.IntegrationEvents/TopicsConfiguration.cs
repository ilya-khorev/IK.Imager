namespace IK.Imager.IntegrationEvents
{
    public class TopicsConfiguration
    {
        public string UploadedImagesTopicName { get; set; }
        public string DeletedImagesTopicName { get; set; }
        public string SubscriptionName { get; set; }
        public int MaxConcurrentCalls { get; set; }
    }
}