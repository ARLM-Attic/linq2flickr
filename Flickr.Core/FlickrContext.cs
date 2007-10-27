using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Flickr.Core.Interface;

namespace Flickr.Core
{
    [Serializable]
    public class FlickrContext 
    {
        private  FlickrQuery _Context = null;
        
        public FlickrQuery Photos
        {
            get
            {
                if (_Context == null)
                {
                    _Context = new FlickrQuery();
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
