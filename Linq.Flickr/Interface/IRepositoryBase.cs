using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Linq.Flickr.Interface
{
    public interface IRepositoryBase 
    {
        string GetFrob();
        string Authenticate(bool validate, string permission);
        string GetSignature(string methodName, bool includeMethod, params object[] args);
        string GetNSID(string method, string field, string value);
    }
}
