using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flickr.Core
{
    internal class Helper
    {
        public static class FlickrMethod
        {
            public const string DELETE_PHOTO = "flickr.photos.delete";
            public const string CHECK_AUTH = "flickr.auth.checkToken";
            public const string GET_AUTH_TOKEN = "flickr.auth.getToken";
            public const string GET_FROB = "flickr.auth.getFrob";
            public const string GET_RECENT_PHOTO = "flickr.photos.getRecent";
            public const string FIND_PEOPLE_BY_USERNAME = "flickr.people.findByUsername";
            public const string FIND_PEOPLE_BY_EMAIL = "flickr.people.findByEmail";
            public const string GET_PHOTOS_SIZES = "flickr.photos.getSizes";
            public const string PHOTO_SEARCH = "flickr.photos.search";
            public const string PHOTO_GET_INFO = "flickr.photos.getInfo";
        }

        internal const string BASE_URL = "http://api.flickr.com/services/rest/";
        internal const string AUTH_URL = "http://flickr.com/services/auth/";
        internal const string UPLOAD_URL = "http://api.flickr.com/services/upload/";
 
    }
}
