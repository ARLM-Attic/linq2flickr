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
        public PhotoQuery Photos
        {
            get
            {
                if (photos == null)
                {
                    photos = new PhotoQuery();
                }

                return photos;
            }
        }

        public TagQuery Tags
        {
            get
            {
                if (tags == null)
                {
                    tags = new TagQuery();
                }

                return tags;
            }
        }

        public PeopleQuery Peoples
        {
            get
            {
                if (peoples == null)
                {
                    peoples = new PeopleQuery();
                }
                return peoples;
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
                IAuthRepository repository = new AuthRepository();
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
            photos.SubmitChanges();
            // sync changed comments, if any.
            photos.Comments.SubmitChanges();
        }

        private PhotoQuery photos;
        private TagQuery tags;
        private PeopleQuery peoples;
    }
}
