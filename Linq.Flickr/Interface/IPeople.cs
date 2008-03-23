using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Linq.Flickr.Attribute;

namespace Linq.Flickr.Interface
{
    public interface IPeople : IDisposable
    {
        [FlickrMethod("flickr.people.getInfo")]
        People GetInfo(string userId);
        /// <summary>
        /// This method is not directely tied to flickr api.
        /// </summary>
        /// <param name="username">username</param>
        /// <returns>UserInfo</returns>
        People GetByUsername(string username);
    }
}
