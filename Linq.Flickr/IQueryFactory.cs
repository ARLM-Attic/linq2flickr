namespace Linq.Flickr
{
    public interface IQueryFactory
    {
        TagQuery CreateTagQuery();

        PeopleQuery CreatePeopleQuery();

        PhotoQuery CreatePhotoQuery();
    }
}