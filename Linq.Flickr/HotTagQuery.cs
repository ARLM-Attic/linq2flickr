using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqExtender;
using Linq.Flickr.Interface;
using Linq.Flickr.Repository;

namespace Linq.Flickr
{
    public class HotTagQuery : Query<HotTag>
    {
        protected override void AddItem(Bucket item)
        {
            throw new Exception("Add not supported for hot tags");
        }

        protected override void RemoveItem(Bucket item)
        {
            throw new Exception("Remove not supported for hot tags");
        }

        protected override void Process(LinqExtender.Interface.IModify<HotTag> items, Bucket bucket)
        {
            object tagsPeriod = bucket.Items[TagColums.PERIOD].Value;
            TagPeriod period =  tagsPeriod == null ? TagPeriod.Day : (TagPeriod)tagsPeriod;

            int score = Convert.ToInt32(bucket.Items[TagColums.SCORE].Value ?? "0");

            int count = (int)bucket.Items[TagColums.COUNT].Value;

            if (count > 200)
            {
                throw new Exception("Tag count should be less than 200");
            }

            using (ITag tagRepo = new TagRepository())
            {
               IEnumerable<HotTag> tags = tagRepo.GetPopularTags(period, count);

               // do the filter on score.
               if (score > 0)
               {
                   var query = from tag in tags
                               where tag.Score == score
                               select tag;
                   tags = query;
               }
               items.AddRange(tags);
            }
        }

        public class TagColums
        {
            public const string PERIOD = "Period";
            public const string COUNT = "Count";
            public const string SCORE = "Score";
        }
    }
}
