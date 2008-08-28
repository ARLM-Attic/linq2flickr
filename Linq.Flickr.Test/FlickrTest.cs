using System;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.IO;
using System.Reflection;
using System.Xml;
using Linq.Flickr.Repository;
using TypeMock;
using System.Security.Cryptography;
using System.Drawing;
using LinqExtender;

namespace Linq.Flickr.Test
{
    [TestFixture]
    public class FlickrTest
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
                photoAddMock.FakeSignatureCall();
                photoAddMock.FakeElementCall(RESOURCE_NS + ".UploadStatus.xml");
                photoAddMock.FakeAuthenticateCall(Permission.Delete, 2);

                byte[] oImage = new byte[photoRes.Length];

                photoRes.Read(oImage, 0, oImage.Length);
                photoRes.Seek(0, SeekOrigin.Begin);
                
                string path = System.AppDomain.CurrentDomain.BaseDirectory + "\\photo.txt" ;

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

                fileStream.Dispose();

                FileStream readStream = File.Open(path, FileMode.Open);

                // read the binary content from file.
                BinaryReader reader = new BinaryReader(readStream);

                byte[] content = new byte[reader.BaseStream.Length];

                content = reader.ReadBytes(content.Length);

                reader.Close();
                
                readStream.Close();
                readStream.Dispose();

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

                mStream.Close();
                // end image verification.

