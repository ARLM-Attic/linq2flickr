using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Linq.Flickr.Authentication;

namespace Linq.Flickr.Configuration
{
    public class AuthenticationInformationFlickrSettingsProvider : IFlickrSettingsProvider
    {
        private readonly AuthenticationInformation authenticationInformation;

        public AuthenticationInformationFlickrSettingsProvider(AuthenticationInformation authenticationInformation)
        {
            this.authenticationInformation = authenticationInformation;
        }

        public FlickrSettings GetCurrentFlickrSettings()
        {
            return authenticationInformation.FlickrSettings;
        }
    }
}
