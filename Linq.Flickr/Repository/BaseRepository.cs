using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Linq.Flickr.Interface;
using System.Web;
using Linq.Flickr.Configuration;
using System.IO;
using System.Diagnostics;

namespace Linq.Flickr.Repository
{

    public class BaseRepository : HttpCallBase, IRepositoryBase
    {
        protected string FLICKR_API_KEY = string.Empty;
        protected string SHARED_SECRET = string.Empty;
        protected string STORE_PATH = string.Empty;
        protected string TOKEN_PATH = string.Empty;

        public BaseRepository()
        {
            LoadBase();
        }

        private void LoadBase()
        {
            try
            {
                LoadFromConfig();
                Type myInterfaceType = typeof (IRepositoryBase);
                myInterfaceType.RefreshExternalMethodList();
            }
            catch (Exception ex)
            {
                throw new Exception("Error initializing Base", ex);
            }
        }

        public BaseRepository(Type intefaceType)
        {
            try
            {
                LoadBase();
                intefaceType.RefreshExternalMethodList();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private void LoadFromConfig()
        {
            // load the keys.
            FLICKR_API_KEY = FlickrSettings.Current.ApiKey;
            SHARED_SECRET = FlickrSettings.Current.SecretKey;

            // if Offline application , then create a cache directory.
            if (HttpContext.Current == null)
            {
                STORE_PATH = FlickrSettings.Current.CacheDirectory;
                // path where token will be stored.
                TOKEN_PATH = STORE_PATH + "\\token_{0}.xml";
            }
        }

        protected string BuildUrl(string functionName, params object[] args)
        {
            return BuildUrl(functionName, new Dictionary<string, string>(), args);
        }

        protected string BuildUrl(string functionName, IDictionary<string, string> dic, params object[] args)
        {
            dic.Add(Helper.BASE_URL + "?method", functionName);
            dic.Add("api_key", FLICKR_API_KEY);

            ProcessArguments(args, dic);

            return GetUrl(dic);
        }

        protected void AddHeader(string method, IDictionary<string, string> dictionary)
        {
            dictionary.Add(Helper.BASE_URL + "?method", method);
            dictionary.Add("api_key", FLICKR_API_KEY);
        }

        protected static string GetUrl(IDictionary<string, string> urlDic)
        {
            string url = string.Empty;

            foreach (string key in urlDic.Keys)
            {
                if (!string.IsNullOrEmpty(urlDic[key]))
                {
                    url += key + "=" + urlDic[key] + "&";
                }
            }

            if (url.Length > 0 && url.Substring(url.Length - 1, 1) == "&")
                url = url.Substring(0, url.Length - 1);

            return url;
        }

        protected static void ProcessArguments(object[] args, IDictionary<string, string> sorted)
        {
            int index = 0;
            while (index < args.Length)
            {
                int nextIndex = index + 1;
                // appned if the search keyword is not empty.
                if (nextIndex < args.Length && (!string.IsNullOrEmpty(Convert.ToString(args[index + 1]))))
                {
                    string value = Convert.ToString(args[index + 1]);
                    
                    if (!string.IsNullOrEmpty(value))
                    {
                        sorted.Add((string)args[index], value);
                    }
                }
                index += 2;
            }
        }

        private void CreateDirectoryIfNecessary()
        {
            if (!Directory.Exists(STORE_PATH))
            {
                Directory.CreateDirectory(STORE_PATH);
            }
        }

        string IRepositoryBase.GetFrob()
        {
            string method = Helper.GetExternalMethodName();
            string signature = GetSignature(method, true);
            string requestUrl = BuildUrl(method, "api_sig", signature);

            string frob = string.Empty;

            try
            {
                var element = GetElement(requestUrl);
                frob = element.Element("frob").InnerXml ?? string.Empty;
                return frob;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

       
        AuthToken  IRepositoryBase.GetAuthenticatedToken (string permission, bool validate)
        {
            string method = Helper.GetExternalMethodName();

            permission = permission.ToLower();

            AuthToken token = null;
                
            if (IsAuthenticated())
                token = GetExistingToken(permission);

            if (token == null && validate)
            {
                token = CreateAndStoreNewToken(method, permission);
            }

            return token;
        }

        protected string Authenticate(string permission, bool validate)
        {
            AuthToken token =  (this as IRepositoryBase).GetAuthenticatedToken(permission, validate);
            if (token != null)
            {
                return token.Id;
            }
            return string.Empty;
        }

        protected string Authenticate(string permission)
        {
            return  (this as IRepositoryBase).GetAuthenticatedToken(permission, true).Id;
        }

        private AuthToken GetExistingToken(string permission)
        {
            if (HttpContext.Current == null)
                return GetDesktopToken(false, permission);
            else
                return GetWebToken(false, permission);
        }

        private AuthToken CreateAndStoreNewToken(string method, string permission)
        {
            return HttpContext.Current != null ? CreateWebToken(permission) : CreateDesktopToken(method, permission);
        }

        private AuthToken CreateWebToken(string permission)
        {
            AuthToken token = null;
         
            try
            {
                if (HttpContext.Current.Request.Cookies["token"] == null)
                {
                    string frob = CreateWebFrobIfNecessary();
                    AuthToken tokenObject = (this as IPhotoRepository).GetTokenFromFrob(frob);

                    HttpCookie authCookie = new HttpCookie(
                        "token", // Name of auth cookie
                        tokenObject.Id + "|" + tokenObject.Perm + "|" + tokenObject.UserId); // Hashed ticket
                    authCookie.Expires = DateTime.Now.AddDays(30);
                    HttpContext.Current.Response.Cookies.Add(authCookie);

                    token = tokenObject;
                }
            }
            catch
            {
                string frob = CreateWebFrobIfNecessary();
                IntializeToken(permission, frob);
            }
            return token;
        }

        private AuthToken CreateDesktopToken(string method, string permission)
        {
            XmlElement tokenElement = null;
            string token = string.Empty;

            try
            {
                string path = string.Format(TOKEN_PATH, permission);
                string frob = (this as IRepositoryBase).GetFrob();

                string sig = GetSignature(method, true, "frob", frob);
                string requestUrl = BuildUrl(method, "frob", frob, "api_sig", sig);

                IntializeToken(permission, frob);

                tokenElement = GetElement(requestUrl);

                CreateDirectoryIfNecessary();

                FileStream stream = File.Open(path, FileMode.OpenOrCreate);

                TextWriter writer = new StreamWriter(stream);

                tokenElement.Save(writer);

                writer.Close();
                stream.Close();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to complete the authentication process", ex);
            }

            return GetAToken(tokenElement);
        }

        private string GetAuthenticationUrl(string permission, string frob)
        {
            string sig = GetSignature(string.Empty, false, "perms", permission, "frob", frob);
            string authenticateUrl = Helper.AUTH_URL + "?api_key=" + FLICKR_API_KEY + "&perms=" + permission + "&frob=" + frob + "&api_sig=" + sig;

            return authenticateUrl;
        }

        private void IntializeToken(string permission, string frob)
        {
            try
            {
                string authenticateUrl = GetAuthenticationUrl(permission, frob);

                // check if the requester is a web application
                if (HttpContext.Current != null)
                {
                    HttpContext.Current.Response.Redirect(authenticateUrl);
                }
                else
                {
                    // do process request and wait till the browser closes.
                    Process p = new Process();
                    p.StartInfo.FileName = "IExplore.exe";
                    p.StartInfo.Arguments = authenticateUrl;
                    p.Start();

                    p.WaitForExit(int.MaxValue);

                    if (p.HasExited)
                    {
                        // do nothing.
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        protected static AuthToken GetAToken(XmlElement tokenElement)
        {
            AuthToken token = (from tokens in tokenElement.Descendants("auth")
                               select new AuthToken
                               {
                                   Id = tokens.Element("token").InnerXml ?? string.Empty,
                                   Perm = tokens.Element("perms").InnerXml,
                                   UserId = tokens.Element("user").Attribute("nsid").Value ?? string.Empty
                               }).Single<AuthToken>();

            return token;
        }
        protected AuthToken ValidateToken(string method, string token)
        {
            string sig = GetSignature(method, true, "auth_token", token);
            string requestUrl = BuildUrl(method, "auth_token", token, "api_sig", sig);

            try
            {
                XmlElement tokenElement = GetElement(requestUrl);

                return GetAToken(tokenElement);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }


        private AuthToken GetDesktopToken(bool validate, string permission)
        {
            string token = string.Empty;
            string path = string.Format(TOKEN_PATH, permission);

            XmlElement tokenElement = XmlExtension.Load(XmlReader.Create(new StreamReader(path))); ;
            AuthToken tokenObject = GetAToken(tokenElement);

            return tokenObject;
        }

        private AuthToken GetWebToken(bool validate, string permission)
        {
            string token = string.Empty;

            try
            {
                if (HttpContext.Current.Request.Cookies["token"] != null)
                {
                    token = HttpContext.Current.Request.Cookies["token"].Value;
                }
            }
            catch
            {
                if (validate)
                {
                    IntializeToken(permission, CreateWebFrobIfNecessary());
                }
            }

            if (!string.IsNullOrEmpty(token))
            {
                 string [] parts = token.Split(new char[] {'|'}, StringSplitOptions.RemoveEmptyEntries);
                 // build the object and return.   
                 if (parts.Length == 3) return new AuthToken {Id = parts[0], Perm = parts[1], UserId = parts[2]};
            }
        
            return null;
        }

        private string CreateWebFrobIfNecessary()
        {
            // if it is a redirect by flickr then take the frob from url.
            return !string.IsNullOrEmpty(HttpContext.Current.Request["frob"]) ? HttpContext.Current.Request["frob"] : (this as IRepositoryBase).GetFrob();
        }

        protected bool IsAuthenticated()
        {
            bool result = false;

            string path = string.Format(TOKEN_PATH, Permission.Delete.ToString().ToLower());

            if (HttpContext.Current == null)
                result = File.Exists(path);
            else
                result = (HttpContext.Current.Request.Cookies["token"] != null);

            return result;
        }

        void IRepositoryBase.ClearToken()
        {
            string path = string.Format(TOKEN_PATH, Permission.Delete.ToString().ToLower());

            if (HttpContext.Current == null)
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            else
            {
                HttpCookie cookie = HttpContext.Current.Request.Cookies["token"];

                if (cookie != null)
                {
                    cookie.Expires = DateTime.Now.AddYears(-1);
                    HttpContext.Current.Response.Cookies.Add(cookie);
                }
            }
        }

        public string GetSignature(string methodName, bool includeMethod, params object[] args)
        {
            IDictionary<string, string> sortedDic = new Dictionary<string, string>();

            object[] originalArgs = args;

            if (args.Length > 0)
            {
                if (args[0] is Dictionary<string, string>)
                {
                    sortedDic = args[0] as Dictionary<string, string>;

                    if (args.Length > 1)
                    {
                        originalArgs = args[1] as object[];
                    }
                    else
                    {
                        originalArgs = new object[0];
                    }
                }

            }
            return GetSignature(methodName, includeMethod, sortedDic, originalArgs);
        }
        
        private string GetSignature(string methodName, bool includeMethod, IDictionary<string, string> sigItems, params object[] args)
        {
            string signature = string.Empty;

            if (includeMethod)
            {
                // add the mehold name param first.
                sigItems.Add("method", methodName);
            }
            // add the api key
            sigItems.Add("api_key", FLICKR_API_KEY);

            if (args.Length > 0)
            {

                // do the argument processing, if there is any    
                for (int index = 0; index < args.Length; index += 2)
                {
                    if (!string.IsNullOrEmpty(Convert.ToString(args[index + 1])))
                    {
                        if (!sigItems.ContainsKey((string) args[index]))
                        {
                            sigItems.Add((string) args[index], Convert.ToString(args[index + 1]));
                        }
                    }
                }
            }
            // sort the items.

            var query = from sigItem in sigItems
                        orderby sigItem.Key ascending
                        select sigItem.Key + sigItem.Value;

            foreach (var keyValuePair in query)
            {
                signature += keyValuePair;
            }

            signature = SHARED_SECRET + signature;

            return signature.GetHash();
        }

        private string GetNSIDInternal(string method, params object[] args)
        {
            string nsId = string.Empty;
            string requestUrl = BuildUrl(method, args);

            try
            {
                XmlElement element = GetElement(requestUrl);
                nsId = element.Element("user").Attribute("nsid").Value;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            return nsId;
        }

        public string GetNSID(string method, string field, string value)
        {
            return GetNSIDInternal(method, field, value);
        }    

    }
}
