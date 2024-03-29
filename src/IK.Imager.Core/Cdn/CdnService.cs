﻿using System;
using IK.Imager.Core.Abstractions.Cdn;
using IK.Imager.Core.Settings;
using Microsoft.Extensions.Options;

namespace IK.Imager.Core.Cdn
{
    public class CdnService: ICdnService
    {
        private readonly IOptions<CdnSettings> _cdnSettings;

        public CdnService(IOptions<CdnSettings> cdnSettings)
        {
            _cdnSettings = cdnSettings;
        }
        
        /// <inheritdoc />
        public Uri TryTransformToCdnUri(Uri originalUri)
        {
            if (_cdnSettings.Value.Uri == null)
                return originalUri;
            
            var builder = new UriBuilder(originalUri)
            {
                Host = _cdnSettings.Value.Uri.Host,
                Scheme = _cdnSettings.Value.Uri.Scheme
            };
            
            return builder.Uri;
        }
    }
}