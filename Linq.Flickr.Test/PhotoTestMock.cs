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
    public class PhotoTestMock
    {
        FlickrContext _context = null;
        private const string API_KEY = "cd80fd317eff3714d43fea491bb97f45";
        private const string RESOURCE_NS = "Linq.Flickr.Test.Responses";
       
        [SetUp]
        public void Setup()
        {
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

            Photo first = query.First();

            Assert.IsTrue(first.SharedProperty.Perpage == count);
            Assert.IsTrue(first.Title == "Mug Shot" && first.Id == "2428052817");

            Photo last = query.Last();

            Assert.IsTrue(last.SharedProperty.Page == 1);
            Assert.IsTrue(last.SharedProperty.Total == 105714);
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

        private const string COMMENT_PHOTO_ID = "1x";
       
        [Test]
        [Category("Photo.Comment")]
        public void Photo_Comment()
        {
            MockManager.Init();

            #region Add comment
            Mock photoCommentAdd = MockManager.Mock<CommentRepository>(Constructor.NotMocked);

            photoCommentAdd.ExpectAndReturn("Authenticate", "1234").Args(Permission.Delete.ToString());
            photoCommentAdd.ExpectAndReturn("GetSignature", "yyyy");
            photoCommentAdd.ExpectAndReturn("DoHTTPPost", MockElement(RESOURCE_NS + ".AddComment.xml").ToString());

            Comment comment = new Comment();

            comment.PhotoId = COMMENT_PHOTO_ID;
            comment.Text = "Testing comment add [LINQ.Flickr]";

            _context.Photos.Comments.Add(comment);
            _context.SubmitChanges();

            Assert.IsTrue(comment.Id == "1"); 
            #endregion

            #region Get added comment
            Mock photoCommentMock = MockManager.Mock<CommentRepository>(Constructor.NotMocked);
            Mock httpCallBase = MockManager.Mock<RestToCollectionBuilder<Comment>>(Constructor.NotMocked);

            photoCommentMock.ExpectAndReturn("GetSignature", "yyyy");
            httpCallBase.ExpectAndReturn("GetElement", MockElement(RESOURCE_NS + ".GetComment.xml"));

            var query = from c in _context.Photos.Comments
                        where c.PhotoId == COMMENT_PHOTO_ID && c.Id == comment.Id
                        select c;

            Comment commentGet = query.Single();

            Assert.IsTrue(commentGet.Author == "11" && commentGet.PhotoId == COMMENT_PHOTO_ID && commentGet.AuthorName == "John Doe"); 
            #endregion

            #region Delete added
            Mock photoCommentDelete = MockManager.Mock<CommentRepository>(Constructor.NotMocked);

            photoCommentDelete.ExpectAndReturn("Authenticate", "1234").Args(Permission.Delete.ToString());
            photoCommentDelete.ExpectAndReturn("GetSignature", "yyyy");
            photoCommentDelete.ExpectAndReturn("DoHTTPPost", MockElement(RESOURCE_NS + ".DeleteComment.xml").ToString());

            _context.Photos.Comments.Remove(comment);
            _context.SubmitChanges(); 
            #endregion

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
            _context = null;
        }

        void Photos_OnError(string error)
        {
            Console.Error.WriteLine(error);
            Assert.Fail(error);
        }

    }
}
