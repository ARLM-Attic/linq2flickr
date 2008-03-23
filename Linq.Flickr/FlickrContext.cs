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
        private PopularTagQuery _hotTags = null;
        private PeopleQuery _peopleQuery = null;
       
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

        public PopularTagQuery HotTags
        {
            get
            {
                if (_hotTags == null)
                {
                    _hotTags = new PopularTagQuery();
                }

                return _hotTags;
            }
        }

        public PeopleQuery Peoples
        {
            get
            {
                if (_peopleQuery == null)
                {
                    _peopleQuery = new PeopleQuery();
                }
                return _peopleQuery;
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
