using NUnit.Framework;
using Linq.Flickr.Repository.Abstraction;
using Linq.Flickr.Repository;
using Moq;
using System.Xml;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Linq.Flickr.Authentication;

namespace Linq.Flickr.Test
{
    [TestFixture]
    public class PhotoFixture
    {
        [Test]
        public void ShouldBeAbleToReadSettingsFromConfig()
        {
            var proxyMock = new Mock<IFlickrElement>();
            
            var provider = new PhotoRepository(proxyMock.Object).Provider;

            Assert.That(provider.GetCurrentFlickrSettings() != null);
        }

        [Test]
        public void ShouldAssertRequestUrl()
        {
            string targetUrl = string.Format(flickrUrl, "flickr.interestingness.getList", "abc");
            targetUrl += "&page=0&per_page=10";

            var proxyMock = new Mock<IFlickrElement>();

            proxyMock.Setup(x => x.GetResponseElement(targetUrl))
                .Returns((string url) => ReadResource(url)).Verifiable();
                
            IPhotoRepository repository = new PhotoRepository(proxyMock.Object);

            // act
            repository.GetMostInteresting(0, 10, PhotoSize.Medium);

            proxyMock.VerifyAll();
        }

        [Test]
        public void ShouldAssertGetSizesWhenOriginalSizeIsSpecifiedForPhotoGet()
        {
            var proxyMock = new Mock<IFlickrElement>();

            proxyMock.Setup(x => x.GetResponseElement(It.IsAny<string>())).Returns(
                (string url) => ReadResource(url));

            IPhotoRepository repository = new PhotoRepository(proxyMock.Object);

            // act
            var photos = repository.GetMostInteresting(0, 10, PhotoSize.Original);

            Assert.AreEqual(4, photos.Count);

            proxyMock.Verify(x => x.GetResponseElement(It.IsAny<string>()), Times.Exactly(5));
        }

        [Test]
        public void ShouldValidateResponseForExtrasOption()
        {
            var proxyMock = new Mock<IFlickrElement>();

            proxyMock.Setup(x => x.GetResponseElement(It.IsAny<string>()))
                .Returns((string url) => ReadResource(url));

            IPhotoRepository repository = new PhotoRepository(proxyMock.Object);

            ExtrasOption options =(ExtrasOption.Views 
                        | ExtrasOption.Date_Taken 
                        | ExtrasOption.Date_Upload 
                        | ExtrasOption.Tags 
                        | ExtrasOption.Date_Upload);
            

            var photos = repository.Search(1, 10, PhotoSize.Default, "abc", "extras" , options.ToExtrasString());

            Assert.IsNotNull(photos.First().Title);

            Assert.AreNotEqual(photos.First().UploadedOn, InvalidDate);
            Assert.AreNotEqual(photos.First().TakeOn, InvalidDate);
            Assert.AreEqual(photos.First().UpdatedOn, InvalidDate);

            Assert.AreEqual(33, photos.First().Views);
            Assert.AreEqual(2, photos.First().Tags.Length);
            Assert.AreEqual("date_taken,date_upload,tags,views", options.ToExtrasString());
        }


        private XmlElement ReadResource(string url)
        {
            if (!string.IsNullOrEmpty(url))
            {
                string @namespace = this.GetType().Namespace;

                string methodName = url.Split('?')[1].Split('&')[0].Split('=')[1];
                string fileName = @namespace + ".Responses." + methodName + ".xml";

                if (!cache.ContainsKey(fileName))
                {
                    using (var stream =
                        Assembly.GetExecutingAssembly().GetManifestResourceStream(fileName))
                    {
                        cache[fileName] = new StreamReader(stream).ReadToEnd();
                    }
                }

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(cache[fileName]);

                return doc.DocumentElement;
            }

            return null;
        }

        private static IDictionary<string, string> cache = new Dictionary<string, string>();

        private DateTime InvalidDate = new DateTime(1970, 1, 1); 
        const string flickrUrl = "http://api.flickr.com/services/rest/?method={0}&api_key={1}";
    }
}
