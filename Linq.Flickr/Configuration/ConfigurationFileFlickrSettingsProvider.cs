using System.Configuration;

namespace Linq.Flickr.Configuration
{
    public class ConfigurationFileFlickrSettingsProvider : IFlickrSettingsProvider
    {
        public FlickrSettings GetCurrentFlickrSettings()
        {
            return (FlickrSettings)ConfigurationManager.GetSection("flickr");
        }
    }
}