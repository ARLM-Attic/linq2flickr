using Linq.Flickr;
using Linq.Flickr.Interface;
using Linq.Flickr.Proxies;

namespace Linq.Flickr.Repository
{
    public class DefaultRepositoryFactory : IRepositoryFactory
    {
        private HttpRequestProxy httpRequest;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultRepositoryFactory"/> class.
        /// </summary>
        public DefaultRepositoryFactory()
        {
            this.httpRequest = new HttpRequestProxy(new WebRequestProxy());
        }

        public IAuthRepository CreateAuthRepository()
        {
            return new AuthRepository();
        }

        public ICommentRepository CreateCommentRepository()
        {
            return new CommentRepository(this.httpRequest);
        }

        public IPeopleRepository CreatePeopleRepository()
        {
            return new PeopleRepository();
        }

        public ITagRepository CreateTagRepository()
        {
            return new TagRepository(this.httpRequest);
        }

        public IPhotoRepository CreatePhotoRepository()
        {
            return new PhotoRepository(this.httpRequest);
        }
    }
}