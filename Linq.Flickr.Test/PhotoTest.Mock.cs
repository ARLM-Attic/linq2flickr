using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Linq.Flickr.Interface;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using Linq.Flickr.Repository;
using TypeMock;

namespace Linq.Flickr.Test
{
    [TestFixture]
    public class PhotoTest
    {
        FlickrContext _context = null;
        private Mock _photoMock = null;
        private const string API_KEY = "cd80fd317eff3714d43fea491bb97f45";
       
        [SetUp]
        public void Setup()
        {
            MockManager.Init();

            _context = new FlickrContext();
            _context.Photos.OnError += new LinqExtender.Query<Photo>.ErrorHandler(Photos_OnError);
            _context.Photos.Comments.OnError += new LinqExtender.Query<Comment>.ErrorHandler(Comments_OnError);
            
        }

        [Test]
        public void SearchPhotos()
        {
            Mock photoMock = MockManager.Mock<PhotoRepository>(Constructor.NotMocked);
            
            string searchUrl = "http://api.flickr.com/services/rest/?method=flickr.photos.search&api_key=" +  API_KEY + "&SearchMode=1&tags=microsoft&page=1&per_page=100";

            photoMock.ExpectAndReturn("GetElement", MockElement("Linq.Flickr.Test.Responses.Search.xml")).Args(searchUrl);

         
            var query = from photo in _context.Photos
                        where photo.SearchMode == SearchMode.TagsOnly && photo.SearchText == "microsoft"
                        select photo;

            int count = query.Count();

            Assert.IsTrue(count == 100);

            Photo first = query.First();

            Assert.IsTrue(first.Title == "Mug Shot" && first.Id == "2428052817");

            Photo last = query.Last();

            Assert.IsTrue(last.Title == "attendee event" && last.Id == "2423378493");

        }

        [Test]
        public void AuthicatedGet()
        {
            MockManager.Init();

            Mock photoMock = MockManager.Mock<PhotoRepository>(Constructor.NotMocked);

            string searchUrl = "http://api.flickr.com/services/rest/?method=flickr.photos.search&api_key=" + API_KEY + "&privacy_filter=0&user_id=xUser&api_sig=yyyy&page=1&per_page=100&auth_token=xyz";

            photoMock.ExpectAndReturn("GetFrob", "1");
            photoMock.ExpectAndReturn("GetSignature", "yyyy");
            photoMock.ExpectAndReturn("CreateDesktopToken", "xyz").Args("1", true, Permission.Delete.ToString().ToLower());
            photoMock.ExpectAndReturn("GetNSID", "xUser").Args("flickr.people.findByUsername", "username", "neetulee");
            photoMock.ExpectAndReturn("GetElement", MockElement("Linq.Flickr.Test.Responses.Owner.xml")).Args(searchUrl);
            
            
            var query = from photo in _context.Photos
                        where photo.ViewMode == ViewMode.Owner && photo.User == "neetulee"
                        select photo;


            Photo lastPhoto = query.Last();

            Assert.IsTrue(lastPhoto.ViewMode == ViewMode.Private);

            MockManager.Verify();
        }

        private XElement MockElement(string resource)
        {
            using (Stream resourceStream = Assembly.GetAssembly(this.GetType()).GetManifestResourceStream(resource))
            {
                XmlReader reader = XmlReader.Create(resourceStream);
                XElement tElement = XElement.Load(reader);
                return tElement;
            }
        }

        void Comments_OnError(string error)
        {
            Console.Out.WriteLine(error);
            Assert.Fail(error);
        }

        [TearDown]
        public void TeadDown()
        {
            MockManager.Verify();
        }

        void Photos_OnError(string error)
        {
            Console.Error.WriteLine(error);
            Assert.Fail(error);
        }

    }
}
