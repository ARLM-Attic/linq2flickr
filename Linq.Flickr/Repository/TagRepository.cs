using System.Collections.Generic;
using Linq.Flickr.Interface;

namespace Linq.Flickr.Repository
{
    public class TagRepository : BaseRepository, ITagRepository
    {
        public TagRepository() : base(typeof(ITagRepository)) { }


        #region ITagRepository Members

        IEnumerable<PopularTag> ITagRepository.GetPopularTags(TagPeriod period, int count)
        {
            string method = Helper.GetExternalMethodName();
            string requestUrl = BuildUrl(method, "period", period.ToString().ToLower(), "count", count.ToString());
           
            CollectionBuilder<PopularTag> builder = new CollectionBuilder<PopularTag>("hottags");

            return builder.ToCollection(requestUrl, null);
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
