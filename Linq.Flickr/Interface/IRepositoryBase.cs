using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Linq.Flickr.Attribute;

namespace Linq.Flickr.Interface
{
    public interface IRepositoryBase 
    {
        [FlickrMethod("flickr.auth.getFrob")]
        string GetFrob();
        string GetSignature(string methodName, bool includeMethod, params object[] args);
        [FlickrMethod("flickr.auth.getToken")]
        AuthToken CreateAuthTokeIfNecessary(string permission, bool validate);
        string GetNsid(string method, string field, string value);
        void ClearToken();
    }
}
