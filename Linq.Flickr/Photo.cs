using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Linq.Flickr.Attribute;
using System.IO;
using System.Drawing;

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

    public enum SortOrder
    {
        Date_Posted_Desc,
        Date_Posted_Asc,
        Date_Taken_Asc,
        Date_Taken_Dsc,
        Interestingness_Desc,
        Interestingness_Asc,
        Relevance
    }

    [Serializable]
    public class Photo
    {
        private string _Url = string.Empty;
        [UseInExpression(false)]
        public string Title { get; set; }
        [UseInExpression(false)]
        public string Description { get; set; }
        public string Id { get; set; }
        internal string SecretId { get; set; }
        internal string ServerId { get; set; }
        internal string FarmId { get; set; }
        internal string DateUploaded { get; set;}
        internal bool IsPublic { get; set; }
        internal bool IsFriend { get; set; }
        internal bool IsFamily { get; set; }
        
        internal bool IsDeleted { get; set; }

        internal bool IsNew 
        {
            get
            {
                return string.IsNullOrEmpty(this.Id) ? true : false;
            } 
        }

        private string _uploadFilename = string.Empty;

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
                    // valid image
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
            this.PhotoSize = PhotoSize.Square;
            this.ViewMode = ViewMode.Public;
            this.SortOrder = SortOrder.Date_Posted_Desc;
            this.SearchMode = SearchMode.FreeText;
        }

        private int _size = 0;

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

        public SortOrder SortOrder
        {
            get
            {
                return (SortOrder)_sortOrder;
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

        public string SearchText { get; set; }
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
