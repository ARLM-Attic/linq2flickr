using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Linq.Flickr.Interface;
using System.Web;
using Linq.Flickr.Configuration;

namespace Linq.Flickr.Repository
{
    public class Base
    {
        protected string FLICKR_API_KEY = string.Empty;
        protected string SHARED_SECRET = string.Empty;
        protected string STORE_PATH = string.Empty;
        protected string TOKEN_PATH = string.Empty;

        public Base()
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
                Helper.RefreshExternalMethodList(typeof(IFlickr));
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

        protected string GetSignature(string methodName, bool includeMethod, params object[] args)
        {
            return GetSignature(methodName, includeMethod, new SortedDictionary<string, string>(), args);
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

    }
}
