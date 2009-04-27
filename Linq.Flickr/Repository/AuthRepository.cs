using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Linq.Flickr.Authentication;
using Linq.Flickr.Configuration;
using Linq.Flickr.Interface;

namespace Linq.Flickr.Repository
{
    public class AuthRepository : BaseRepository, IAuthRepository
    {
        public AuthRepository() : base(typeof(IAuthRepository)) { }

        public AuthToken Authenticate(bool validate, Permission permission)
        {
           return (this as IAuthRepository).CreateAuthTokenIfNecessary(permission.ToString().ToLower(), validate);
        }

        public AuthToken CreateAuthTokenIfNecessary(string permission, bool validate)
        {
            AuthenticaitonProvider authenticaitonProvider = GetDefaultAuthenticationProvider();

            permission = permission.ToLower();

            AuthToken token = authenticaitonProvider.GetToken(permission);

            if (token == null && validate)
            {
                authenticaitonProvider.SaveToken(permission);
            }

            return authenticaitonProvider.GetToken(permission);
        }

        public string Authenticate(string permission, bool validate)
        {
            AuthToken token = (this as IAuthRepository).CreateAuthTokenIfNecessary(permission, validate);
            if (token != null)
            {
                return token.Id;
            }
            return string.Empty;
        }

        public string Authenticate(Permission permission)
        {
            return (this as IAuthRepository).CreateAuthTokenIfNecessary(permission.ToString(), true).Id;
        }

        bool IAuthRepository.IsAuthenticated()
        {
            AuthenticaitonProvider authProvider = GetDefaultAuthenticationProvider();
            return authProvider.GetToken(Permission.Delete.ToString().ToLower()) != null;
        }

        AuthToken IAuthRepository.GetTokenFromFrob(string frob)
        {
            string method = Helper.GetExternalMethodName();

            string sig = GetSignature(method, true, "frob", frob);
            string requestUrl = BuildUrl(method, "api_sig", sig, "frob", frob);

            try
            {
                XmlElement tokenElement = GetElement(requestUrl);
                return GetAToken(tokenElement);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return null;
            }
        }

        AuthToken IAuthRepository.CheckToken(string token)
        {
            string method = Helper.GetExternalMethodName();
            return ValidateToken(method, token);
        }

        private AuthenticaitonProvider GetDefaultAuthenticationProvider()
        {
            AuthProviderElement providerElement = FlickrSettings.Current.DefaultProvider;

            return (AuthenticaitonProvider)Activator.CreateInstance(Type.GetType(providerElement.Type), null);
        }

        void IAuthRepository.ClearToken()
        {
            AuthenticaitonProvider authenticaitonProvider = GetDefaultAuthenticationProvider();
            authenticaitonProvider.ClearToken(Permission.Delete.ToString().ToLower());
        }

        public void Dispose()
        {
            
        }
    }
}
