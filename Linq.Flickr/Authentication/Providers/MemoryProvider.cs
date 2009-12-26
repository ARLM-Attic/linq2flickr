namespace Linq.Flickr.Authentication.Providers
{
    public class MemoryProvider : AuthenticaitonProvider
    {
        private readonly AuthenticationInformation authenticationInformation;

        public MemoryProvider(AuthenticationInformation authenticationInformation)
            : base(authenticationInformation)
        {
            this.authenticationInformation = authenticationInformation;
        }

        public override AuthToken GetToken(string permission)
        {
            return authenticationInformation.AuthToken;
        }

        public override bool SaveToken(string permission)
        {
            return true;
        }

    }
}