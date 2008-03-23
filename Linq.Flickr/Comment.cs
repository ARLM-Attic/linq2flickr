using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqExtender.Attribute;
using LinqExtender;
using Linq.Flickr.Attribute;

namespace Linq.Flickr
{
    /// <summary>
    ///  Holds comment informaton for a photo.
    /// </summary>
    [Serializable, XElement("comment")]
    public partial class Comment : QueryObjectBase
    {
        public Comment()
        {
        }

        public override bool IsNew
        {
            get
            {
                return string.IsNullOrEmpty(this.Id) ? true : false;
            }
        }

        [LinqVisible(), OriginalFieldName("id"), XAttribute("id")]
        public string Id { get; set; }

        [LinqVisible(), OriginalFieldName("photo_id"), XAttribute("photo_id")]
        public string PhotoId { get; set; }

        [LinqVisible(false), XAttribute("permalink")]
        public string PermaLink { get; set; }
        [LinqVisible(false), XElement("comment")]
        public string Text { get; set; }

        [XAttribute("datecreate")]
        internal string PDateCreated { get; set; }

        [XAttribute("author")]
        public string Author { get; set; }
        [XAttribute("authorname")]
        public string AuthorName { get; set; }

        public DateTime DateCreated
        {
            get
            {
                long longdate = 0;
                long.TryParse(PDateCreated, out longdate);
                return new DateTime(longdate);
            }
        }
    }
}
