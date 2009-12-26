using Linq.Flickr.Authentication;
using Linq.Flickr.Interface;

namespace Linq.Flickr.Repository
{
    public class AuthenticationInformationRepositoryFactory : IRepositoryFactory
    {
        private readonly AuthenticationInformation authenticationInformation;

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
            return new CommentRepository(authenticationInformation);
        }

        public IPeopleRepository CreatePeopleRepository()
        {
            return new PeopleRepository(authenticationInformation);
        }

        public ITagRepository CreateTagRepository()
        {
            return new TagRepository(authenticationInformation, CreateAuthRepository());
        }

        public IPhotoRepository CreatePhotoRepository()
        {
            return new PhotoRepository(authenticationInformation);
        }
    }
}