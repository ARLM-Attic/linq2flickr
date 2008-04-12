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
        private string USER_NAME = System.Configuration.ConfigurationManager.AppSettings["USERNAME"];

        [SetUp]
        public void Init()
        {
            _context = new FlickrContext();
            _context.PopularTags.OnError += new LinqExtender.Query<PopularTag>.ErrorHandler(HotTags_OnError);
        }

        void HotTags_OnError(string error)
        {
            Console.Out.WriteLine(error);
            Assert.Fail(error);
        }

        [Test]
        public void PopularTagTest()
        {
            var query = from tag in _context.PopularTags
                        where tag.Period == TagPeriod.Week && tag.Count == 10 orderby tag.Title ascending
                        select tag;

            int count = query.Count();

            Assert.IsTrue(count == 10);

            foreach (PopularTag tagObject in query)
            {
                Console.Out.WriteLine(tagObject.Title);
            }
        }

        [Test]
        public void PeopleTest()
        {
            var query = from people in _context.Peoples
                        where people.Username == USER_NAME
                        select people;

            People p = query.Single();

            var query2 = from people in _context.Peoples
                         where people.Id == p.Id
                         select new { people.IconUrl };

            Assert.IsTrue(query2.Count() == 1);

            var icon = query2.Single();

            Assert.IsTrue(icon.IconUrl.Equals(p.IconUrl));

            Console.Out.WriteLine(icon.IconUrl);
                          
        }
       

        [TearDown]
        public void Destroy()
        {
            _context = null;
        }
    }
}
