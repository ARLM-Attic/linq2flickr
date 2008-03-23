using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Linq.Flickr.Interface;
using System.Web;
using Linq.Flickr.Configuration;
using System.Xml.Linq;
using System.IO;
using System.Diagnostics;
using System.Net;

namespace Linq.Flickr.Repository
{

    public class Base
    {
        protected string FLICKR_API_KEY = string.Empty;
        protected string SHARED_SECRET = string.Empty;
        protected string STORE_PATH = string.Empty;
        protected string TOKEN_PATH = string.Empty;

        public Base(Type intefaceType)
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
                Helper.RefreshExternalMethodList(intefaceType);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
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

        protected string GetUrl(IDictionary<string, string> urlDic)
        {
            string url = string.Empty;

            foreach (string key in urlDic.Keys)
            {
                url += key + "=" + urlDic[key] + "&";
            }

            if (url.Length > 0 && url.Substring(url.Length - 1, 1) == "&")
                url = url.Substring(0, url.Length - 1);

            return url;
        }

        protected void ProcessArguments(object[] args, IDictionary<string, string> sorted)
        {
            int index = 0;
            while (index < args.Length)
            {
                int nextIndex = index + 1;
                // appned if the search keyword is not empty.
                if (nextIndex < args.Length && (!string.IsNullOrEmpty(Convert.ToString(args[index + 1]))))
                {
                    sorted.Add((string)args[index], Convert.ToString(args[index + 1]));
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

        protected string Authenticate(string permission)
        {
            return Authenticate(true, permission.ToLower());
        }

        protected string GetFrob(string method)
        {
            string signature = GetSignature(method, true);
            string requestUrl = BuildUrl(method, "api_sig", signature);

            string frob = string.Empty;

            try
            {
                XElement element = GetElement(requestUrl);
                frob = element.Element("frob").Value ?? string.Empty;
                return frob;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }
        }

        protected string Authenticate(bool validate, string permission)
        {
            string frob = string.Empty;

            if (HttpContext.Current != null)
            {
                if (!string.IsNullOrEmpty(HttpContext.Current.Request["frob"]))
                {
                    frob = HttpContext.Current.Request["frob"];
                }
                else
                {
                    frob = GetFrob("flickr.auth.getFrob");
                }
                return CreateWebToken(frob, validate, permission);
            }
            else
            {
                frob = GetFrob("flickr.auth.getFrob"); ;
                return CreateDesktopToken(frob, validate, permission);
            }
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
            catch (ApplicationException ex)
            {
                throw new ApplicationException(ex.Message);
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

        private string CreateDesktopToken(string frob, bool validate, string permission)
        {
            string sig = GetSignature(Helper.FlickrMethod.GET_AUTH_TOKEN, true, "frob", frob);
            string requestUrl = BuildUrl(Helper.FlickrMethod.GET_AUTH_TOKEN, "frob", frob, "api_sig", sig);
            string token = string.Empty;

            XElement tokenElement = null;
            string path = string.Format(TOKEN_PATH, permission);

            try
            {
                tokenElement = XElement.Load(path);

            }
            catch
            {
                if (validate)
                {
                    IntializeToken(permission, frob);

                    tokenElement = GetElement(requestUrl);

                    CreateDirectoryIfNecessary();

                    FileStream stream = File.Open(path, FileMode.OpenOrCreate);

                    TextWriter writer = new StreamWriter(stream);

                    tokenElement.Save(writer);

                    writer.Close();
                    stream.Close();
                }
                else
                {
                    return token;
                }
            }

            AuthToken tokenObject = GetAToken(tokenElement);

            if (tokenObject != null)
                token = tokenObject.ID;

            return token;
        }

        private string CreateWebToken(string frob, bool validate, string permission)
        {
            string token = string.Empty;
            try
            {
                if (HttpContext.Current.Request.Cookies["token"] == null)
                {
                    AuthToken tokenObject = (this as IPhoto).GetTokenFromFrob(frob);

                    HttpCookie authCookie = new HttpCookie(
                       "token", // Name of auth cookie
                        tokenObject.ID); // Hashed ticket
                    authCookie.Expires = DateTime.Now.AddDays(30);
                    HttpContext.Current.Response.Cookies.Set(authCookie);

                    token = tokenObject.ID;
                }
                else
                {
                    token = HttpContext.Current.Request.Cookies["token"].Value;
                }
            }
            catch
            {
                if (validate)
                {
                    IntializeToken(permission, frob);
                }
            }
            return token;
        }


        protected string GetSignature(string methodName, bool includeMethod, params object[] args)
        {
            return GetSignature(methodName, includeMethod, new SortedDictionary<string, string>(), args);
        }

        protected XElement GetElement(string requestUrl)
        {
            return requestUrl.GetElement();
        }

        protected string GetSignature(string methodName, bool includeMethod, IDictionary<string, string> sorted, params object[] args)
        {
            string signature = string.Empty;

            if (includeMethod)
            {
                // add the mehold name param first.
                sorted.Add("method", methodName);
            }
            // add the api key
            sorted.Add("api_key", FLICKR_API_KEY);

            // do the argument processing, if there is any    
            for (int index = 0; index < args.Length; index += 2)
            {
                if (!string.IsNullOrEmpty(Convert.ToString(args[index + 1])))
                {
                    if (!sorted.ContainsKey((string)args[index]))
                    {
                        sorted.Add((string)args[index], Convert.ToString(args[index + 1]));
                    }
                }
            }

            foreach (string key in sorted.Keys)
            {
                signature += key + sorted[key];
            }

            signature = SHARED_SECRET + signature;

            return signature.GetHash();
        }

        protected static string DoHTTPPost(string requestUrl)
        {
            // Create a request using a URL that can receive a post. 
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(requestUrl);
            // Set the Method property of the request to POST.
            request.Method = "POST";

            request.KeepAlive = true;
            //// Set the ContentType property of the WebRequest.
            request.ContentType = "charset=UTF-8";
            request.ContentLength = 0;
            // Get the request stream.
            Stream dataStream = request.GetRequestStream();
            // Get the response.
            WebResponse response = request.GetResponse();

            dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseFromServer = reader.ReadToEnd();
            // Clean up the streams.
            reader.Close();
            // validate the response.
            // clean up garbage charecters.
            responseFromServer = responseFromServer.Replace("\r", string.Empty).Replace("\n", string.Empty);
            return responseFromServer;
        }

    }
}
