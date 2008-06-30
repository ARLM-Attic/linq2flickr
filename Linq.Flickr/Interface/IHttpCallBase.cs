using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Linq.Flickr.Interface
{
    public interface IHttpCallBase
    {
        XElement GetElement(string requestUrl);
        XElement ParseElement(string response);
        string DoHTTPPost(string requestUrl);
    }
}
