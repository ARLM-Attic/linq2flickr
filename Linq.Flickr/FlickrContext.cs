using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Linq.Flickr.Interface;

namespace Linq.Flickr
{
    [Serializable]
    public class FlickrContext 
    {
        private  FlickrPhotoQuery _Context = null;

        public FlickrPhotoQuery Photos
        {
            get
            {
                if (_Context == null)
                {
                    _Context = new FlickrPhotoQuery();
                }

                return _Context;
            }
        }


        public void SubmitChanges()
        {
            _Context.SubmitChanges();
        }

        public void Authenticate()
        {
            using (IFlickr flickr = new DataAccess())
            {
                flickr.Authenticate(true);
            }
        }

    }
}
