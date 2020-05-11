using System;

namespace IK.Imager.Core.Abstractions.Services
{
    public interface ICdnService
    {
        Uri TryTransformToCdnUri(Uri originalUri);
    }
}