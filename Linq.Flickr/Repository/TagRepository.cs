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

        IEnumerable<PopularTag> ITag.GetPopularTags(TagPeriod period, int count)
        {
            string method = Helper.GetExternalMethodName();
            string requestUrl = BuildUrl(method, "period", period.ToString().ToLower(), "count", count.ToString());
           
            RestToCollectionBuilder<PopularTag> builder = new RestToCollectionBuilder<PopularTag>("hottags");

            return builder.ToCollection(requestUrl);
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
