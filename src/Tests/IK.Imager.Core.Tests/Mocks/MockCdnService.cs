using System;
using IK.Imager.Core.Abstractions.Cdn;

namespace IK.Imager.Core.Tests.Mocks
{
    public class MockCdnService: ICdnService
    {
        public Uri TryTransformToCdnUri(Uri originalUri)
        {
            return originalUri;
        }
    }
}