using Linq.Flickr.Configuration;

namespace Linq.Flickr.Authentication
{
    public class AuthenticationInformation
    {
        public AuthToken AuthToken { get; set; }

        public FlickrSettings FlickrSettings { get; set; }
    }
}