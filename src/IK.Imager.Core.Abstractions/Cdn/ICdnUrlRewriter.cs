using System;

namespace IK.Imager.Core.Abstractions.Cdn
{
    public interface ICdnUrlRewriter
    {
        Uri Rewrite(Uri originalUri);
    }
}
