using System;
using Linq.Flickr.Repository.Abstraction;
using Linq.Flickr.Repository;
using Linq.Flickr.Authentication;
using Linq.Flickr.Proxies;

namespace Linq.Flickr
{
    /// <summary>
    /// Entry point for LINQ to Flickr query.
    /// </summary>
    [Serializable]
    public class FlickrContext 
    {
        private readonly AuthenticationInformation authenticationInformation;
        private readonly IQueryFactory queryFactory;
        private IFlickrElement elementProxy;

        public FlickrContext()
        {
            authenticationInformation = null;
            elementProxy = new FlickrElementProxy(new WebRequestProxy());
            
            // creates the query entry point.
            queryFactory = new DefaultQueryFactory(elementProxy);
        }

        public FlickrContext(AuthenticationInformation authenticationInformation)
        {
            this.authenticationInformation = authenticationInformation;
            queryFactory = new AuthQueryFactory(elementProxy, authenticationInformation);
        }

        public PhotoCollection Photos
        {
            get
            {
                if (photos == null)
                    photos = queryFactory.CreatePhotoQuery();
                return photos;
            }
        }

        public TagCollection Tags
        {
            get
            {
                if (tags == null)
                {
                    tags = queryFactory.CreateTagQuery();
                }

                return tags;
            }
        }

        public PeopleCollection Peoples
        {
            get
            {
                if (peoples == null)
                {
                    peoples = queryFactory.CreatePeopleQuery();
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
            using (IAuthRepository authRepository = CreateNewAuthRepository())
            {
                return authRepository.IsAuthenticated();
            }
        }

        /// <summary>
        /// does a manual authentication.
        /// </summary>
        public AuthToken Authenticate()
        {
            using (IAuthRepository authRepository = CreateNewAuthRepository())
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
                IAuthRepository repository = CreateNewAuthRepository();
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

        private IAuthRepository CreateNewAuthRepository()
        {
            IAuthRepository authRepository;
            if (authenticationInformation != null)
                authRepository = CreateAuthRepositoryWithProvidedAuthenticationInformation();
            else
                authRepository = new AuthRepository(elementProxy);
            return authRepository;
        }

        private AuthRepository CreateAuthRepositoryWithProvidedAuthenticationInformation()
        {
            return new AuthRepository(elementProxy, authenticationInformation);
        }


        private PhotoCollection photos;
        private TagCollection tags;
        private PeopleCollection peoples;
    }
}
