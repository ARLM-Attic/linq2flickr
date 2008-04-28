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
using System.Net;
using System.Security.Cryptography;

namespace Linq.Flickr.Test
{
    [TestFixture]
    public class PhotoMockTest
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
        public void DoPhotoUploadTest()
        {
            MockManager.Init();

            Mock photoMock = MockManager.Mock<PhotoRepository>(Constructor.NotMocked);

            photoMock.ExpectAndReturn("GetSignature", "yyyy", 2);
            photoMock.ExpectAndReturn("Authenticate", "1234", 2).Args(true, Permission.Delete.ToString());
            photoMock.ExpectAndReturn("GetElement", MockElement(RESOURCE_NS + ".UploadStatus.xml"));

            Photo photo = new Photo { Title = "Flickr logo", FileName ="Test.Mock", File = GetResourceStream("Linq.Flickr.Test.blank.gif"), ViewMode = ViewMode.Public };
           
            Mock httpRequest = MockManager.Mock(typeof(HttpWebRequest));
      
            string path = System.AppDomain.CurrentDomain.BaseDirectory + "\\photo.txt";

            FileStream fileStream = null;
            
            if (!File.Exists(path))
                fileStream = new FileStream(path, FileMode.OpenOrCreate);
            else
                fileStream = new FileStream(path, FileMode.Truncate);

            httpRequest.ExpectSet("ContentType");
            httpRequest.ExpectAndReturn("GetRequestStream", fileStream);

            MockObject responseObject = MockManager.MockObject<WebResponse>();
            httpRequest.ExpectAndReturn("GetResponse", responseObject.Object);

            responseObject.ExpectAndReturn("GetResponseStream", GetResourceStream(RESOURCE_NS + ".Photo.xml"));
            responseObject.ExpectCall("Close");

            // add to the collection.
            _context.Photos.Add(photo);
            _context.Photos.SubmitChanges();
           
            //string content = reader.ReadToEnd();

            TextReader reader = new StreamReader(new FileStream(path, FileMode.Open));
            
            string content = reader.ReadToEnd();
            string generatedHash = GetHash(content);
            
            reader.Close();

            reader = new StreamReader(GetResourceStream(RESOURCE_NS + ".PicPostData.txt"));

            content = reader.ReadToEnd();

            string originalHash = GetHash(content);

            reader.Close();

            //Assert.IsTrue(string.Compare(generatedHash, originalHash, true) == 0);

            Assert.IsTrue(photo.Id == "1");

            MockManager.Verify();
        }

        internal string GetHash(string inputString)
        {
            MD5 md5 = MD5CryptoServiceProvider.Create();

            byte[] input = Encoding.UTF8.GetBytes(inputString);
            byte[] output = MD5.Create().ComputeHash(input);

            return BitConverter.ToString(output).Replace("-", "").ToLower();
        }

        private Stream GetResourceStream(string name)
        {
            return Assembly.GetAssembly(this.GetType()).GetManifestResourceStream(name);
        }

        [Test]
        public void DoPhotoTest()
        {
            MockManager.Init();

            Mock normSearchMock = MockManager.Mock<PhotoRepository>(Constructor.NotMocked);

            //string searchUrl = "http://api.flickr.com/services/rest/?method=flickr.photos.search&api_key=" +  API_KEY + "&SearchMode=1&tags=microsoft&page=1&per_page=100";

            normSearchMock.ExpectAndReturn("GetElement", MockElement("Linq.Flickr.Test.Responses.Search.xml"));


            var searchQuery = from photo in _context.Photos
                              where photo.SearchMode == SearchMode.TagsOnly && photo.SearchText == "microsoft"
                              select photo;

            int count = searchQuery.Count();

            Photo first = searchQuery.First();

            Assert.IsTrue(first.SharedProperty.Perpage == count);
            Assert.IsTrue(first.Title == "Mug Shot" && first.Id == "2428052817");

            Photo last = searchQuery.Last();

            Assert.IsTrue(last.SharedProperty.Page == 1);
            Assert.IsTrue(last.SharedProperty.Total == 105714);
            Assert.IsTrue(last.Title == "attendee event" && last.Id == "2423378493");

            Mock authenticatedMock = MockManager.Mock<PhotoRepository>(Constructor.NotMocked);

            //string searchUrl = "http://api.flickr.com/services/rest/?method=flickr.photos.search&api_key=" + API_KEY + "&privacy_filter=0&user_id=xUser&api_sig=yyyy&page=1&per_page=100&auth_token=xyz";

            authenticatedMock.ExpectAndReturn("GetFrob", "1");
            authenticatedMock.ExpectAndReturn("GetSignature", "yyyy");
            authenticatedMock.ExpectAndReturn("CreateDesktopToken", "xyz").Args("1", true, Permission.Delete.ToString().ToLower());
            authenticatedMock.ExpectAndReturn("GetNSID", "xUser").Args("flickr.people.findByUsername", "username", "neetulee");
            authenticatedMock.ExpectAndReturn("GetElement", MockElement("Linq.Flickr.Test.Responses.Owner.xml"));

            var authQuery = from photo in _context.Photos
                            where photo.ViewMode == ViewMode.Owner && photo.User == "neetulee"
                            select photo;

            Photo lastPhoto = authQuery.Last();

            Assert.IsTrue(lastPhoto.ViewMode == ViewMode.Private);

            MockManager.Verify();
        }

        private const string COMMENT_PHOTO_ID = "1x";
       
        [Test]
        [Category("Photo.Comment")]
        public void DoPhoto_CommentTest()
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

        [Test]
        public void DoPopularTagTest()
        {
            MockManager.Init();

            Mock restBuilderMock = MockManager.Mock<RestToCollectionBuilder<PopularTag>>(Constructor.NotMocked);
            // set the expectation.
            restBuilderMock.ExpectAndReturn("GetElement", MockElement(RESOURCE_NS + ".HotTagGetList.xml"));

            var query = from tag in _context.PopularTags
                        where tag.Period == TagPeriod.Day && tag.Count == 6
                        orderby tag.Score ascending
                        select tag;

            int count = query.Count();
            // see if the expected and returned value are same.
            Assert.IsTrue(count == 6);

            PopularTag firstTag = query.First();

            Assert.IsTrue(firstTag.Score == 4);

            PopularTag lastTag = query.Last();

            Assert.IsTrue(lastTag.Score == 20);

            MockManager.Verify();
        }

        [Test]
        public void DoPeopleTest()
        {
            MockManager.Init();

            string id = "12037949754@N01";

            Mock photoMock = MockManager.Mock<PhotoRepository>(Constructor.NotMocked);
            Mock peopleMock = MockManager.Mock<PeopleRepository>(Constructor.NotMocked);
            Mock restBuilderMock = MockManager.Mock<RestToCollectionBuilder<People>>(Constructor.NotMocked);

            // this also ensures that GetNSID should be called from photo repo only.
            photoMock.ExpectAndReturn("GetNSID", id);
            peopleMock.ExpectAndReturn("GetSignature", "yyyy");
            restBuilderMock.ExpectAndReturn("GetElement", MockElement(RESOURCE_NS + ".PeopleInfo.xml"));

            var query = from p in _context.Peoples
                        where p.Username == "bees"
                        select p;

            People people = query.Single();

            Assert.IsTrue(people.Id == id && people.Username == "bees");
          
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
