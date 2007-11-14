using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Linq.Flickr.Attribute;

namespace Linq.Flickr.Interface
{
    public interface IFlickr : IDisposable
    {
        // photo releated method.
        [FlickrMethod("flickr.photos.getInfo")]
        Photo GetPhotoDetail(string id, PhotoSize size);
        [FlickrMethod("flickr.photos.search")]
        IEnumerable<Photo> Search(string user, string filter, PhotoSize size, ViewMode visibility, SortOrder sortOrder, int index, int pageLen);
        [FlickrMethod("flickr.photos.search")]
        IEnumerable<Photo> Search(string user, string filter, string tags, TagMode tagMode, PhotoSize size, ViewMode visibility, SortOrder sortOrder, int index, int pageLen);
        [FlickrMethod("flickr.photos.getRecent")]
        IList<Photo> GetRecent(int index, int itemsPerPage, PhotoSize photoSize);
        [FlickrMethod("flickr.photos.delete")]
        bool Delete(string photoId);
        [FlickrMethod("flickr.photos.getSizes")]
        string GetSizedPhotoUrl(string id, PhotoSize size);

        // user related methods.
        [FlickrMethod("flickr.people.findByEmail")]
        string GetNSIDByEmail(string email);
        [FlickrMethod("flickr.people.findByUsername")]
        string GetNSIDByUsername(string username);
        [FlickrMethod("flickr.auth.getFrob")]
        string GetFrob();
        [FlickrMethod("flickr.auth.getFrob")]
        AuthToken GetTokenFromFrob(string frob);
        [FlickrMethod("flickr.auth.checkToken")]
        AuthToken CheckToken(string token);

        //Method that POST_Call / Get calls to validate and upload photos.
        string Authenticate(bool validate);
        string Upload(Photo photo);
    }
}
