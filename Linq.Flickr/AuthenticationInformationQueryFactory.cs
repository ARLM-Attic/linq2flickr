using Linq.Flickr.Authentication;

namespace Linq.Flickr
{
    public class AuthenticationInformationQueryFactory : IQueryFactory
    {
        private readonly AuthenticationInformation authenticationInformation;

        public AuthenticationInformationQueryFactory(AuthenticationInformation authenticationInformation)
        {
            this.authenticationInformation = authenticationInformation;
        }

        public TagQuery CreateTagQuery()
        {
            return new TagQuery(authenticationInformation);
        }

        public PeopleQuery CreatePeopleQuery()
        {
            return new PeopleQuery(authenticationInformation);
        }

        public PhotoQuery CreatePhotoQuery()
        {
            return new PhotoQuery(authenticationInformation);
        }
    }
}