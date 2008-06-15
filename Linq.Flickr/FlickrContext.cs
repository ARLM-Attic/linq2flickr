using System;

namespace Linq.Flickr
{
    [Serializable]
    public class FlickrContext 
    {
        private  PhotoQuery _Photos;
        private PopularTagQuery _hotTags;
        private PeopleQuery _peopleQuery;
       
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

        public PopularTagQuery PopularTags
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
