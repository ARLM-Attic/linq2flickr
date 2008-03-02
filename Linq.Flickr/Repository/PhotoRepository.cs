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
using Linq.Flickr.Interface;
using Linq.Flickr.Attribute;
using System.Reflection;
using System.Xml;

namespace Linq.Flickr.Repository
{
    public enum Permission
    {
        Read = 0,
        Write,
        Delete
    }
    //[DebuggerStepThrough]
    public class PhotoRepository : Base, IPhoto
    {

        public PhotoRepository() : base(typeof(IPhoto)) { }

         string IPhoto.GetFrob()
        {
            string method = Helper.GetExternalMethodName();

            return GetFrob(method);
        }

        AuthToken IPhoto.CheckToken(string token)
        {
            string method = Helper.GetExternalMethodName();

            string sig = GetSignature(method, true, "auth_token", token);
            string requestUrl = BuildUrl(method, "auth_token", token, "api_sig", sig);

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

        People IPhoto.GetUploadStatus()
        {
            string token = Authenticate();
            
            string method = Helper.GetExternalMethodName();
            string sig = GetSignature(method, true, "auth_token", token);
            string requestUrl = BuildUrl(method, "api_sig", sig, "auth_token", token);

            try
            {
                XElement element = GetElement(requestUrl);

                People people = (from p in element.Descendants("user")
                                 select new People
                                 {
                                     Id = p.Attribute("id").Value ?? string.Empty,
                                     IsPro = Convert.ToInt32(p.Attribute("ispro").Value) == 0 ? false : true,
                                     BandWidth = (from b in element.Descendants("bandwidth")
                                                  select new BandWidth
                                                  {
                                                      RemainingKB =  Convert.ToInt32(b.Attribute("remainingkb").Value),
                                                      UsedKB = Convert.ToInt32(b.Attribute("usedkb").Value)
                                                  }).Single<BandWidth>()
                                 }).Single<People>();

                return people;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }
        }
        

        string IPhoto.Authenticate(bool validate, Permission permission)
        {
            return this.Authenticate(validate, permission.ToString().ToLower());
        }

        AuthToken IPhoto.GetTokenFromFrob(string frob)
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

        IList<Photo> IPhoto.GetRecent(int index, int itemsPerPage, PhotoSize size)
        {
            string method = Helper.GetExternalMethodName();
            string requestUrl = BuildUrl(method, "page", index.ToString(), "per_page", itemsPerPage.ToString());

            IList<Photo> photos = new List<Photo>();

            try
            {
                photos = GetPhotos(requestUrl, size).ToList<Photo>();
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }
            return photos;
        }

        string IPhoto.GetNSIDByUsername(string username)
        {
            string nsId = string.Empty;
            string method = Helper.GetExternalMethodName();

            string requestUrl = BuildUrl(method, "username", username);

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


        string IPhoto.GetNSIDByEmail(string email)
        {
            string nsId = string.Empty;
            string method = Helper.GetExternalMethodName();

            string requestUrl = BuildUrl(email, "find_email", email);

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

        private string GetNSID(string user)
        {
            string nsId = string.Empty;

            if (user.IsValidEmail())
            {
                nsId = (this as IPhoto).GetNSIDByEmail(user);
            }
            else
            {
                nsId = (this as IPhoto).GetNSIDByUsername(user);
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

        string IPhoto.GetSizedPhotoUrl(string id, PhotoSize size)
        {
            if (_PhotoSize == PhotoSize.Original)
            {
                string method = Helper.GetExternalMethodName();
                string requestUrl = BuildUrl(method, "photo_id", id);

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

        /// <summary>
        /// calls flickr.photos.search to list of photos.
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        IEnumerable<Photo> IPhoto.Search(int index, int pageLen, PhotoSize photoSize, params string[] args)
        {
            string method = Helper.GetExternalMethodName();

            string token = string.Empty;
            string sig = string.Empty;

            token = (this as IPhoto).Authenticate(false, Permission.Delete);

            if (!string.IsNullOrEmpty(token))
            {
                IDictionary<string, string> sorted = new SortedDictionary<string, string>();
                ProcessArguments(args, sorted);
                ProcessArguments(new object[] { "page", index.ToString(), "per_page", pageLen.ToString(), "auth_token", token }, sorted);
                sig = GetSignature(method, true, sorted);
            }

            IDictionary<string, string> dicionary = new Dictionary<string, string>();

            dicionary.Add(Helper.BASE_URL + "?method", method);
            dicionary.Add("api_key", FLICKR_API_KEY);

            ProcessArguments(args, dicionary);
            ProcessArguments( new object [] {"api_sig", sig, "page", index.ToString(), "per_page", pageLen.ToString(), "auth_token", token }, dicionary);

            string requestUrl = GetUrl(dicionary);

            if (index < 1 || index > 500)
            {
                throw new ApplicationException("Index must be between 1 and 500");
            }

            return GetPhotos(requestUrl, photoSize);
        }
 
        #region PhotoGetBlock
        private IEnumerable<Photo> GetPhotos(string requestUrl, PhotoSize size)
        {
            XElement doc = GetElement(requestUrl);

            var query = from photos in doc.Descendants("photo")
                        select new Photo
                        {
                            Id = photos.Attribute("id").Value ?? string.Empty,
                            FarmId = photos.Attribute("farm").Value ?? string.Empty,
                            ServerId = photos.Attribute("server").Value ?? string.Empty,
                            SecretId = photos.Attribute("secret").Value ?? string.Empty,
                            Title = photos.Attribute("title").Value ?? string.Empty,
                            IsPublic = photos.Attribute("ispublic").Value == "0" ? false : true,
                            IsFamily = photos.Attribute("isfamily").Value == "0" ? false : true,
                            IsFriend = photos.Attribute("isfriend").Value == "0" ? false : true,
                            Url = (this as IPhoto).GetSizedPhotoUrl(photos.Attribute("id").Value, size) ?? string.Empty,
                            PhotoSize = size
                        };
            return query;
        } 

        #endregion
        /// <summary>
        /// calls flickr.photos.getInfo to get the photo object.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Photo IPhoto.GetPhotoDetail(string id, PhotoSize size)
        {
            this._PhotoSize = size;
            Photo pObject = null;

            string method = Helper.GetExternalMethodName();

            string token = (this as IPhoto).Authenticate(false, Permission.Delete);
            string sig = GetSignature(method, true, "photo_id", id, "auth_token", token);
            string requestUrl = BuildUrl(method, "photo_id", id, "auth_token", token, "api_sig", sig);

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
                            Url = (this as IPhoto).GetSizedPhotoUrl(photos.Attribute("id").Value, size)
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

        bool IPhoto.Delete(string photoId)
        {
            string token = Authenticate();
            string method = Helper.GetExternalMethodName();

            string sig = GetSignature(method, true, "photo_id", photoId, "auth_token", token);
            string requestUrl = BuildUrl(method, "photo_id", photoId, "auth_token", token, "api_sig", sig);

            try
            {
                string responseFromServer = DoHTTPPost(requestUrl);
                XElement element = XElement.Parse(responseFromServer, LoadOptions.None);
                ParseElement(element);

                return true;
            }
            catch
            {
                return false;
            }
        }

        string IPhoto.Upload(object[] args, string fileName, byte[] photoData)
        {
            string token = Authenticate();

            string boundary = "FLICKR_BOUNDARY";

            IDictionary<string, string> sorted = new SortedDictionary<string, string>();

            ProcessArguments(new object[]{ "auth_token", token }, sorted);

            string sig = GetSignature(Helper.UPLOAD_URL, false,sorted, args);
           
            StringBuilder builder = new StringBuilder();

            EncodeAndAddItem(boundary, ref builder, new object[] { "api_key", FLICKR_API_KEY, "auth_token", token, "api_sig", sig});
            EncodeAndAddItem(boundary, ref builder, args);
            EncodeAndAddItem(boundary, ref builder, new object[] { "photo", fileName});

            //builder = builder.Remove(builder.Length - 4, 4);
           
            // Create a request using a URL that can receive a post. 
            HttpWebRequest request = (HttpWebRequest) HttpWebRequest.Create(Helper.UPLOAD_URL);
            // Set the Method property of the request to POST.
            request.Method = "POST";
            request.KeepAlive = true;
            // never timeout.
            request.Timeout = 300000;
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
            return elemnent.Element("photoid").Value ?? string.Empty;
        }

        private string Authenticate()
        {
            string token = string.Empty;

            token = (this as IPhoto).Authenticate(true, Permission.Delete);

            if (string.IsNullOrEmpty(token))
            {
                throw new ApplicationException("You must be authenticate to upload data");
            }
            return token;
        }

    }
}
