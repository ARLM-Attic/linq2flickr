using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Linq.Flickr;
using System.IO;
using System.Reflection;
using System.Configuration;
using LinqExtender;

namespace Linq.Flickr.Test
{
    //[TestFixture] -- not needed basically.
    public class FlickrPhotoBasicTest
    {
        FlickrContext _context = null;
        IList<string> list = new List<string>();
        private string USER_NAME = ConfigurationManager.AppSettings["USERNAME"];
        private bool deleteOnce = false;

        [SetUp]
        public void Init()
        {
            list.Clear();
            _context = new FlickrContext();
            _context.Photos.OnError += new LinqExtender.Query<Photo>.ErrorHandler(Photos_OnError);
            _context.Photos.Comments.OnError += new LinqExtender.Query<Comment>.ErrorHandler(Comments_OnError);
            
            if (!deleteOnce)
            {
                DeleteAllPhotos();
                deleteOnce = true;
            }
        }

        [Test]
        public void DoOriginalAdd()
        {
            AddNewPhoto();
        }

        void Comments_OnError(ProviderException ex)
        {
            Console.Out.WriteLine(ex.Message);
            Assert.Fail(ex.StackTrace);
        }
        public void AddNewPhoto()
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

        void Photos_OnError(ProviderException ex)
        {
            Console.Error.WriteLine(ex.Message);
        }

        private void DeleteAllPhotos()
        {
            var query = from ph in _context.Photos
                        where ph.User == USER_NAME && ph.PhotoSize == PhotoSize.Square
                        orderby PhotoOrder.Date_Taken ascending
                        select ph;

            foreach (Photo p in query)
            {
                _context.Photos.Remove(p);
            }
            _context.SubmitChanges();
        }
        
        //[Test, Sequence(3)]
        //public void TakeAndSkipTest()
        //{
        //    try
        //    {
        //        int page = (list.Count) / 10;

        //        var query = (from ph in _context.Photos
        //                     where ph.User == USER_NAME && ph.PhotoSize == PhotoSize.Square
        //                     orderby PhotoOrder.Date_Taken ascending
        //                     select ph).Skip(page - 1).Take(10);

        //        Photo photo = query.Last();

        //        Assert.IsTrue(photo.Id == list[list.Count - 1]);
        //    }
        //    catch (Exception ex)
        //    {
        //        Assert.Fail(ex.Message);
        //    }

        //}
        public void DeleteAdded()
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
            DeleteAdded();
            //DeleteAllPhotos();
            _context = null;
        }


    }
}
