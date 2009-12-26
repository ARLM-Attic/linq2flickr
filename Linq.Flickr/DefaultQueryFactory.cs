using System;

namespace Linq.Flickr
{
    public class DefaultQueryFactory : IQueryFactory
    {
        public TagQuery CreateTagQuery()
        {
            return new TagQuery();
        }

        public PeopleQuery CreatePeopleQuery()
        {
            return new PeopleQuery();
        }

        public PhotoQuery CreatePhotoQuery()
        {
            return new PhotoQuery();
        }
    }
}