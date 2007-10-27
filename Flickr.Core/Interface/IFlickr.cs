using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flickr.Core.Interface
{
    public interface IFlickr : IDisposable
    {
        /// <summary>
        /// flickr.photos.getInfo
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Photo</returns>
        Photo GetPhotoDetail(string id, PhotoSize size);
   
        IEnumerable<Photo> Search(string filter, int index, int pageLen);
        IEnumerable<Photo> Search(string filter, int index, int pageLen, PhotoSize size, ViewMode visisblity);
        //IEnumerable<Photo> Search(Photo dummyObject, int index, int pageLen, SearchMode mode);
        IEnumerable<Photo> Search(string user, string filter, PhotoSize size, ViewMode visibility, SortOrder sortOrder, int index, int pageLen, SearchMode mode);
        IEnumerable<Photo> Search(string user, string filter, int index, int pageLen);
        
        string GetFrob();
        AuthToken GetTokenFromFrob(string frob);
        // authenticate a call.
        AuthToken CheckToken(string token);
        IList<Photo> GetRecent(int index, int itemsPerPage, PhotoSize photoSize);
        string Authenticate(bool validate);
        string Upload(Photo photo);
        bool Delete(string photoId);
    }
}
