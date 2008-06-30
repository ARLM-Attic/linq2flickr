using System;
using System.Collections.Generic;
using System.Linq;
using Linq.Flickr.Interface;
using System.Web;
using Linq.Flickr.Configuration;
using System.Xml.Linq;
using System.IO;
using System.Diagnostics;
using System.Collections;

namespace Linq.Flickr.Repository
{

    public class BaseRepository : HttpCallBase, IRepositoryBase
    {
        protected string FLICKR_API_KEY = string.Empty;
        protected string SHARED_SECRET = string.Empty;
        protected string STORE_PATH = string.Empty;
        protected string TOKEN_PATH = string.Empty;

     
        public BaseRepository(Type intefaceType)
        {
            try
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
                intefaceType.RefreshExternalMethodList();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
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

        public string GetFrob()
        {
            const string method = Helper.FlickrMethod.GET_FROB;
            string signature = GetSignature(method, true);
            string requestUrl = BuildUrl(method, "api_sig", signature);

            string frob = string.Empty;

            try
            {
                var element = GetElement(requestUrl);
                frob = element.Element("frob").Value ?? string.Empty;
                return frob;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        protected string Authenticate (string permission, bool validate)
        {
            permission = permission.ToLower();

            string token = string.Empty;

            if (IsAuthenticated())
                token = GetExistingToken(permission);

            if (string.IsNullOrEmpty(token) && validate)
            {
                token = CreateAndStoreNewToken(permission);
            }

            return token;
        }

        public string Authenticate(string permission)
        {
            return Authenticate(permission, true);
        }

        private string GetExistingToken(string permission)
        {
            if (HttpContext.Current == null)
                return GetDesktopToken(false, permission);
            else
                return GetWebToken(false, permission);
        }

        private string CreateAndStoreNewToken(string permission)
        {
            return HttpContext.Current != null ? CreateWebToken(permission) : CreateDesktopToken(permission);
        }

        private string CreateWebToken(string permission)
        {
            string token = string.Empty;
            string frob = string.Empty;

            try
            {
                if (HttpContext.Current.Request.Cookies["token"] == null)
                {
                    frob = CreateWebFrobIfNecessary();
                    AuthToken tokenObject = (this as IPhotoRepository).GetTokenFromFrob(frob);

                    HttpCookie authCookie = new HttpCookie(
                        "token", // Name of auth cookie
                        tokenObject.ID); // Hashed ticket
                    authCookie.Expires = DateTime.Now.AddDays(30);
                    HttpContext.Current.Response.Cookies.Set(authCookie);

                    token = tokenObject.ID;
                }
            }
            catch
            {
                frob = CreateWebFrobIfNecessary();
                IntializeToken(permission, frob);
            }
            return token;
        }

        private string CreateDesktopToken(string permission)
        {
            XElement tokenElement = null;
            string token = string.Empty;

            try
            {
                string path = string.Format(TOKEN_PATH, permission);
                string frob = GetFrob();

                string sig = GetSignature(Helper.FlickrMethod.GET_AUTH_TOKEN, true, "frob", frob);
                string requestUrl = BuildUrl(Helper.FlickrMethod.GET_AUTH_TOKEN, "frob", frob, "api_sig", sig);

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
                throw new Exception("Error creating token", ex);
            }

            AuthToken tokenObject = GetAToken(tokenElement);

            if (tokenObject != null)
                token = tokenObject.ID;

            return token;
        }

        private string GetAuthenticationUrl(string permission, string frob)
        {
            string sig = GetSignature(string.Empty, false, "perms", permission, "frob", frob);
            string authenticateUrl = Helper.AUTH_URL + "?api_key=" + FLICKR_API_KEY + "&perms=" + permission + "&frob=" + frob + "&api_sig=" + sig;

            return authenticateUrl;
        }

        private string IntializeToken(string permission, string frob)
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

                    }
                }
                return frob;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        protected static AuthToken GetAToken(XElement tokenElement)
        {
            AuthToken token = (from tokens in tokenElement.Descendants("auth")
                               select new AuthToken
                               {
                                   ID = tokens.Element("token").Value ?? string.Empty,
                                   Perm = tokens.Element("perms").Value
                               }).Single<AuthToken>();

            return token;
        }

        private string GetDesktopToken(bool validate, string permission)
        {
            string token = string.Empty;

            string path = string.Format(TOKEN_PATH, permission);
            XElement tokenElement = XElement.Load(path); ;

            AuthToken tokenObject = GetAToken(tokenElement);

            if (tokenObject != null)
                token = tokenObject.ID;

            return token;
        }

        private string GetWebToken(bool validate, string permission)
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
                    string frob = CreateWebFrobIfNecessary();
                    IntializeToken(permission, frob);
                }
            }
            return token;
        }

        private string CreateWebFrobIfNecessary()
        {
            // if it is a redirect by flickr then take the frob from url.
            return !string.IsNullOrEmpty(HttpContext.Current.Request["frob"]) ? HttpContext.Current.Request["frob"] : GetFrob();
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
                XElement element = GetElement(requestUrl);
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
