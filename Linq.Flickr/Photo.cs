using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Linq.Flickr.Attribute;
using System.IO;
using System.Drawing;
using LinqExtender;
using LinqExtender.Attribute;
using System.Drawing.Imaging;

namespace Linq.Flickr
{
    /// <summary>
    ///  AND means the result will ANED with tags, And OR means it will be ORED.
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
        Date_Taken
    }

    [Serializable]
    public class Photo : QueryObjectBase
    {
        private string _Url = string.Empty;
        [LinqVisible(false), OriginalFieldName("title")]
        public string Title { get; set; }
        [LinqVisible(false), OriginalFieldName("description")]
        public string Description { get; set; }
        [OriginalFieldName("photo_id"), LinqVisible]
        public string Id { get; set; }
        internal string SecretId { get; set; }
        internal string ServerId { get; set; }
        internal string FarmId { get; set; }
        internal string DateUploaded { get; set;}
        internal bool IsPublic { get; set; }
        internal bool IsFriend { get; set; }
        internal bool IsFamily { get; set; }

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
                throw new ApplicationException("Invalid image file");
            }


        }

        public Photo()
        {
            this.ViewMode = ViewMode.Public;
            this.SortOrder = PhotoOrder.Date_Posted;
            this.SearchMode = SearchMode.FreeText;
        }

        private int _size = 0;

        [LinqVisible]
        public PhotoSize PhotoSize
        {
            get
            {
                return (PhotoSize)_size;
            }
            set
            {
                _size = (int)value;
            }
        }


        private int _searchMode = 0;

        [LinqVisible, OriginalFieldName("tag_mode")]
        public SearchMode SearchMode
        {
            get
            {
                return (SearchMode)_searchMode;
            }
            set
            {
                _searchMode = (int)value;
            }
        }

        int _visibility = 0;

        [LinqVisible(), OriginalFieldName("privacy_filter")]
        public ViewMode ViewMode
        {
            get
            {
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
        
        public DateTime UploadedOn
        {
            get
            {
               return new DateTime(long.Parse(DateUploaded));
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

        [LinqVisible(), OriginalFieldName("text")]
        public string SearchText { get; set; }
        [LinqVisible()]
        public string User { get; set; }

        string GetSizePostFix(PhotoSize size)
        {
            string sizePostFx = string.Empty;

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
    }
}
