using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Linq.Flickr.Authentication;
using Linq.Flickr.Interface;
using System.Web;
using Linq.Flickr.Configuration;
using System.Diagnostics;

namespace Linq.Flickr.Repository
{
    public class BaseRepository : HttpCallBase, IRepositoryBase
    {
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
            flickrApiKey = FlickrSettings.Current.ApiKey;
            sharedSecret = FlickrSettings.Current.SecretKey;
        }

        protected string BuildUrl(string functionName, params object[] args)
        {
            return BuildUrl(functionName, new Dictionary<string, string>(), args);
        }

        protected string BuildUrl(string functionName, IDictionary<string, string> dic, params object[] args)
        {
            dic.Add(Helper.BASE_URL + "?method", functionName);
            dic.Add("api_key", flickrApiKey);

            ProcessArguments(args, dic);

            return GetUrl(dic);
        }

        protected void AddHeader(string method, IDictionary<string, string> dictionary)
        {
            dictionary.Add(Helper.BASE_URL + "?method", method);
            dictionary.Add("api_key", flickrApiKey);
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

        string IRepositoryBase.GetFrob()
        {
            string method = Helper.GetExternalMethodName();
            string signature = GetSignature(method, true);
            string requestUrl = BuildUrl(method, "api_sig", signature);

            string frob = string.Empty;

            try
            {
                var element = GetElement(requestUrl);
                frob = element.Element("frob").InnerText ?? string.Empty;
                return frob;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        AuthToken  IRepositoryBase.CreateAuthTokeIfNecessary (string permission, bool validate)
        {
            AuthenticaitonProvider authenticaitonProvider = GetDefaultAuthenticationProvider();

            permission = permission.ToLower();

            AuthToken token = null;

            if (IsAuthenticated())
                token = authenticaitonProvider.GetToken(permission);

            if (token == null && validate)
            {
                authenticaitonProvider.SaveToken(permission);
            }

            return authenticaitonProvider.GetToken(permission);
        }

        private AuthenticaitonProvider GetDefaultAuthenticationProvider()
        {
            FlickrProviderElement providerElement = FlickrSettings.Current.DefaultProvider;

            return (AuthenticaitonProvider) Activator.CreateInstance(Type.GetType(providerElement.Type), null);
        }

        protected string Authenticate(string permission, bool validate)
        {
            AuthToken token =  (this as IRepositoryBase).CreateAuthTokeIfNecessary(permission, validate);
            if (token != null)
            {
                return token.Id;
            }
            return string.Empty;
        }

        protected string Authenticate(string permission)
        {
            return  (this as IRepositoryBase).CreateAuthTokeIfNecessary(permission, true).Id;
        }
  
        protected static AuthToken GetAToken(XmlElement tokenElement)
        {
            AuthToken token = (from tokens in tokenElement.Descendants("auth")
                               select new AuthToken
                               {
                                   Id = tokens.Element("token").InnerText ?? string.Empty,
                                   Perm = tokens.Element("perms").InnerText,
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

        protected bool IsAuthenticated()
        {
            AuthenticaitonProvider authenticaitonProvider = GetDefaultAuthenticationProvider();
            return authenticaitonProvider.GetToken(Permission.Delete.ToString()) != null;
        }

        void IRepositoryBase.ClearToken()
        {
            AuthenticaitonProvider authenticaitonProvider = GetDefaultAuthenticationProvider();
            authenticaitonProvider.ClearToken(Permission.Delete.ToString().ToLower());
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
            sigItems.Add("api_key", flickrApiKey);

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

            signature = sharedSecret + signature;

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

        public string GetNsid(string method, string field, string value)
        {
            return GetNSIDInternal(method, field, value);
        }

        protected string flickrApiKey = string.Empty;
        protected string sharedSecret = string.Empty;
 
    }
}
