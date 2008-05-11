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
using System.Drawing;

namespace Linq.Flickr.Test
{
    [TestFixture]
    public class PhotoMockTest
    {
        FlickrContext _context = null;
        private const string RESOURCE_NS = "Linq.Flickr.Test.Responses";
       
        [SetUp]
        public void Setup()
        {
            _context = new FlickrContext();
            _context.Photos.OnError += new LinqExtender.Query<Photo>.ErrorHandler(Photos_OnError);
            _context.Photos.Comments.OnError += new LinqExtender.Query<Comment>.ErrorHandler(Comments_OnError);
        }

        [Test]
        public void DoPhotoUploadAndDeleteTest()
        {
            MockManager.Init();

            Stream photoRes = GetResourceStream("Linq.Flickr.Test.blank.gif");
            Photo photo = new Photo { Title = "Flickr logo", FileName = "Test.Mock", File = photoRes, ViewMode = ViewMode.Public };

            using (FakeFlickrRepository<PhotoRepository, Photo> photoAddMock = new FakeFlickrRepository<PhotoRepository, Photo>())
            {
                photoAddMock.MockSignatureCall();
                photoAddMock.MockElementCall(RESOURCE_NS + ".UploadStatus.xml");
                photoAddMock.MockAuthenticateCall(true, Permission.Delete, 2);

                byte[] oImage = new byte[photoRes.Length];

                photoRes.Read(oImage, 0, oImage.Length);
                photoRes.Seek(0, SeekOrigin.Begin);
                
                string path = System.AppDomain.CurrentDomain.BaseDirectory + "\\photo.txt";

                FileStream fileStream = null;

                if (!File.Exists(path))
                    fileStream = File.Create(path);
                else
                    fileStream = File.Open(path, FileMode.Truncate);

                photoAddMock.FakeHttpRequestObject(fileStream);
                photoAddMock.FakeWebResponse_GetResponse();
                photoAddMock.FakeWebResponseObject(RESOURCE_NS + ".Photo.xml");
                
               
                // add to the collection.
                _context.Photos.Add(photo);
                _context.Photos.SubmitChanges();

                // read the binary content from file.
                BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open));

                byte[] content = new byte[reader.BaseStream.Length];

                content = reader.ReadBytes(content.Length);

                reader.Close();

                // end file read

                // construct and verify image 
                byte[] uImage = new byte[oImage.Length];

                byte[] footer = Encoding.UTF8.GetBytes("\r\n--FLICKR_BOUNDARY--\r\n");

                int endIndex = content.Length - footer.Length;
                int startIndex = endIndex - oImage.Length;

                int count = 0;

                for (int index = startIndex; index < endIndex; index++)
                {
                    uImage[count] = content[index];
                    count++;
                }
                MemoryStream mStream = new MemoryStream();

                mStream.Write(uImage, 0, uImage.Length);
                mStream.Seek(0, SeekOrigin.Begin);

                using (Bitmap bitmap = new Bitmap(mStream))
                {

                }

                // end image verification.

                Assert.IsTrue(photo.Id == "1");
            }

            using (FakeFlickrRepository<PhotoRepository, Photo> photoDeleteMock = new FakeFlickrRepository<PhotoRepository, Photo>())
            {
                photoDeleteMock.MockAuthenticateCall(true, Permission.Delete, 1);
                photoDeleteMock.MockSignatureCall();
                photoDeleteMock.MockDoHttpPost(RESOURCE_NS + ".DeletePhoto.xml");

                _context.Photos.Remove(photo);
                _context.SubmitChanges();

                Assert.IsTrue(photo.IsDeleted);
            }

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

            using (FakeFlickrRepository<PhotoRepository, Photo> photoGetMock = new FakeFlickrRepository<PhotoRepository, Photo>())
            {
                photoGetMock.MockElementCall("Linq.Flickr.Test.Responses.Search.xml");

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
            }

            using (FakeFlickrRepository<PhotoRepository, Photo> authenticatedMock = new FakeFlickrRepository<PhotoRepository, Photo>())
            {
                authenticatedMock.MockSignatureCall();
                authenticatedMock.MockCreateAndStoreNewToken(Permission.Delete);
                authenticatedMock.MockGetNSIDByUsername("neetulee");
                authenticatedMock.MockElementCall("Linq.Flickr.Test.Responses.Owner.xml");

                var authQuery = from photo in _context.Photos
                                where photo.ViewMode == ViewMode.Owner && photo.User == "neetulee"
                                select photo;

                Photo lastPhoto = authQuery.Last();

                Assert.IsTrue(lastPhoto.ViewMode == ViewMode.Private);

            }

            using (FakeFlickrRepository<PhotoRepository, Photo> getDetailPhoto = new FakeFlickrRepository<PhotoRepository, Photo>())
            {
                getDetailPhoto.MockAuthenticateCall(false, Permission.Delete, 1);
                getDetailPhoto.MockSignatureCall();
                getDetailPhoto.MockElementCall("Linq.Flickr.Test.Responses.PhotoDetail.xml");
               
                var photoDetailQuery = from photo in _context.Photos
                                       where photo.Id == "xxx" && photo.PhotoSize == PhotoSize.Medium
                                       select photo;

                Photo detailPhoto = photoDetailQuery.Single();

                Assert.IsTrue(detailPhoto.User == "*Park+Ride*");
            }

            MockManager.Verify();
        }

        private const string COMMENT_PHOTO_ID = "1x";
       
        [Test]
        [Category("Photo.Comment")]
        public void DoPhoto_CommentTest()
        {
            MockManager.Init();

            Comment comment = new Comment();

            #region Add comment
            using (FakeFlickrRepository<CommentRepository, Comment> commentAddMock = new FakeFlickrRepository<CommentRepository, Comment>())
            {
                commentAddMock.MockAuthenticateCall(Permission.Delete, 1);
                commentAddMock.MockSignatureCall();
                commentAddMock.MockDoHttpPostAndReturnStringResult(RESOURCE_NS + ".AddComment.xml");

                comment.PhotoId = COMMENT_PHOTO_ID;
                comment.Text = "Testing comment add [LINQ.Flickr]";

                _context.Photos.Comments.Add(comment);
                _context.SubmitChanges();

                Assert.IsTrue(comment.Id == "1");
            }
            #endregion

            #region Get added comment

            using (FakeFlickrRepository<CommentRepository, Comment> commentGetMock = new FakeFlickrRepository<CommentRepository, Comment>())
            {
                commentGetMock.MockSignatureCall();
                commentGetMock.MockRESTBuilderGetElement(RESOURCE_NS + ".GetComment.xml");

                var query = from c in _context.Photos.Comments
                            where c.PhotoId == COMMENT_PHOTO_ID && c.Id == comment.Id
                            select c;

                Comment commentGet = query.Single();

                Assert.IsTrue(commentGet.Author == "11" && commentGet.PhotoId == COMMENT_PHOTO_ID && commentGet.AuthorName == "John Doe");
            }
            #endregion

            #region Delete added

            using (FakeFlickrRepository<CommentRepository, Comment> commentDeleteMock = new FakeFlickrRepository<CommentRepository, Comment>())
            {
                commentDeleteMock.MockAuthenticateCall(Permission.Delete, 1);
                commentDeleteMock.MockSignatureCall();
                commentDeleteMock.MockDoHttpPostAndReturnStringResult(RESOURCE_NS + ".DeleteComment.xml");

                _context.Photos.Comments.Remove(comment);
                _context.SubmitChanges();
            }
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
