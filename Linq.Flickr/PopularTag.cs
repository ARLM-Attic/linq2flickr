using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqExtender;
using LinqExtender.Attribute;
using Linq.Flickr.Attribute;

namespace Linq.Flickr
{
    public enum TagPeriod
    {
        Day,
        Week
    }
 
    [XElement("tag")]
    public class PopularTag : QueryObjectBase
    {
        [LinqVisible(false), XElement("tag")]
        public string Title { get; internal set; }
        [LinqVisible, XAttribute("score")]
        public int Score { get; internal set; }
        // query params for tag objects.
        [XAttribute("period")]
        internal string pPeriod
        {
            set
            {
                if (value == "week")
                {
                    _period = TagPeriod.Week;
                }
                else
                {
                    _period = TagPeriod.Day;
                }
            }
        }

        TagPeriod _period = TagPeriod.Day;

        [LinqVisible(true), OriginalFieldName("period")]
        public TagPeriod Period
        {
            get
            {
                return _period;
            }
            set
            {
                _period = value;
            }
        }

        [LinqVisible(true), OriginalFieldName("count"), XAttribute("count")]
        public int Count { get; set; }
    }
}
