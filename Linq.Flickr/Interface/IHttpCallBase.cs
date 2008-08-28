using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Linq.Flickr.Interface
{
    public interface IHttpCallBase
    {
        XmlElement GetElement(string requestUrl);
        XmlElement ParseElement(string response);
        string DoHTTPPost(string requestUrl);
    }
}
