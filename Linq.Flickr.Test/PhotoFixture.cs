using NUnit.Framework;
using Linq.Flickr.Repository.Abstraction;
using Linq.Flickr.Repository;
using Linq.Flickr.Proxies;
using Telerik.JustMock;
using System.Xml;
using System.Resources;
using System.Reflection;
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
                .Returns(ReadFromResource("flickr.interestingness.getList"))
                .MustBeCalled();

            IPhotoRepository repository = new PhotoRepository(proxy);

            // act
            repository.GetMostInteresting(0, 10, PhotoSize.Medium);

            Mock.Assert(proxy);
        }


        private XmlElement ReadFromResource(string fullName)
        {
            string @namespace = this.GetType().Namespace;
            
            using (var stream = 
                Assembly.GetExecutingAssembly().GetManifestResourceStream(@namespace + ".Responses." + fullName + ".xml"))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(stream);
                return doc.DocumentElement;
            }
        }

        const string flickrUrl = "http://api.flickr.com/services/rest/?method={0}&api_key={1}";
    }
}
