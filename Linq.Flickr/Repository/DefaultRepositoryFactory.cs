using System;
using Linq.Flickr.Interface;

namespace Linq.Flickr.Repository
{
    public class DefaultRepositoryFactory : IRepositoryFactory
    {
        public IAuthRepository CreateAuthRepository()
        {
            return new AuthRepository();
        }

        public ICommentRepository CreateCommentRepository()
        {
            return new CommentRepository();
        }

        public IPeopleRepository CreatePeopleRepository()
        {
            return new PeopleRepository();
        }

        public ITagRepository CreateTagRepository()
        {
            return new TagRepository();
        }

        public IPhotoRepository CreatePhotoRepository()
        {
            return new PhotoRepository();
        }
    }
}