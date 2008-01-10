using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Linq.Flickr.Attribute;
using Linq.Flickr.Repository;

namespace Linq.Flickr.Interface
{
    public interface IFlickr : IDisposable
    {
        // photo releated method.
        [FlickrMethod("flickr.photos.getInfo")]
        Photo GetPhotoDetail(string id, PhotoSize size);
        [FlickrMethod("flickr.photos.search")]
        IEnumerable<Photo> Search(int index, int pageLen, PhotoSize photoSize, params string[] args);
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
        string Authenticate(bool validate, Permission permission);
        string Upload(object[] args, string fileName, byte[] photoData);
        [FlickrMethod("flickr.people.getUploadStatus")]
        People GetUploadStatus();
    }
}
