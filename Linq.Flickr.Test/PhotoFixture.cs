using NUnit.Framework;
using Linq.Flickr.Repository.Abstraction;
using Linq.Flickr.Repository;
using Telerik.JustMock;
using System.Xml;
using System.Reflection;
using System.Collections.Generic;
using System.IO;

namespace Linq.Flickr.Test
{
    [TestFixture]
    public class PhotoFixture
    {
        [Test]
        public void ShouldAssertRequestUrl()
        {
            string targetUrl = string.Format(flickrUrl, "flickr.interestingness.getList", "abc");
            targetUrl += "&page=0&per_page=10";

            var proxy = Mock.Create<IFlickrElement>();

            Mock.Arrange(() => proxy.GetResponseElement(targetUrl))
                .Returns((string url) => ReadResource(url))
                .MustBeCalled();

            IPhotoRepository repository = new PhotoRepository(proxy);

            // act
            repository.GetMostInteresting(0, 10, PhotoSize.Medium);

            Mock.Assert(proxy);
        }

        [Test]
        public void ShouldAssertGetSizesWhenOriginalSizeIsSpecifiedForPhotoGet()
        {
            var proxy = Mock.Create<IFlickrElement>();

            Mock.Arrange(() => proxy.GetResponseElement(Arg.AnyString))
                .Returns((string url) => ReadResource(url));

            IPhotoRepository repository = new PhotoRepository(proxy);

            // act
            var photos = repository.GetMostInteresting(0, 10, PhotoSize.Original);

            Assert.AreEqual(4, photos.Count);

            // according to xml data.
            Mock.Assert(() => proxy.GetResponseElement(Arg.AnyString), Occurs.Exactly(5));
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

        const string flickrUrl = "http://api.flickr.com/services/rest/?method={0}&api_key={1}";
    }
}
