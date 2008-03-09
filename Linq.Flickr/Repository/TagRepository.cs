using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Linq.Flickr.Interface;
using System.Xml.Linq;

namespace Linq.Flickr.Repository
{
    public class TagRepository : Base, ITag
    {
        public TagRepository() : base(typeof(ITag)) { }


        #region ITag Members

        IEnumerable<HotTag> ITag.GetPopularTags(TagPeriod period, int count)
        {
            string method = Helper.GetExternalMethodName();
            string requestUrl = BuildUrl(method, "period", period.ToString().ToLower(), "count", count.ToString());

            XElement element = GetElement(requestUrl);

            var query = from tag in element.Descendants("tag")
                        select new HotTag
                        {
                            Title = tag.Value ?? string.Empty,
                            Score = Convert.ToInt32(tag.Attribute("score").Value ?? string.Empty)
                        };

            return query;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            // nothing here.
        }

        #endregion
    }
}
