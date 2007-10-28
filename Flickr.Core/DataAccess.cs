using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;
using System.Web;
using System.Net;
using System.Web.Security;
using Flickr.Core.Interface;
using Flickr.Core.Attribute;
using System.Reflection;

namespace Flickr.Core
{

    public class Permission
    {
        public const string READ = "read";
        public const string WRITE = "write";
        public const string DELETE = "delete";
    }

    public class DataAccess : IFlickr
    {
        private string FLICKR_API_KEY = string.Empty;
        private string SHARED_SECRET = string.Empty;
        private string STORE_PATH = string.Empty;
        private string TOKEN_PATH = string.Empty;

        public DataAccess()
        {
            try
            {
                // load the keys.
                FLICKR_API_KEY = System.Configuration.ConfigurationManager.AppSettings["api_key"];
                SHARED_SECRET = System.Configuration.ConfigurationManager.AppSettings["secret_key"];

                // if Offline application , then create a cache directory.
                if (HttpContext.Current == null)
                {
                    STORE_PATH = System.Configuration.ConfigurationManager.AppSettings["cache_dir"];
                    // path where token will be stored.
                    TOKEN_PATH = STORE_PATH + "\\token_{0}.xml";
                }
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);      
            }
        }

        private string BuildUrl(string functionName, params string [] args)
        {
            string requestUrl =  Helper.BASE_URL + "?method=" + functionName  + "&api_key=" + FLICKR_API_KEY;

            for (int index = 0; index < args.Length; index += 2)
            {
                // appned if the search keyword is not empty.
                if (!string.IsNullOrEmpty(args[index + 1]))
                {
                    requestUrl += '&' + args[index] + '=' + args[index + 1];
                }
            }
            return requestUrl;
        }

       
        private string GetSignature(string methodName, bool includeMethod, params object[] args)
        {
            string signature = string.Empty;

            SortedDictionary<string, string> sorted = new SortedDictionary<string, string>();

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
                    sorted.Add((string)args[index], Convert.ToString(args[index + 1]));
                }
            }

            foreach (string key in sorted.Keys)
            {
                signature += key + sorted[key];
            }

            signature = SHARED_SECRET + signature;

            return GetHash(signature);
        }

        private string GetHash(string inputString)
        {
            MD5 md5 = MD5CryptoServiceProvider.Create();

            byte[] input = Encoding.UTF8.GetBytes(inputString);
            byte[] output = MD5.Create().ComputeHash(input);

            return BitConverter.ToString(output).Replace("-", "").ToLower();
        }

        string IFlickr.GetFrob()
        {
            string signature = GetSignature(Helper.FlickrMethod.GET_FROB, true);
            string requestUrl = BuildUrl(Helper.FlickrMethod.GET_FROB, "api_sig", signature);
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

        AuthToken IFlickr.CheckToken(string token)
        {
            string sig = GetSignature(Helper.FlickrMethod.CHECK_AUTH, true, "auth_token", token);
            string requestUrl = BuildUrl(Helper.FlickrMethod.CHECK_AUTH, "auth_token", token, "api_sig", sig);

            try
            {
                XElement tokenElement = GetElement(requestUrl);

                return GetAToken(tokenElement);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }
        }

        private static AuthToken GetAToken(XElement tokenElement)
        {
            AuthToken token = (from tokens in tokenElement.Descendants("auth")
                               select new AuthToken
                               {
                                   ID = tokens.Element("token").Value ?? string.Empty,
                                   Perm = tokens.Element("perms").Value
                               }).Single<AuthToken>();

            return token;
        }

        string IFlickr.Authenticate(bool validate)
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
                    frob = (this as IFlickr).GetFrob();
                }
                return CreateWebToken(frob, validate);
            }
            else
            {
                frob = (this as IFlickr).GetFrob();
                return CreateDesktopToken(frob, validate);
            }
        }

        private string CreateDesktopToken(string frob, bool validate)
        {
            string sig = GetSignature(Helper.FlickrMethod.GET_AUTH_TOKEN, true, "frob", frob);
            string requestUrl = BuildUrl(Helper.FlickrMethod.GET_AUTH_TOKEN, "frob", frob, "api_sig", sig);
            string token = string.Empty;

            XElement tokenElement = null;
            string path = string.Format(TOKEN_PATH, Permission.DELETE);

            try
            {
                tokenElement = XElement.Load(path);
                
            }
            catch
            {
                if (validate)
                {
                    IntializeToken(Permission.DELETE, frob);

                    tokenElement = GetElement(requestUrl);

                    CreateDirectoryIfNecessary();

                    FileStream stream = File.Open(path, FileMode.OpenOrCreate);

                    TextWriter writer = new StreamWriter(stream);

                    tokenElement.Save(writer);
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

        private string CreateWebToken(string frob, bool validate)
        {
            string token = string.Empty;
            try
            {
                if (HttpContext.Current.Request.Cookies["token"] == null)
                {
                    AuthToken tokenObject = (this as IFlickr).GetTokenFromFrob(frob);
                  
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
                    IntializeToken(Permission.DELETE, frob);
                }
            }
            return token;
        }

        AuthToken IFlickr.GetTokenFromFrob(string frob)
        {
            string sig = GetSignature(Helper.FlickrMethod.GET_AUTH_TOKEN, true, "frob", frob);
            string requestUrl = BuildUrl(Helper.FlickrMethod.GET_AUTH_TOKEN, "frob", frob, "api_sig", sig);

            try
            {
                XElement tokenElement = GetElement(requestUrl);
                return GetAToken(tokenElement);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }
        }

        private void CreateDirectoryIfNecessary()
        {
            if (!Directory.Exists(STORE_PATH))
            {
                Directory.CreateDirectory(STORE_PATH);
            }
        }

        IList<Photo> IFlickr.GetRecent(int index, int itemsPerPage, PhotoSize size)
        {
            string requestUrl = BuildUrl(Helper.FlickrMethod.GET_RECENT_PHOTO, "page", index.ToString(), "per_page", itemsPerPage.ToString());

            IList<Photo> photos = new List<Photo>();

            try
            {
                photos = GetPhotos(requestUrl, size, ViewMode.Public).ToList<Photo>();
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }
            return photos;
        }

        string GetNSIDByUsername(string username)
        {
            string nsId = string.Empty;

            string requestUrl = BuildUrl(Helper.FlickrMethod.FIND_PEOPLE_BY_USERNAME, "username", username);

            try
            {
                XElement element = GetElement(requestUrl);
                nsId = element.Element("user").Attribute("nsid").Value;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }
            
            return nsId;
        }


        string GetNSIDByEmail(string email)
        {
            string nsId = string.Empty;

            string requestUrl = BuildUrl(Helper.FlickrMethod.FIND_PEOPLE_BY_EMAIL, "find_email", email);

            try
            {
                XElement element = GetElement(requestUrl);
                nsId = element.Element("user").Attribute("nsid").Value;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }

            return nsId;
        }

        private XElement GetElement(string requestUrl)
        {
            XElement element = XElement.Load(requestUrl);

            if (element.Attribute("stat").Value == "ok")
            {
                return element;
            }
            else
            {
                 _error = (from erros in element.Descendants("err")
                            select new Error
                            {
                                Code = erros.Attribute("code").Value,
                                Message = erros.Attribute("msg").Value
                            }).Single<Error>();

                throw new ApplicationException("Error code: " +  _error.Code + " Message: " + _error.Message);
            }
        }

        private Error _error;
    
        internal class Error
        {
            public string Code { get; set; }
            public string Message { get; set; }
        }

        private bool IsValidEmail(string inputString)
        {
            Regex emailregex = new Regex(@"^([0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*@([0-9a-zA-Z][-\w]*[0-9a-zA-Z]\.)+[a-zA-Z]{2,9})$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            return emailregex.IsMatch(inputString);
        }

        IEnumerable<Photo> IFlickr.Search(string user, string filter, int index, int pageLen)
        {
            return Search(filter, user, PhotoSize.Default, ViewMode.Public, SortOrder.Date_Posted_Desc, index, pageLen, SearchMode.OR);
        }

        private string GetNSID(string user)
        {
            string nsId = string.Empty;

            if (IsValidEmail(user))
            {
                nsId = GetNSIDByEmail(user);
            }
            else
            {
                nsId = GetNSIDByUsername(user);
            }
            return nsId;
        }

        internal class PhotoSizeWrapper
        {
            public string Label {get;set;}
            public string Url { get; set; }
        }

        internal PhotoSize _PhotoSize { get; set; }
        internal ViewMode _Visibility { get; set; }

        string GetSize(string id, PhotoSize size)
        {
            if (_PhotoSize == PhotoSize.Original)
            {

                string requestUrl = BuildUrl(Helper.FlickrMethod.GET_PHOTOS_SIZES, "photo_id", id);
                XElement doc = GetElement(requestUrl);

                var query = from sizes in doc.Descendants("size")
                            select new PhotoSizeWrapper
                            {
                                Label = sizes.Attribute("label").Value ?? string.Empty,
                                Url = sizes.Attribute("source").Value ?? string.Empty
                            };

                PhotoSizeWrapper[] sizeWrapper = query.ToArray<PhotoSizeWrapper>();
                try
                {
                    return sizeWrapper[(int)size].Url;
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        IEnumerable<Photo> IFlickr.Search(string user, string filter, PhotoSize size, ViewMode visibility, SortOrder sortOrder, int index, int pageLen, SearchMode mode)
        {
            return Search(filter, user, size, visibility, sortOrder, index, pageLen, SearchMode.OR);
        }

        IEnumerable<Photo> IFlickr.Search(string filter, int index, int pageLen)
        {
            return Search(filter, string.Empty, PhotoSize.Default, ViewMode.Public, SortOrder.Date_Posted_Desc, index, pageLen, SearchMode.OR);
        }

        IEnumerable<Photo> IFlickr.Search(string filter, int index, int pageLen, PhotoSize size, ViewMode visisblity)
        {
            return Search(filter, string.Empty, size, visisblity,SortOrder.Date_Posted_Desc, index, pageLen, SearchMode.OR);
        }


        private string GetSortOrder(SortOrder order)
        {
           return order.ToString().ToLower().Replace('_', '-');
        }
        /// <summary>
        /// calls flickr.photos.search to list of photos.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private IEnumerable<Photo> Search(string filter, string user, PhotoSize size, ViewMode visibility, SortOrder order,  int index, int pageLen, SearchMode mode)
        {
            // defualt the search mode is any , so no need to pass it.
            string sMode = string.Empty;
            string nsId = string.Empty;

            if (mode == SearchMode.AND)
            {
                sMode = "all";
            }

            if (!string.IsNullOrEmpty(user))
            {
                if (IsValidEmail(user))
                {
                    nsId = GetNSIDByEmail(user);
                }
                else
                {
                    nsId = GetNSIDByUsername(user);
                }
            }

            string token = string.Empty;
            string sig = string.Empty;

            string pFilter = string.Empty;

            if (visibility != ViewMode.Owner)
            {
                pFilter = Convert.ToString((int)visibility);
            }

            
            token = (this as IFlickr).Authenticate(false);

            if (visibility != ViewMode.Public && string.IsNullOrEmpty(token))
            {
                throw new ApplicationException("You must be authenticate to view semi-private / private photos");
            }

            if (!string.IsNullOrEmpty(token))
            {
                if (string.IsNullOrEmpty(user) && visibility == ViewMode.Owner)
                {
                    nsId = "me";
                }

                sig = GetSignature(Helper.FlickrMethod.PHOTO_SEARCH, true, "text", filter, "user_id", nsId, "privacy_filter", pFilter, "tag_mode", sMode, "page", index.ToString(), "per_page", pageLen.ToString(), "sort", GetSortOrder(order), "auth_token", token);
            }

            string requestUrl = BuildUrl(Helper.FlickrMethod.PHOTO_SEARCH, "api_sig", sig, "text", filter, "user_id", nsId, "privacy_filter", pFilter, "tag_mode", sMode, "page", index.ToString(), "per_page", pageLen.ToString(),"sort", GetSortOrder(order), "auth_token", token);
            
            if (index < 1 || index > 500)
            {
                throw new ApplicationException("Index must be between 1 and 500");
            }

            return GetPhotos(requestUrl, size, visibility);
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
        
        private IEnumerable<Photo> GetPhotos(string requestUrl, PhotoSize size, ViewMode visibility)
        {
            XElement doc = GetElement(requestUrl);

            var query = from photos in doc.Descendants("photo")
                        select new Photo
                        {
                            Id = photos.Attribute("id").Value,
                            FarmId = photos.Attribute("farm").Value,
                            ServerId = photos.Attribute("server").Value,
                            SecretId = photos.Attribute("secret").Value,
                            Title = photos.Attribute("title").Value,
                            IsPublic = photos.Attribute("ispublic").Value == "0" ? false : true,
                            IsFamily = photos.Attribute("isfamily").Value == "0" ? false : true,
                            IsFriend = photos.Attribute("isfriend").Value == "0" ? false : true,
                            Url = GetSize(photos.Attribute("id").Value, size) ?? string.Empty,
                            PhotoSize = size
                        };
            return query;
        }
        /// <summary>
        /// calls flickr.photos.getInfo to get the photo object.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Photo IFlickr.GetPhotoDetail(string id, PhotoSize size)
        {
            this._PhotoSize = size;
            Photo pObject = null;

            string token = (this as IFlickr).Authenticate(false);


            string sig = GetSignature(Helper.FlickrMethod.PHOTO_GET_INFO, true, "photo_id", id, "auth_token", token);
            string requestUrl = BuildUrl(Helper.FlickrMethod.PHOTO_GET_INFO, "photo_id", id, "auth_token", token, "api_sig", sig);

            XElement doc = GetElement(requestUrl);

            var query = from photos in doc.Descendants("photo")
                        select new Photo
                        {
                            Id = photos.Attribute("id").Value,
                            FarmId = photos.Attribute("farm").Value,
                            ServerId = photos.Attribute("server").Value,
                            SecretId = photos.Attribute("secret").Value,
                            Title = photos.Element("title").Value,
                            Description = photos.Element("description").Value ?? string.Empty,
                            DateUploaded = photos.Attribute("dateuploaded").Value,
                            PTags = (from tag in photos.Descendants("tag")
                                    select new Tag
                                    {
                                        Id = tag.Attribute("id").Value,
                                        Title = tag.Value
                                    }).ToArray<Tag>(),
                            PhotoSize = size,
                            Url = GetSize(photos.Attribute("id").Value, size)
                        };

            pObject = query.Single<Photo>();

            return pObject;
        }

        public void Dispose()
        {

        }

        private void EncodeAndAddItem(string boundary, ref StringBuilder baseRequest, params object [] items)
        {
            if (baseRequest == null)
            {
                baseRequest = new StringBuilder();
            }

            string form_data = "Content-Disposition: form-data; name=\"{0}\"\r\n";
            string photo_key = "Content-Disposition: form-data; name=\"{0}\";filename=\"{1}\"\r\n";
            string escape = "\r\n";
            string content_type = "Content-Type:application/octet-stream";
            string dash = "--";

            for (int index = 0; index < items.Length; index += 2)
            {
                string key = Convert.ToString(items[index]);
                string value = string.Empty;

                if (index + 1 < items.Length)
                {
                    value = Convert.ToString(items[index + 1]);
                }

                if (!string.IsNullOrEmpty(value))
                {
                    baseRequest.Append(dash);
                    baseRequest.Append(boundary);
                    baseRequest.Append(escape);
                     
                    if (string.Compare(key, "Photo", true) == 0)
                    {
                        baseRequest.Append(string.Format(photo_key, key, value));
                        baseRequest.Append(content_type);
                        baseRequest.Append(escape);
                        baseRequest.Append(escape);
                    }
                    else
                    {
                        baseRequest.Append(string.Format(form_data, key));
                        baseRequest.Append(escape);
                        baseRequest.Append(value);
                        baseRequest.Append(escape);
                    }

                }

                
            }
        
        }

        bool IFlickr.Delete(string photoId)
        {
            string token = Authenticate();
            string sig = GetSignature(Helper.FlickrMethod.DELETE_PHOTO, true, "photo_id", photoId, "auth_token", token);
            string requestUrl = BuildUrl(Helper.FlickrMethod.DELETE_PHOTO, "photo_id", photoId, "auth_token", token, "api_sig", sig);

            try
            {
                // Create a request using a URL that can receive a post. 
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(requestUrl);
                // Set the Method property of the request to POST.
                request.Method = "POST";
                request.KeepAlive = true;
                // Set the ContentType property of the WebRequest.
                request.ContentType = "charset=UTF-8";
                WebResponse response = request.GetResponse();

                Stream dataStream = response.GetResponseStream();
                // Open the stream using a StreamReader for easy access.
                StreamReader reader = new StreamReader(dataStream);
                // Read the content.
                string responseFromServer = reader.ReadToEnd();
                // Clean up the streams.
                reader.Close();
                // validate the response.
                GetElement(responseFromServer);

                return true;
            }
            catch
            {
                return false;
            }
        }

        string IFlickr.Upload(Photo photo)
        {
            string token = Authenticate();

            string boundary = "FLICKR_BOUNDARY";

            int is_public = photo.ViewMode == ViewMode.Public ? 1 : 0;
            int is_friend = photo.ViewMode == ViewMode.Friends || photo.ViewMode == ViewMode.FriendsFamily ? 1 : 0;
            int is_family = photo.ViewMode == ViewMode.Family || photo.ViewMode == ViewMode.FriendsFamily ? 1 : 0; 

            string sig = GetSignature(Helper.UPLOAD_URL, false, "title", photo.Title, "description", photo.Description, "tags", photo.Tags, "is_public" , is_public, "is_family", is_family, "is_friend", is_friend, "auth_token", token);

            StringBuilder builder = new StringBuilder();

            byte[] photoData =  photo.GetBytesFromPhysicalFile();

            EncodeAndAddItem(boundary, ref builder, "title", photo.Title, "description", photo.Description, "tags", photo.Tags, "api_key", FLICKR_API_KEY, "is_public", is_public, "is_family", is_family, "is_friend", is_friend, "auth_token", token, "api_sig", sig, "photo", photo.FileName);

            // Create a request using a URL that can receive a post. 
            HttpWebRequest request = (HttpWebRequest) HttpWebRequest.Create(Helper.UPLOAD_URL);
            // Set the Method property of the request to POST.
            request.Method = "POST";
            request.KeepAlive = true;
            // Set the ContentType property of the WebRequest.
            request.ContentType = "multipart/form-data;charset=UTF-8;boundary=" + boundary + "";
          
            byte[] photoAttributeData = Encoding.UTF8.GetBytes(builder.ToString());
            byte[] footer = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");

            byte[] postContent = new byte[photoData.Length + photoAttributeData.Length + footer.Length];
            
            Buffer.BlockCopy(photoAttributeData, 0, postContent, 0, photoAttributeData.Length);
            Buffer.BlockCopy(photoData, 0, postContent, photoAttributeData.Length, photoData.Length);
            Buffer.BlockCopy(footer, 0, postContent, photoData.Length + photoAttributeData.Length, footer.Length);

            // Set the ContentLength property of the WebRequest.
            request.ContentLength = postContent.Length;
            // Get the request stream.
            Stream dataStream = request.GetRequestStream();

            dataStream.Write(postContent, 0, postContent.Length);
            // Close the Stream object.
            dataStream.Close();
            // Get the response.
            WebResponse response = request.GetResponse();
            // Get the stream containing content returned by the server.
            dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseFromServer = reader.ReadToEnd();
            // Clean up the streams.
            reader.Close();
            dataStream.Close();
            response.Close();
            // get the photo id.
            XElement elemnent = XElement.Parse(responseFromServer);
            photo.Id =  elemnent.Element("photoid").Value ?? string.Empty;

            return photo.Id;

        }

        private string Authenticate()
        {
            string token = string.Empty;

            token = (this as IFlickr).Authenticate(true);

            if (string.IsNullOrEmpty(token))
            {
                throw new ApplicationException("You must be authenticate to upload data");
            }
            return token;
        }

    }
}
