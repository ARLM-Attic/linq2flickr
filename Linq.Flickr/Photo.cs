using System;
using System.IO;
using System.Drawing;
using Linq.Flickr.Attribute;
using LinqExtender;
using LinqExtender.Attribute;

namespace Linq.Flickr
{
    /// <summary>
    ///  AND means the result will ANDED with tags, And OR means it will be ORED.
    /// </summary>
    public enum TagMode
    {
        AND = 0,
        OR
    }
    public enum PhotoSize
    {
        Square,
        Thumbnail,
        Small,
        Medium,
        Original,
        Default
    }

    public enum ViewMode
    {
        Owner,
        Public = 1,
        Friends,
        Family,
        FriendsFamily,
        Private
    }

    public enum SearchMode
    {
        FreeText,
        TagsOnly
    }

    public enum PhotoOrder
    {
        Date_Posted,
        Date_Taken,
        Interestingness
    }

    public enum FilterMode
    {
        Safe = 1,
        Moderate = 2,
        Restricted = 3
    }

    [Serializable, XElement("photo")]
    public class Photo : QueryObjectBase
    {
        private string _Url = string.Empty;
        [LinqVisible(false), OriginalFieldName("title"), XAttribute("title")]
        public string Title { get; set; }
        [LinqVisible(false), OriginalFieldName("description")]
        public string Description { get; set; }
        [OriginalFieldName("photo_id"), LinqVisible, UniqueIdentifier, XAttribute("id")]
        public string Id { get; set; }

        private string _webUrl = string.Empty;
        /// <summary>
        /// The original url of the photo in flickr page.
        /// </summary>
        public string WebUrl
        {
            get
            {
                _webUrl = string.Format("http://www.flickr.com/photos/{0}/{1}/", NsId, Id);
                return _webUrl;
            }
            internal set
            {
                _webUrl = value;   
            }
        }
        [XAttribute("secret")]
        internal string SecretId { get; set; }
        [XAttribute("server")]
        internal string ServerId { get; set; }
        [XAttribute("farm")]
        internal string FarmId { get; set; }
        [XAttribute("dateupload")]
        internal string DateUploaded { get; set;}
        /// <summary>
        /// tied to Extras option
        /// </summary>
        [XAttribute("lastupdate")]
        internal string LastUpdated { get; set; }
        /// <summary>
        /// tied to Extras option
        /// </summary>
        [XAttribute("datetaken")]
        internal string DateTaken { get; set; }
        [XAttribute("ispublic")]
        internal bool IsPublic { get; set; }
        [XAttribute("isfriend")]
        internal bool IsFriend { get; set; }
        [XAttribute("isfamily")]
        internal bool IsFamily { get; set; }
       /// <summary>
       /// tied to Extras option
       /// </summary>
        [XAttribute("license")]
        public string License { get; internal set; }
        /// <summary>
        /// tied to Extras option
        /// </summary>
        [XAttribute("views")]
        public int Views { get; internal set; }
        /// <summary>
        /// tied to Extras option
        /// </summary>
        [XAttribute("owner_name")]
        public string OwnerName { get; internal set; }
        /// <summary>
        /// tied to Extras option
        /// </summary>
        [XAttribute("media")]
        public string Media { get; internal set; }
        /// <summary>
        /// tied to Extras option
        /// </summary>
        [XAttribute("original_format")]
        public string OriginalFormat { get; internal set; }
        /// <summary>
        /// tied to Extras option Geo
        /// </summary>
        [XAttribute("latitude")]
        public string Latitude { get; internal set; }
        /// <summary>
        /// tied to Extras option Geo
        /// </summary>
        [XAttribute("longitude")]
        public string Longitude { get; internal set; }
        /// <summary>
        /// tied to Extras option Geo
        /// </summary>
        [XAttribute("accuracy")]
        public string Accuracy { get; internal set; }

        private int _filterMode;

        [OriginalFieldName("safe_search"), LinqVisible]
        public FilterMode FilterMode
        {
            get
            {
                return (FilterMode)_filterMode;
            }
            internal set
            {
                _filterMode = (int)value;
            }      
        }


        public override bool IsNew
        {
            get
            {
                return string.IsNullOrEmpty(this.Id) ? true : false;
            }
        }
        
        private string _uploadFilename = string.Empty;

        [LinqVisible(false), OriginalFieldName("photo")]
        public string FileName
        {
            set
            {
                _uploadFilename = value;
            }
            get
            {
                if (string.IsNullOrEmpty(_uploadFilename))
                {
                    _uploadFilename = Guid.NewGuid().ToString();
                }
                return _uploadFilename;
            }
        }

        public string FilePath { get; set; }
        public Stream File { get; set; }

        private byte[] _postContent = null;

        [LinqVisible(false)]
        public byte[] PostContent
        {
            get
            {
                if (_postContent == null)
                {
                    _postContent = GetBytesFromPhysicalFile();
                }
                return _postContent;
            }
      
        }


        internal byte[] GetBytesFromPhysicalFile()
        {
            Stream stream = null;
       
            try
            {
                if (File != null)
                {
                    stream = File;
                }
                else
                {
                    stream = new FileStream(FilePath, FileMode.Open);
                }

                using (Bitmap bitmap = new Bitmap(stream))
                {
                    //bitmap.v(stream, ImageFormat.Jpeg);
                }

                byte[] image = new byte[stream.Length];

                stream.Seek(0, SeekOrigin.Begin);
                stream.Read(image, 0, image.Length);

                return image;
            }
            catch
            {
                return null;
            }
        }

