using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Net;
using Linq.Flickr.Interface;

namespace Linq.Flickr.Repository
{
    public enum Permission
    {
        Read = 0,
        Write,
        Delete
    }

    public class PhotoRepository : BaseRepository, IPhotoRepository
    {
        public PhotoRepository() : base(typeof(IPhotoRepository)) { }
       
        AuthToken IPhotoRepository.CheckToken(string token)
        {
            string method = Helper.GetExternalMethodName();
            return ValidateToken(method, token);
        }

        People IPhotoRepository.GetUploadStatus()
        {
            string token = base.Authenticate(Permission.Delete.ToString());
            
            string method = Helper.GetExternalMethodName();
            string sig = base.GetSignature(method, true, "auth_token", token);
            string requestUrl = BuildUrl(method, "api_sig", sig, "auth_token", token);

            try
            {
                XElement element = base.GetElement(requestUrl);

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
                throw new Exception(ex.Message);
            }
        }

        bool IPhotoRepository.SetMeta(string photoId, string title, string description)
        {
            string method = Helper.GetExternalMethodName();
            string token = base.Authenticate(Permission.Delete.ToString());
            string sig = base.GetSignature(method, true,"photo_id", photoId, "title", title, "description", description, "auth_token", token);
            string requestUrl = BuildUrl(method, "photo_id", photoId, "title", title, "description", description, "auth_token", token, "api_sig", sig);

            try
            {
                string responseFromServer = DoHTTPPost(requestUrl);
                XElement element = XElement.Parse(responseFromServer, LoadOptions.None);
                element.ValidateResponse();

                return true;
            }
            catch
            {
                return false;
            }
 
        }


        AuthToken IPhotoRepository.Authenticate(bool validate, Permission permission)
        {
            string method = Helper.GetExternalMethodName();
            return  (this as IRepositoryBase).GetAuthenticatedToken(permission.ToString().ToLower(), validate);
        }
        bool IPhotoRepository.IsAuthenticated()
        {
            return IsAuthenticated();
        }

        AuthToken IPhotoRepository.GetTokenFromFrob(string frob)
        {
            string method = Helper.GetExternalMethodName();

            string sig = base.GetSignature(method, true, "frob", frob);
            string requestUrl = BuildUrl(method, "frob", frob, "api_sig", sig);

            try
            {
                XElement tokenElement = base.GetElement(requestUrl);
                return GetAToken(tokenElement);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        IList<Photo> IPhotoRepository.GetMostInteresting(int index, int itemsPerPage, PhotoSize size)
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
                throw new Exception(ex.Message);
            }
            return photos;
        }

        string IPhotoRepository.GetNSIDByUsername(string username)
        {
            string method = Helper.GetExternalMethodName();
            return base.GetNSID(method, "username", username);
        }

        string IPhotoRepository.GetNSIDByEmail(string email)
        {
            string method = Helper.GetExternalMethodName();
            return base.GetNSID(method, "find_email", email);
        }

        internal class PhotoSizeWrapper
        {
            public string Label {get;set;}
            public string Url { get; set; }
        }

        internal PhotoSize _PhotoSize { get; set; }
        internal ViewMode _Visibility { get; set; }

