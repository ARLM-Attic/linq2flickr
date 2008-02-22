using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

namespace Linq.Flickr.Configuration {

    public class FlickrSettings: ConfigurationSection {

        #region Current Section

        public static FlickrSettings Current {
            get {
                return (FlickrSettings)ConfigurationManager.GetSection("flickr");
            }
        }

        #endregion

        [ConfigurationProperty("apiKey",DefaultValue="#yourKey#")]
        public string ApiKey {
            get {
                return (string)this["apiKey"];
            }
            set {
                this["apiKey"] = value;
            }
        }

        [ConfigurationProperty("secretKey", DefaultValue = "#yourSecretKey#")]
        public string SecretKey {
            get {
                return (string)this["secretKey"];
            }
            set {
                this["secretKey"] = value;
            }
        }

        [ConfigurationProperty("cacheDirectory", DefaultValue = "cache")]
        public string CacheDirectory {
            get {
                return (string)this["cacheDirectory"];
            }
            set {
                this["cacheDirectory"] = value;
            }
        }
    }
}
