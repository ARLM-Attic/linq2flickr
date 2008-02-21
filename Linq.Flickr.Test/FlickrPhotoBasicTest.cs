using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Linq.Flickr;
using System.IO;
using System.Reflection;

namespace Linq.Flickr.Test
{
    [TestFixture]
    public class FlickrPhotoBasicTest
    {
        FlickrContext _context = null;
        IList<string> list = new List<string>();
        private string USER_NAME = "hossain_mehfuz";
        private bool deleteOnce = false;

        [SetUp]
        public void Init()
        {
            _context = new FlickrContext();
            _context.Photos.OnError += new LinqExtender.Query<Photo>.ErrorHandler(Photos_OnError);

            if (!deleteOnce)
            {
                DeleteAllPhotos();
                deleteOnce = true;
            }
        }
        [Test]
        public void AAddNewPhoto()
        {
            using (Stream resourceStream = Assembly.GetAssembly(this.GetType()).GetManifestResourceStream("Linq.Flickr.Test.blank.gif"))
            {
                //byte[] imageByte = new byte[resourceStream.Length];
                //resourceStream.Read(imageByte, 0, (int)resourceStream.Length);

                Photo phtoto = new Photo { Title = "Flickr logo", File = resourceStream, ViewMode = ViewMode.Public };
                // add to the collection.
                _context.Photos.Add(phtoto);
                _context.Photos.SubmitChanges();

                Assert.IsTrue(!string.IsNullOrEmpty(phtoto.Id));

                list.Add(phtoto.Id);
            }
            
        }

        void Photos_OnError(string error)
        {
            Console.Error.WriteLine(error);
        }

        private void DeleteAllPhotos()
        {
            var query =  from ph in _context.Photos
                         where ph.User == USER_NAME && ph.PhotoSize == PhotoSize.Square
                         orderby PhotoOrder.Date_Taken ascending
                         select ph;

            foreach (Photo p in query)
            {
                _context.Photos.Remove(p);
            }
            _context.SubmitChanges();
        }

        [Test, Sequence(2)]
        public void BGetPhotoById()
        {
            try
            {
                var query = from ph in _context.Photos
                            where ph.Id == list[0]
                            select new { ph.Id };

                var item = query.Single();
               
                Assert.IsTrue(item.Id == list[0]);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }
        [Test, Sequence(3)]
        public void CTakeAndSkipTest()
        {
            try
            {
                int page = (list.Count) / 10;

                var query = (from ph in _context.Photos
                             where ph.User == USER_NAME && ph.PhotoSize == PhotoSize.Square
                             orderby PhotoOrder.Date_Taken ascending
                             select ph).Skip(page - 1).Take(10);

                Photo photo = query.Last();

                Assert.IsTrue(photo.Id == list[list.Count - 1]);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
            
        }
        [Test, Sequence(4)]
        public void DDeleteAdded()
        {
            var query = from ph in _context.Photos
                        where ph.Id == list[0]
                        select ph;

            int count = query.Count();

            Assert.IsTrue(count == 1);

            Photo photo = query.First();

            _context.Photos.Remove(photo);
            _context.SubmitChanges();

            query = from ph in _context.Photos
                        where ph.Id == list[0]
                        select ph;

            Assert.IsTrue(query.Count() == 0);
        }

        [TearDown]
        public void Destroy()
        {
            _context = null;
            //DeleteAllPhotos();
        }
      
        
    }
}
