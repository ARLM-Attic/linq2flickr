using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Linq.Flickr.Test
{
    [TestFixture]
    public class MiscTest
    {
        FlickrContext _context = null;

        [SetUp]
        public void Init()
        {
            _context = new FlickrContext();
            _context.HotTags.OnError += new LinqExtender.Query<HotTag>.ErrorHandler(HotTags_OnError);
        }

        void HotTags_OnError(string error)
        {
            Console.Out.WriteLine(error);
            Assert.Fail(error);
        }

        [Test]
        public void PopularTagTest()
        {
            var query = from tag in _context.HotTags
                        where tag.Period == TagPeriod.Week && tag.Count == 10
                        select tag;

            int count = query.Count();

            Assert.IsTrue(count == 10);

            foreach (HotTag tagObject in query)
            {
                Console.Out.WriteLine(tagObject.Title);
            }
        }

        [TearDown]
        public void Destroy()
        {
            _context = null;
        }
    }
}
