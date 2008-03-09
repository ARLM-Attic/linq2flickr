using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Linq.Flickr.Attribute;

namespace Linq.Flickr.Interface
{
    public interface ITag : IDisposable
    {
        // comment 
        [FlickrMethod("flickr.tags.getHotList")]
        IEnumerable<HotTag> GetPopularTags(TagPeriod period, int count);
    }
}
