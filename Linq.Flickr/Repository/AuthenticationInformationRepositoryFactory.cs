using Linq.Flickr;
using Linq.Flickr.Authentication;
using Linq.Flickr.Interface;
using Linq.Flickr.Proxies;

namespace Linq.Flickr.Repository
{
    public class AuthenticationInformationRepositoryFactory : IRepositoryFactory
    {
        private readonly AuthenticationInformation authenticationInformation;
        private HttpRequestProxy httpRequest;

        public AuthenticationInformationRepositoryFactory()
        {
            this.httpRequest = new HttpRequestProxy(new WebRequestProxy());
        }

        public AuthenticationInformationRepositoryFactory(AuthenticationInformation authenticationInformation)
        {
            this.authenticationInformation = authenticationInformation;
        }

        public IAuthRepository CreateAuthRepository()
        {
            return new AuthRepository(authenticationInformation);
        }

        public ICommentRepository CreateCommentRepository()
        {
            return new CommentRepository(this.httpRequest, authenticationInformation);
        }

        public IPeopleRepository CreatePeopleRepository()
        {
            return new PeopleRepository(authenticationInformation);
        }

        public ITagRepository CreateTagRepository()
        {
            return new TagRepository(authenticationInformation, CreateAuthRepository(), httpRequest);
        }

        public IPhotoRepository CreatePhotoRepository()
        {
            return new PhotoRepository(this.httpRequest, authenticationInformation);
        }
    }
}