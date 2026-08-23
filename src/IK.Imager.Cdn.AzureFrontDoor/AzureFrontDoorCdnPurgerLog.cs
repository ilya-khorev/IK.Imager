using Microsoft.Extensions.Logging;

namespace IK.Imager.Cdn.AzureFrontDoor;

internal static partial class AzureFrontDoorCdnPurgerLog
{
    [LoggerMessage(EventId = 4100, Level = LogLevel.Information,
        Message = "Front Door accepted a purge of {UriCount} path(s) on endpoint {EndpointName}.")]
    public static partial void Purged(this ILogger logger, int uriCount, string endpointName);

    //a failed submission surfaces as an exception out of the Azure SDK, which MassTransit logs; this is the
    //one failure the purger itself decides on
    [LoggerMessage(EventId = 4101, Level = LogLevel.Error,
        Message = "Refusing a purge of {UriCount} path(s) on endpoint {EndpointName}: over the {MaxUriCount} Front Door accepts at once.")]
    public static partial void BatchTooLarge(this ILogger logger, int uriCount, string endpointName, int maxUriCount);
}
