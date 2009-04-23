using System;
using Linq.Flickr.Interface;
using Linq.Flickr.Repository;

namespace Linq.Flickr
{
    /// <summary>
    /// Entry point for LINQ to Flickr query.
    /// </summary>
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
        /// <summary>
        /// check if the user is already authicated for making authenticated calls.
        /// </summary>
        /// <returns>returns true/false</returns>
        public bool IsAuthenticated()
        {
            using (IAuthRepository authRepository = new AuthRepository())
            {
                return authRepository.IsAuthenticated();
            }
        }
        /// <summary>
        /// does a manual authentication.
        /// </summary>
        public AuthToken Authenticate()
        {
            using (IAuthRepository authRepository = new AuthRepository())
            {
                return authRepository.Authenticate(true, Permission.Delete);
            }
        }

        /// <summary>
        /// removes the token from cache or cookie.
        /// </summary>
        /// <returns></returns>
        public bool ClearToken()
        {
            bool result = true;

            try
            {
                IRepositoryBase repository = new BaseRepository();
                repository.ClearToken();
            }
            catch
            {
                result = false;
            }

            return result;
        }


        public void SubmitChanges()
        {
            _Photos.SubmitChanges();
            // sync changed comments, if any.
            _Photos.Comments.SubmitChanges();
        }

    }
}
