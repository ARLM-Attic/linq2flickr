using LinqExtender.Attribute;
using Linq.Flickr.Attribute;
using LinqExtender.Interface;

namespace Linq.Flickr
{
    public enum TagPeriod
    {
        Day,
        Week
    }
 
    [XElement("tag")]
    public class PopularTag : IQueryObject
    {
        [Ignore, XElement("tag")]
        public string Title { get; internal set; }
        [XAttribute("score")]
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

        [OriginalFieldName("period")]
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

        [OriginalFieldName("count"), XAttribute("count")]
        public int Count { get; set; }
    }
}
