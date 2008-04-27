using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Linq.Flickr.Interface
{
    public interface IHttpCall
    {
        XElement GetElement(string requestUrl);
        string DoHTTPPost(string requestUrl);
    }
}
