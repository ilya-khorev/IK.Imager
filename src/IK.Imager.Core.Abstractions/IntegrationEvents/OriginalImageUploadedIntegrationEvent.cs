﻿using IK.Imager.EventBus.Abstractions;

namespace IK.Imager.Core.Abstractions.IntegrationEvents
{
    public class OriginalImageUploadedIntegrationEvent: IntegrationEvent
    {
        public override string MessageId => ImageId + PartitionKey;
        public string ImageId { get; set; }
        public string PartitionKey { get; set; }
    }
}