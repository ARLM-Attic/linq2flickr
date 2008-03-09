using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqExtender;
using LinqExtender.Attribute;

namespace Linq.Flickr
{
    public enum TagPeriod
    {
        Day,
        Week
    }
 
    public class HotTag : QueryObjectBase
    {
        public override bool IsNew
        {
            get
            {
                return string.IsNullOrEmpty(Title) ? true : false;
            }
        }

        [LinqVisible(false)]
        public string Title { get; set; }
        [LinqVisible]
        public int Score { get; set; }
        // query params for tag objects.
        [LinqVisible(true), OriginalFieldName("period")]
        public TagPeriod Period { get; set; }
        [LinqVisible(true), OriginalFieldName("count")]
        public int Count { get; set; }
    }
}
