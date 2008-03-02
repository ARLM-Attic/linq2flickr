using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqExtender.Attribute;
using LinqExtender;

namespace Linq.Flickr
{
   [Serializable]
    public class Comment : QueryObjectBase
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

        [LinqVisible(), OriginalFieldName("id")]
        public string Id { get; set; }

        [LinqVisible(), OriginalFieldName("photo_id")]
        public string PhotoId { get; set; }

        private Author _author = null;

        [LinqVisible(false)]
        public Author Author
        {
            get
            {
                if (_author == null)
                    _author = new Author();
                return _author;
            }
        }

        [LinqVisible(false)]
        public string PermaLink { get; set; }
        [LinqVisible(false)]
        public string Text { get; set; }

        internal string PDateCreated { get; set; }

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
