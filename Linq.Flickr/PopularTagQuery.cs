using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqExtender;
using Linq.Flickr.Interface;
using Linq.Flickr.Repository;
using System.Collections;

namespace Linq.Flickr
{
    public class PopularTagQuery : Query<PopularTag>
    {
        protected override bool AddItem()
        {
            return false;
        }

        protected override bool RemoveItem()
        {
            return false;
        }

        protected override void Process(LinqExtender.Interface.IModify<PopularTag> items)
        {
            object tagsPeriod = Bucket.Instance.For.Item(TagColums.Period).Value;
            TagPeriod period =  tagsPeriod == null ? TagPeriod.Day : (TagPeriod)tagsPeriod;

            int score = Convert.ToInt32(Bucket.Instance.For.Item(TagColums.Score).Value ?? "0");

            int count = (int)Bucket.Instance.For.Item(TagColums.Count).Value;

            if (count > 200)
            {
                throw new Exception("Tag count should be less than 200");
            }

            using (ITagRepository tagRepositoryRepo = new TagRepository())
            {
               IEnumerable<PopularTag> tags = tagRepositoryRepo.GetPopularTags(period, count);
               // do the filter on score.
             
               if (score > 0)
               {
                   tags = tags.Where(tag => tag.Score == score).Select(tag => tag);
               }

               items.AddRange(tags, true);
            }
        }

        public class TagColums
        {
            public const string Period = "Period";
            public const string Count = "Count";
            public const string Score = "Score";
        }
    }
}
