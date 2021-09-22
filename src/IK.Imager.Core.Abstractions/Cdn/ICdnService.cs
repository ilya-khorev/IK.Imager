using System;

namespace IK.Imager.Core.Abstractions.Cdn
{
    public interface ICdnService
    {
        Uri TryTransformToCdnUri(Uri originalUri);
    }
}