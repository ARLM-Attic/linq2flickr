using System;
using System.Web;
using Linq.Flickr.Interface;
using Linq.Flickr.Repository;

namespace Linq.Flickr.Authentication.Providers
{
    public class WebProvider : AuthenticaitonProvider
    {
        public override bool SaveToken(string permission)
        {
            IAuthRepository authRepository = new AuthRepository();
            try
            {
                string frob = CreateWebFrobIfNecessary();
                AuthToken token = authRepository.GetTokenFromFrob(frob);

                if (token == null)
                {
                    /// initiate the authenticaiton process.
                    HttpContext.Current.Response.Redirect(GetAuthenticationUrl(permission, frob));
                }

                OnAuthenticationComplete(token);
            }
            catch (Exception)
            {
                throw new Exception("Some error occured during authentication process");
            }
            return false;
        }

        public override void OnAuthenticationComplete(AuthToken token)
        {
            string xml = XmlToObject<AuthToken>.Serialize(token);
            /// create a cookie out of it.
            HttpCookie authCookie = new HttpCookie("token", xml);
            /// set exipration.
            authCookie.Expires = DateTime.Now.AddDays(30);
            /// put it to response.
            HttpContext.Current.Response.Cookies.Add(authCookie);
        }

        public override AuthToken GetToken(string permission)
        {
            try
            {
                if (HttpContext.Current.Request.Cookies["token"] != null)
                {
                    if (HttpContext.Current.Request.Cookies != null)
                    {
                        string xml = HttpContext.Current.Request.Cookies["token"].Value;
                        if (!string.IsNullOrEmpty(xml))
                        {
                            return XmlToObject<AuthToken>.Deserialize(xml);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                throw new Exception("There has been some error getting existing token, plear clear your browser cache", ex);
            }
            return null;
        }


        public override void OnClearToken(AuthToken token)
        {
            HttpCookie cookie = HttpContext.Current.Request.Cookies["token"];

            if (cookie != null)
            {
                cookie.Expires = DateTime.Now.AddYears(-1);
                HttpContext.Current.Response.Cookies.Add(cookie);
            }
        }

        private string CreateWebFrobIfNecessary()
        {
            IRepositoryBase repositoryBase = new BaseRepository();
            // if it is a redirect by flickr then take the frob from url.
            return !string.IsNullOrEmpty(HttpContext.Current.Request["frob"]) ? HttpContext.Current.Request["frob"] : repositoryBase.GetFrob();
        }
    }
}
