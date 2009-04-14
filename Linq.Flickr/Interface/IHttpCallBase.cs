using System.Xml;

namespace Linq.Flickr.Interface
{
    public interface IHttpCallBase
    {
        XmlElement GetElement(string requestUrl);
        XmlElement ParseElement(string response);
        string DoHTTPPost(string requestUrl);
    }
}
