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
        private HotTagQuery _hotTags = null;
       
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

        public HotTagQuery HotTags
        {
            get
            {
                if (_hotTags == null)
                {
                    _hotTags = new HotTagQuery();
                }

                return _hotTags;
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
