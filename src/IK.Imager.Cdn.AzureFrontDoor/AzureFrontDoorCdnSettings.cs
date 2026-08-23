namespace IK.Imager.Cdn.AzureFrontDoor;

public class AzureFrontDoorCdnSettings
{
    /// <summary>
    /// Id of the subscription the Front Door profile lives in.
    /// </summary>
    public string SubscriptionId { get; set; } = null!;

    /// <summary>
    /// Resource group of the Front Door profile.
    /// </summary>
    public string ResourceGroupName { get; set; } = null!;

    /// <summary>
    /// Name of the Front Door profile.
    /// </summary>
    public string ProfileName { get; set; } = null!;

    /// <summary>
    /// Name of the endpoint within the profile.
    /// </summary>
    public string EndpointName { get; set; } = null!;
}
