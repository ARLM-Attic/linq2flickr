using System;
using Linq.Flickr.Attribute;
using Linq.Flickr.Repository;

namespace Linq.Flickr.Interface
{
    public interface IAuthRepository : IDisposable
    {
        [FlickrMethod("flickr.auth.getToken")]
        AuthToken GetTokenFromFrob(string frob);
        [FlickrMethod("flickr.auth.checkToken")]
        AuthToken CheckToken(string token);
        [FlickrMethod("flickr.auth.getToken")]
        AuthToken Authenticate(bool validate, Permission permission);
        bool IsAuthenticated();
     
    }
}
