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
        FlickrContext context;
        private const string ResourceNs = "Linq.Flickr.Test.Responses";
       
        [SetUp]
        public void Setup()
        {
            context = new FlickrContext();
            context.Photos.OnError += Photos_OnError;
            context.Photos.Comments.OnError += Comments_OnError;
        }

        [Test]
        public void DoPhotoUploadAndDeleteTest()
        {
            MockManager.Init();

            Stream photoRes = GetResourceStream("Linq.Flickr.Test.blank.gif");
            Photo photo = new Photo { Title = "Flickr logo", FileName = "Test.Mock", File = photoRes, ViewMode = ViewMode.Public };

            using (var photoAddMock = new FakeFlickrRepository<PhotoRepository, Photo>())
            {
                photoAddMock.FakeSignatureCall();
                photoAddMock.FakeElementCall(ResourceNs + ".UploadStatus.xml");
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
                photoAddMock.FakeWebResponseGetResponse();
                photoAddMock.FakeWebResponseObject(ResourceNs + ".Photo.xml");
                
                // add to the collection.
                context.Photos.Add(photo);
                context.Photos.SubmitChanges();

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

            using (var photoDeleteMock = new FakeFlickrRepository<PhotoRepository, Photo>())
            {
                photoDeleteMock.FakeAuthenticateCall(Permission.Delete, 1);
                photoDeleteMock.FakeSignatureCall();
                photoDeleteMock.FakeDoHttpPost(ResourceNs + ".DeletePhoto.xml");

                context.Photos.Remove(photo);
                context.SubmitChanges();
            }
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

            using (var photoGetMock = new FakeFlickrRepository<PhotoRepository, Photo>())
            {
                photoGetMock.FakeElementCall("Linq.Flickr.Test.Responses.Search.xml");

                var searchQuery = (from p in context.Photos
                             where p.SearchText == "macbook" && p.FilterMode == FilterMode.Safe 
                             && p.Extras == (ExtrasOption.Views | ExtrasOption.Date_Taken | ExtrasOption.Date_Upload | ExtrasOption.Tags | ExtrasOption.Date_Upload)
                             orderby PhotoOrder.Interestingness ascending
                             select p).Take(100);

                int count = searchQuery.Count();

                Photo first = searchQuery.First();

                Assert.IsTrue(first.SharedProperty.Perpage == count);
                Assert.IsTrue(first.Title == "test" && first.Id == "505611561");
                Assert.IsTrue(first.ExtrasResult.Views == 8);

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

            using (var authenticatedMock = new FakeFlickrRepository<PhotoRepository, Photo>())
            {
                authenticatedMock.FakeSignatureCall();
                authenticatedMock.FakeCreateAndStoreNewToken(Permission.Delete);
                authenticatedMock.FakeGetNsidByUsername("neetulee");
                authenticatedMock.FakeElementCall("Linq.Flickr.Test.Responses.Owner.xml");

                var authQuery = from photo in context.Photos
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

                authenticatedMockU.FakeDoHttpPostAndReturnStringResult(ResourceNs + ".DeletePhoto.xml");

                authPhoto.Title = updatedTitle;
                // will raise a update call.
                context.SubmitChanges();
            }
        }

        [Test]
        public void TestQueryDetail()
        {
            using (var repository = new FakeFlickrRepository<PhotoRepository, Photo>())
            {
                repository.FakeCreateAndStoreNewToken(Permission.Delete);
                repository.FakeAuthenticateCall(false, Permission.Delete, 1);
                repository.FakeSignatureCall();
                repository.FakeElementCall("Linq.Flickr.Test.Responses.PhotoDetail.xml");
               
                var photoDetailQuery = from photo in context.Photos
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
            using (var commentAddMock = new FakeFlickrRepository<CommentRepository, Comment>())
            {
                commentAddMock.FakeAuthenticateCall(Permission.Delete, 1);
                commentAddMock.FakeSignatureCall();
                commentAddMock.FakeDoHttpPostAndReturnStringResult(ResourceNs + ".AddComment.xml");

                comment.PhotoId = COMMENT_PHOTO_ID;
                comment.Text = "Testing comment add [LINQ.Flickr]";

                context.Photos.Comments.Add(comment);
                context.SubmitChanges();

                Assert.IsTrue(comment.Id == "1");
            }
            #endregion

            #region Get added comment

            Comment commentGet = null;

            using (var commentGetMock = new FakeFlickrRepository<CommentRepository, Comment>())
            {
                commentGetMock.FakeSignatureCall();
                commentGetMock.MockRESTBuilderGetElement(ResourceNs + ".GetComment.xml");

                var query = from c in context.Photos.Comments
                            where c.PhotoId == COMMENT_PHOTO_ID && c.Id == comment.Id
                            select c;

                commentGet = query.Single();

                Assert.IsTrue(commentGet.Author == "11" && commentGet.PhotoId == COMMENT_PHOTO_ID && commentGet.AuthorName == "John Doe");
            }
            #endregion


            #region update comment
            using (var commentUpdateMock = new FakeFlickrRepository<CommentRepository, Comment>())
            {
                const string updatedText = "#123#";
                commentUpdateMock.FakeAuthenticateCall(Permission.Delete, 1);
                // line verfies if the text is passed properly for update.
                commentUpdateMock.FakeSignatureCall("flickr.photos.comments.editComment", true, "comment_id", "1", "comment_text", updatedText, "auth_token", "1234");
                commentUpdateMock.FakeDoHttpPostAndReturnStringResult(ResourceNs + ".UpdateComment.xml");

                commentGet.Text = updatedText;

                context.SubmitChanges();
            }
            #endregion

            #region Delete added

            using (var commentDeleteMock = new FakeFlickrRepository<CommentRepository, Comment>())
            {
                commentDeleteMock.FakeAuthenticateCall(Permission.Delete, 1);
                commentDeleteMock.FakeSignatureCall();
                commentDeleteMock.FakeDoHttpPostAndReturnStringResult(ResourceNs + ".DeleteComment.xml");

                context.Photos.Comments.Remove(commentGet);
                context.SubmitChanges();
            }
            #endregion
        }

        [Test]
        public void DoPopularTagTest()
        {
            MockManager.Init();

            Mock restBuilderMock = MockManager.Mock<CollectionBuilder<PopularTag>>(Constructor.NotMocked);
            // set the expectation.
            restBuilderMock.ExpectAndReturn("GetElement", MockElement(ResourceNs + ".HotTagGetList.xml"));

            var query = from tag in context.PopularTags
                        where tag.Period == TagPeriod.Day && tag.Count == 6
                        orderby tag.Title ascending
                        select tag;

            int count = query.Count();
            // see if the expected and returned value are same.
            Assert.IsTrue(count == 6);

            PopularTag firstTag = query.First();

            Assert.IsTrue(firstTag.Score == 4);

            PopularTag lastTag = query.Last();

            Assert.IsTrue(lastTag.Score == 10);

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
            restBuilderMock.ExpectAndReturn("GetElement", MockElement(ResourceNs + ".PeopleInfo.xml"));

            var query = from p in context.Peoples
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
            context = null;
        }

        void Photos_OnError(ProviderException ex)
        {
            Console.Error.WriteLine(ex.Message);
            Assert.Fail(ex.StackTrace);
        }

    }
}
