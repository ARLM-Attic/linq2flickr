using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Linq.Flickr
{
    public class FlickrException : System.Exception
    {
        public FlickrException(string code, string message) :
            base("Error code: " + code + " Message: " + message)
        { }
    }
}
