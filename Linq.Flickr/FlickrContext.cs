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
        private CommentQuery _Comments = null;
       
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

        public CommentQuery Comments
        {
            get
            {
                if (_Comments == null)
                {
                    _Comments = new CommentQuery();
                }

                return _Comments;
            }
        }

        public void SubmitChanges()
        {
            _Photos.SubmitChanges();
            // do if any.
            _Comments.SubmitChanges();
        }

    }
}