        public Photo()
        {
            IsPublic = true;
            this.SortOrder = PhotoOrder.Date_Posted;
            this.SearchMode = SearchMode.FreeText;
            this.FilterMode = FilterMode.Safe;
        }

        private int _size = 0;

        [LinqVisible]
        public PhotoSize PhotoSize
        {
            get
            {
                return (PhotoSize)_size;
            }
            internal set
            {
                _size = (int)value;
            }
        }

        private int _tagMode = 0;

        [LinqVisible, OriginalFieldName("tag_mode")]
        public TagMode TagMode
        {
            get
            {
                return (TagMode)_tagMode;
            }
            internal set 
            {
                _tagMode = (int)value;
            }
        }


        private int _searchMode = 0;

        [LinqVisible]
        public SearchMode SearchMode
        {
            get
            {
                return (SearchMode)_searchMode;
            }
            internal set
            {
                _searchMode = (int)value;
            }
        }
        /// <summary>
        /// A comma-delimited list of extra information to fetch for each returned record. 
        /// Currently supported fields are: license, date_upload, date_taken, owner_name, icon_server, 
        /// original_format, last_update, geo, tags, machine_tags, o_dims, views, media. 
        /// </summary>
        [LinqVisible, OriginalFieldName("extras")]
        public string Extras { get; internal set; }
  
        int _visibility = 0;

        [LinqVisible(), OriginalFieldName("privacy_filter")]
        public ViewMode ViewMode
        {
            get
            {
                if (!IsPublic)
                    _visibility = (int)ViewMode.Private;
                else if (IsFriend)
                    _visibility = (int)ViewMode.Friends;
                else if (IsFamily)
                    _visibility = (int)ViewMode.Family;
                else
                    _visibility = (int)ViewMode.Public;

                return (ViewMode)_visibility;
            }
            set
            {
                _visibility = (int)value;
            }
        }

        private int _sortOrder = 0;

        public PhotoOrder SortOrder
        {
            get
            {
                return (PhotoOrder)_sortOrder;
            }
            set
            {
                _sortOrder = (int)value;
            }
        }
        /// <summary>
        /// date when photo is uploaded in flickr
        /// </summary>
        public DateTime UploadedOn
        {
            get
            {
                return DateUploaded.GetDate();
            }
        }
        /// <summary>
        /// date when photo is updated in flickr
        /// </summary>
        public DateTime UpdatedOn
        {
            get
            {
                return LastUpdated.GetDate();
            }
        }

        /// <summary>
        /// the date when the photo is taken.
        /// </summary>
        public DateTime TakeOn
        {
            get
            {
                return DateTime.Parse(DateTaken);
            }
        }

        internal Tag[] PTags { get; set; }
        public Tag[] PhotoTags { get { return PTags; } }

        private string[] _tags = new string[0];

        internal string Tags 
        {
            get
            {
                return string.Join(",", _tags).Replace(" ", string.Empty);
            }
            set
            {
                _tags = value.Split(new char[] { ',', ';'}, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        /// <summary>
        ///  text on which to search on flickr.
        /// </summary>
        [LinqVisible, OriginalFieldName("text")]
        public string SearchText { get; internal set; }
        /// <summary>
        /// Use to query user in flickr, is filled up only when a photo is get by photoId.
        /// </summary>
        [LinqVisible]
        public string User { get; internal set; }
        /// <summary>
        /// this is the unique Id aginst username, is availble with data only by GetPhotoDetail
        /// </summary>
        [XAttribute("owner")]
        public string NsId { get; internal set; }    

        private string GetSizePostFix(PhotoSize size)
        {
            string sizePostFx;

            switch (size)
            {
                case PhotoSize.Square:
                    sizePostFx = "_s";
                    break;
                case PhotoSize.Small:
                    sizePostFx = "_m";
                    break;
                case PhotoSize.Thumbnail:
                    sizePostFx = "_t";
                    break;
                default:
                    sizePostFx = string.Empty;
                    break;

            }
            return sizePostFx;
        }


        public string Url
        {
            get
            {
                if (string.IsNullOrEmpty(_Url))
                {
                    _Url = "http://farm" + FarmId + ".static.flickr.com/" + ServerId + "/" + Id + "_" + SecretId + GetSizePostFix(PhotoSize) + ".jpg?v=0";
                }
                return _Url;
            }
            set
            {
                _Url = value;
            }
        }

        [XElement("photos")]
        public class CommonAttribute : IDisposable
        {
            [XAttribute("page")]
            public int Page { get; set; }
            [XAttribute("pages")]
            public int Pages { get; set; }
            [XAttribute("perpage")]
            public int Perpage { get; set; }
            [XAttribute("total")]
            public int Total { get; set; }

            #region IDisposable Members

            void IDisposable.Dispose()
            {
                //throw new NotImplementedException();
            }

            #endregion
        }

        /// <summary>
        /// holds out the common propeties like page , total page count and total item count
        /// </summary>
        public CommonAttribute SharedProperty { get; set; }
    }

   
}
