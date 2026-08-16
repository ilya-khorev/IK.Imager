#pragma warning disable 1591
namespace IK.Imager.Api.IntegrationEvents;

public class TopicsSettings
{
    public string UploadedImagesTopicName { get; set; } = null!;
    public string DeletedImagesTopicName { get; set; } = null!;
    public string SubscriptionName { get; set; } = null!;
    public int MaxConcurrentCalls { get; set; }
}
