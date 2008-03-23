using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Linq.Flickr;
using System.IO;
using System.Reflection;
using System.Configuration;

namespace Linq.Flickr.Test
{
    [TestFixture]
    public class FlickrPhotoBasicTest
    {
        FlickrContext _context = null;
        IList<string> _list = new List<string>();
        private string USER_NAME = System.Configuration.ConfigurationManager.AppSettings["USERNAME"];
        private bool deleteOnce = false;

        [SetUp]
        public void Init()
        {
            _list.Clear();
            _context = new FlickrContext();
            _context.Photos.OnError += new LinqExtender.Query<Photo>.ErrorHandler(Photos_OnError);
            _context.Photos.Comments.OnError += new LinqExtender.Query<Comment>.ErrorHandler(Comments_OnError);

            if (!deleteOnce)
            {
                DeleteAllPhotos();
                deleteOnce = true;
            }
            AddNewPhoto();
        }

        void Comments_OnError(string error)
        {
            Console.Out.WriteLine(error);
            Assert.Fail(error);
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

                _list.Add(phtoto.Id);
            }

        }

        void Photos_OnError(string error)
        {
            Console.Error.WriteLine(error);
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

        [Test]
        public void CommentTest()
        {
            Comment comment = new Comment();

            comment.PhotoId = _list[0];
            comment.Text = "Testing comment add [LINQ.Flickr]";

            _context.Photos.Comments.Add(comment);
            _context.SubmitChanges();

            Assert.IsTrue(!string.IsNullOrEmpty(comment.Id));

            Console.Out.WriteLine("Getting .. comments");

            var query = from c in _context.Photos.Comments
                        where c.PhotoId == comment.PhotoId && c.Id == comment.Id
                        select c;

            Assert.IsTrue(query.Count() == 1);

            _context.Photos.Comments.Remove(query.Single<Comment>());

            Console.Out.WriteLine("Delete comment " + comment.Id);

            _context.SubmitChanges();

            Console.Out.WriteLine("Done");
        }

        

        [Test, Sequence(2)]
        public void GetPhotoById()
        {
            try
            {
                var query = from ph in _context.Photos
                            where ph.Id == _list[0]
                            select new { ph.Id };

                var item = query.Single();

                Assert.IsTrue(item.Id == _list[0]);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }
        [Test, Sequence(3)]
        public void TakeAndSkipTest()
        {
            try
            {
                int page = (_list.Count) / 10;

                var query = (from ph in _context.Photos
                             where ph.User == USER_NAME && ph.PhotoSize == PhotoSize.Square
                             orderby PhotoOrder.Date_Taken ascending
                             select ph).Skip(page - 1).Take(10);

                Photo photo = query.Last();

                Assert.IsTrue(photo.Id == _list[_list.Count - 1]);
            }
            catch (Exception ex)
            {
                Assert.Fail(ex.Message);
            }

        }
        public void DeleteAdded()
        {
            var query = from ph in _context.Photos
                        where ph.Id == _list[0]
                        select ph;

            int count = query.Count();

            Assert.IsTrue(count == 1);

            Photo photo = query.First();

            _context.Photos.Remove(photo);
            _context.SubmitChanges();

            query = from ph in _context.Photos
                    where ph.Id == _list[0]
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