        string IPhotoRepository.GetSizedPhotoUrl(string id, PhotoSize size)
        {
            if (_PhotoSize == PhotoSize.Original)
            {
                string method = Helper.GetExternalMethodName();
                string requestUrl = BuildUrl(method, "photo_id", id);

                XElement doc = base.GetElement(requestUrl);

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
        /// <param name="index"></param>
        /// <param name="pageLen"></param>
        /// <param name="photoSize"></param>
        /// <param name="token"></param>
        /// <param name="args"></param>
        /// <returns>Enumerable of Photos</returns>
        IEnumerable<Photo> IPhotoRepository.Search(int index, int pageLen, PhotoSize photoSize, string token, params string[] args)
        {
            string method = Helper.GetExternalMethodName();

            string sig = string.Empty;

            if (!string.IsNullOrEmpty(token))
            {
                IDictionary<string, string> sorted = new Dictionary<string, string>();
                ProcessArguments(args, sorted);
                ProcessArguments(new object[] { "page", index.ToString(), "per_page", pageLen.ToString(), "auth_token", token }, sorted);
                sig = base.GetSignature(method, true, sorted);
            }

            IDictionary<string, string> dicionary = new Dictionary<string, string>();

            dicionary.Add(Helper.BASE_URL + "?method", method);
            dicionary.Add("api_key", FLICKR_API_KEY);

            ProcessArguments(args, dicionary);
            ProcessArguments( new object [] {"api_sig", sig, "page", index.ToString(), "per_page", pageLen.ToString(), "auth_token", token }, dicionary);

            string requestUrl = GetUrl(dicionary);

            if (index < 1 || index > 500)
            {
                throw new Exception("Index must be between 1 and 500");
            }

            return GetPhotos(requestUrl, photoSize);
        }
 
        #region PhotoGetBlock
        private IEnumerable<Photo> GetPhotos(string requestUrl, PhotoSize size)
        {
            XElement doc = base.GetElement(requestUrl);
            XElement photosElement = doc.Element("photos");

            RestToCollectionBuilder<Photo> builder = new RestToCollectionBuilder<Photo>("photos");

            RestToCollectionBuilder<Photo.CommonAttribute> commBuilder =
            new RestToCollectionBuilder<Photo.CommonAttribute>("photos");
            Photo.CommonAttribute sharedProperty = commBuilder.ToCollection(doc, null).Single();
         
            return builder.ToCollection(photosElement, photo =>
             {
                 photo.Url = (this as IPhotoRepository).GetSizedPhotoUrl(photo.Id, size) ?? string.Empty;
                 photo.PhotoSize = size;
                 photo.SharedProperty = sharedProperty;
             });
        } 

        #endregion
        
        /// <summary>
        /// calls flickr.photos.getInfo to get the photo object.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="size"></param>
        /// <returns>Detail of photo</returns>
        Photo IPhotoRepository.GetPhotoDetail(string id, PhotoSize size)
        {
            this._PhotoSize = size;
        
            string method = Helper.GetExternalMethodName();

            string token = base.Authenticate(Permission.Delete.ToString(), false);
            string sig = base.GetSignature(method, true, "photo_id", id, "auth_token", token);
            string requestUrl = BuildUrl(method, "photo_id", id, "auth_token", token, "api_sig", sig);

            XElement doc = base.GetElement(requestUrl);

            var query = from photo in doc.Descendants("photo")
                        select new Photo
                                   {
                                       Id = photo.Attribute("id").Value,
                                       FarmId = photo.Attribute("farm").Value,
                                       ServerId = photo.Attribute("server").Value,
                                       SecretId = photo.Attribute("secret").Value,
                                       Title = photo.Element("title").Value,
                                       User = photo.Element("owner").Attribute("username").Value ?? string.Empty,
                                       NsId = photo.Element("owner").Attribute("nsid").Value ?? string.Empty,
                                       Description = photo.Element("description").Value ?? string.Empty,
                                       DateUploaded =  photo.Attribute("dateuploaded").Value,
                                       PTags = (from tag in photo.Descendants("tag")
                                                select new Tag
                                                           {
                                                               Id = tag.Attribute("id").Value,
                                                               Title = tag.Value
                                                           }).ToArray<Tag>(),
                                       PhotoSize = size,
                                       WebUrl = (from photoPage in photo.Descendants("url")
                                                    where photoPage.Attribute("type").Value == "photopage"
                                                    select photoPage.Value
                                                   ).First(),
                                       Url = PhotoDetailUrl(photo.Attribute("id").Value, size)
                                   };

            return query.Single<Photo>();
        }

        private string PhotoDetailUrl(string photoId, PhotoSize size)
        {
            return (this as IPhotoRepository).GetSizedPhotoUrl(photoId, size);
        }

        public void Dispose()
        {

        }

        private static void EncodeAndAddItem(string boundary, ref StringBuilder baseRequest, params object [] items)
        {
            if (baseRequest == null)
            {
                baseRequest = new StringBuilder();
            }

            const string form_data = "Content-Disposition: form-data; name=\"{0}\"\r\n";
            const string photo_key = "Content-Disposition: form-data; name=\"{0}\";filename=\"{1}\"\r\n";
            const string escape = "\r\n";
            const string content_type = "Content-Type:application/octet-stream";
            const string dash = "--";

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

        bool IPhotoRepository.Delete(string photoId)
        {
            string token = base.Authenticate(Permission.Delete.ToString());
            string method = Helper.GetExternalMethodName();

            string sig = base.GetSignature(method, true, "photo_id", photoId, "auth_token", token);
            string requestUrl = BuildUrl(method, "photo_id", photoId, "auth_token", token, "api_sig", sig);

            try
            {
                string responseFromServer = DoHTTPPost(requestUrl);
                XElement element = XElement.Parse(responseFromServer, LoadOptions.None);
                element.ValidateResponse();

                return true;
            }
            catch
            {
                return false;
            }
        }

        string IPhotoRepository.Upload(object[] args, string fileName, byte[] photoData)
        {
            string token = base.Authenticate(Permission.Delete.ToString());

            const string boundary = "FLICKR_BOUNDARY";

            IDictionary<string, string> sorted = new Dictionary<string, string>();

            ProcessArguments(new object[]{ "auth_token", token }, sorted);

            string sig = base.GetSignature(Helper.UPLOAD_URL, false,sorted, args);
           
            StringBuilder builder = new StringBuilder();

            EncodeAndAddItem(boundary, ref builder, new object[] { "api_key", FLICKR_API_KEY, "auth_token", token, "api_sig", sig});
            EncodeAndAddItem(boundary, ref builder, args);
            EncodeAndAddItem(boundary, ref builder, new object[] { "photo", fileName});

            //builder = builder.Remove(builder.Length - 4, 4);
           
            // Create a request using a URL that can receive a post. 
            HttpWebRequest request = (HttpWebRequest) WebRequest.Create(Helper.UPLOAD_URL);
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
            dataStream.Flush();
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
            response.Close();
            reader.Close();
            
            // get the photo id.
            XElement elemnent = ParseElement(responseFromServer);
            return elemnent.Element("photoid").Value ?? string.Empty;
        }

    }
}
