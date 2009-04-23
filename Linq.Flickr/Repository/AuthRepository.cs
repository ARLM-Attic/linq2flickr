using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Linq.Flickr.Interface;

namespace Linq.Flickr.Repository
{
    public class AuthRepository : BaseRepository, IAuthRepository
    {
        public AuthRepository() : base(typeof(IAuthRepository)) { }

        AuthToken IAuthRepository.Authenticate(bool validate, Permission permission)
        {
            string method = Helper.GetExternalMethodName();
            return (this as IRepositoryBase).CreateAuthTokeIfNecessary(permission.ToString().ToLower(), validate);
        }

        bool IAuthRepository.IsAuthenticated()
        {
            return IsAuthenticated();
        }

        AuthToken IAuthRepository.GetTokenFromFrob(string frob)
        {
            string method = Helper.GetExternalMethodName();

            string sig = base.GetSignature(method, true, "frob", frob);
            string requestUrl = BuildUrl(method, "frob", frob, "api_sig", sig);

            try
            {
                XmlElement tokenElement = base.GetElement(requestUrl);
                return GetAToken(tokenElement);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        AuthToken IAuthRepository.CheckToken(string token)
        {
            string method = Helper.GetExternalMethodName();
            return ValidateToken(method, token);
        }


        public void Dispose()
        {
            
        }
    }
}