                Assert.IsTrue(photo.Id == "1");
            }

            using (FakeFlickrRepository<PhotoRepository, Photo> photoDeleteMock = new FakeFlickrRepository<PhotoRepository, Photo>())
            {
                photoDeleteMock.FakeAuthenticateCall(Permission.Delete, 1);
                photoDeleteMock.FakeSignatureCall();
                photoDeleteMock.FakeDoHttpPost(RESOURCE_NS + ".DeletePhoto.xml");

                _context.Photos.Remove(photo);
                _context.SubmitChanges();
            }

            MockManager.Verify();
        }

        internal string GetHash(string inputString)
        {
            MD5 md5 = MD5.Create();

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
                photoGetMock.FakeElementCall("Linq.Flickr.Test.Responses.Search.xml");

                var searchQuery = (from p in _context.Photos
                             where p.SearchText == "macbook" && p.FilterMode == FilterMode.Safe 
                             && p.Extras == (ExtrasOption.Views | ExtrasOption.Date_Taken | ExtrasOption.Date_Upload | ExtrasOption.Tags | ExtrasOption.Date_Upload)
                             orderby PhotoOrder.Interestingness ascending
                             select p).Take(100);

                int count = searchQuery.Count();

                Photo first = searchQuery.First();

                Assert.IsTrue(first.SharedProperty.Perpage == count);
                Assert.IsTrue(first.Title == "test" && first.Id == "505611561");
                Assert.IsTrue(first.Views == 8);

                Photo last = searchQuery.Last();

                Assert.IsTrue(last.SharedProperty.Page == 1);
                Assert.IsTrue(last.SharedProperty.Total == 66604);
                Assert.IsTrue(last.Title == "DSCN0355" && last.Id == "2373030074");

                DateTime dateTime = DateTime.Parse("2008-03-29 20:25:05");
                
                Assert.IsTrue(dateTime == last.TakeOn);

                dateTime = GetDate("1206847505");

                Assert.IsTrue(dateTime == last.UploadedOn);

                dateTime = GetDate("1206847506");

                Assert.IsTrue(dateTime == last.UpdatedOn);

                Assert.IsTrue(last.Tags.Length == 2);

                //Assert.IsTrue();

                string constructedWebUrl = string.Format("http://www.flickr.com/photos/{0}/{1}/", last.NsId, last.Id);

                Assert.IsTrue(string.Compare(constructedWebUrl, last.WebUrl, StringComparison.Ordinal) == 0);
            }

            Photo authPhoto = null;

            using (FakeFlickrRepository<PhotoRepository, Photo> authenticatedMock = new FakeFlickrRepository<PhotoRepository, Photo>())
            {
                authenticatedMock.FakeSignatureCall();
                authenticatedMock.FakeCreateAndStoreNewToken(Permission.Delete);
                authenticatedMock.FakeGetNSIDByUsername("neetulee");
                authenticatedMock.FakeElementCall("Linq.Flickr.Test.Responses.Owner.xml");

                var authQuery = from photo in _context.Photos
                                where photo.ViewMode == ViewMode.Owner && photo.User == "neetulee"
                                select photo;

                authPhoto = authQuery.Last();

                Assert.IsTrue(authPhoto.ViewMode == ViewMode.Private);

            }

            
            using (FakeFlickrRepository<PhotoRepository, Photo> authenticatedMockU = new FakeFlickrRepository<PhotoRepository, Photo>())
            {
                const string updatedTitle = "NewTitle";
                authenticatedMockU.FakeAuthenticateCall(Permission.Delete, 1);
                // line verfies if the text is passed properly for update.
                authenticatedMockU.FakeSignatureCall("flickr.photos.setMeta", true, "photo_id", authPhoto.Id, "title", updatedTitle,
                                                     "description", authPhoto.Description ?? " ", "auth_token", "1234");

                authenticatedMockU.FakeDoHttpPostAndReturnStringResult(RESOURCE_NS + ".DeletePhoto.xml");

                authPhoto.Title = updatedTitle;
                // will raise a update call.
                _context.SubmitChanges();
            }

            using (FakeFlickrRepository<PhotoRepository, Photo> getDetailPhoto = new FakeFlickrRepository<PhotoRepository, Photo>())
            {
                getDetailPhoto.FakeCreateAndStoreNewToken(Permission.Delete);
                getDetailPhoto.FakeAuthenticateCall(false, Permission.Delete, 1);
                getDetailPhoto.FakeSignatureCall();
                getDetailPhoto.FakeElementCall("Linq.Flickr.Test.Responses.PhotoDetail.xml");
               
                var photoDetailQuery = from photo in _context.Photos
                                       where photo.Id == "xxx" && photo.PhotoSize == PhotoSize.Medium && photo.ViewMode == ViewMode.Owner
                                       select photo;

                Photo detailPhoto = photoDetailQuery.Single();
                // Element test
                Assert.IsTrue(detailPhoto.Title == "Mug Shot");
                // more that 1
                Assert.IsTrue(detailPhoto.Tags.Length > 1);

                Assert.IsTrue(detailPhoto.UploadedOn == GetDate("1208716675"));
                Assert.IsTrue(detailPhoto.User == "*Park+Ride*");
                Assert.IsTrue(detailPhoto.NsId == "63497523@N00");
                Assert.IsTrue(detailPhoto.WebUrl == "http://www.flickr.com/photos/63497523@N00/2428052817/");
            }

            MockManager.Verify();
        }

        DateTime GetDate(string timeStamp)
        {
            long ticks = 0;
            long.TryParse(timeStamp, out ticks);
            // First make a System.DateTime equivalent to the UNIX Epoch.
            System.DateTime dateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, 0);
            // Add the number of seconds in UNIX timestamp to be converted.
            dateTime = dateTime.AddSeconds(ticks);
            return dateTime;
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
                commentAddMock.FakeAuthenticateCall(Permission.Delete, 1);
                commentAddMock.FakeSignatureCall();
                commentAddMock.FakeDoHttpPostAndReturnStringResult(RESOURCE_NS + ".AddComment.xml");

                comment.PhotoId = COMMENT_PHOTO_ID;
                comment.Text = "Testing comment add [LINQ.Flickr]";

                _context.Photos.Comments.Add(comment);
                _context.SubmitChanges();

                Assert.IsTrue(comment.Id == "1");
            }
            #endregion

            #region Get added comment

            Comment commentGet = null;

            using (FakeFlickrRepository<CommentRepository, Comment> commentGetMock = new FakeFlickrRepository<CommentRepository, Comment>())
            {
                commentGetMock.FakeSignatureCall();
                commentGetMock.MockRESTBuilderGetElement(RESOURCE_NS + ".GetComment.xml");

                var query = from c in _context.Photos.Comments
                            where c.PhotoId == COMMENT_PHOTO_ID && c.Id == comment.Id
                            select c;

                commentGet = query.Single();

                Assert.IsTrue(commentGet.Author == "11" && commentGet.PhotoId == COMMENT_PHOTO_ID && commentGet.AuthorName == "John Doe");
            }
            #endregion


            #region update comment
            using (FakeFlickrRepository<CommentRepository, Comment> commentUpdateMock = new FakeFlickrRepository<CommentRepository, Comment>())
            {
                const string updateText = "#123#";
                commentUpdateMock.FakeAuthenticateCall(Permission.Delete, 1);
                // line verfies if the text is passed properly for update.
                commentUpdateMock.FakeSignatureCall("flickr.photos.comments.editComment", true, "comment_id", "1", "comment_text", updateText, "auth_token", "1234");
                commentUpdateMock.FakeDoHttpPostAndReturnStringResult(RESOURCE_NS + ".UpdateComment.xml");

                commentGet.Text = updateText;

                _context.SubmitChanges();
            }
            #endregion

            #region Delete added

            using (FakeFlickrRepository<CommentRepository, Comment> commentDeleteMock = new FakeFlickrRepository<CommentRepository, Comment>())
            {
                commentDeleteMock.FakeAuthenticateCall(Permission.Delete, 1);
                commentDeleteMock.FakeSignatureCall();
                commentDeleteMock.FakeDoHttpPostAndReturnStringResult(RESOURCE_NS + ".DeleteComment.xml");

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

            Mock restBuilderMock = MockManager.Mock<CollectionBuilder<PopularTag>>(Constructor.NotMocked);
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
            
            Mock restBuilderMock = MockManager.Mock<CollectionBuilder<People>>(Constructor.NotMocked);

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

        private XmlElement MockElement(string resource)
        {
            using (Stream resourceStream = Assembly.GetAssembly(this.GetType()).GetManifestResourceStream(resource))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(XmlReader.Create(resourceStream));
                return doc.DocumentElement;
            }
        }

        void Comments_OnError(ProviderException ex)
        {
            Console.Out.WriteLine(ex.Message);
            Assert.Fail(ex.StackTrace);
        }

        [TearDown]
        public void TeadDown()
        {
            _context = null;
        }

        void Photos_OnError(ProviderException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Assert.Fail(ex.StackTrace);
        }

    }
}
