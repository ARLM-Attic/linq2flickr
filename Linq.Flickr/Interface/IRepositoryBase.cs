using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Linq.Flickr.Attribute;

namespace Linq.Flickr.Interface
{
    public interface IRepositoryBase 
    {
        string GetFrob();
        string Authenticate(string permission);
        string GetSignature(string methodName, bool includeMethod, params object[] args);
        string GetNSID(string method, string field, string value);
    }
}
