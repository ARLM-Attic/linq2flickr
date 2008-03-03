using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Linq.Flickr.Interface;
using Linq.Flickr.Repository;

namespace Linq.Flickr
{
    [Serializable]
    public class FlickrContext 
    {
        private  PhotoQuery _Photos = null;
       
        public PhotoQuery Photos
        {
            get
            {
                if (_Photos == null)
                {
                    _Photos = new PhotoQuery();
                }

                return _Photos;
            }
        }

        public void SubmitChanges()
        {
            _Photos.SubmitChanges();
            // sync changed comments, if any.
            _Photos.Comments.SubmitChanges();
        }

    }
}
