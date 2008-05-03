using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TypeMock;
using System.Xml.Linq;
using System.IO;
using System.Xml;
using System.Reflection;
using Linq.Flickr.Repository;
using System.Net;
using LinqExtender;

namespace Linq.Flickr.Test
{
    public class FakeFlickrRepository<T,Item> : IDisposable where Item : QueryObjectBase
    {
        private Mock _mockRepository = null;
        private MockObject _webResponseMock = null;
        private Mock _httpRequestMock = null;
        private Mock _httpCallBase = null;

        private string signature = "xyz";
        private string authToken = "1234";
        private string nsId = "xUser";

        public FakeFlickrRepository()
        {
            _mockRepository = MockManager.Mock<T>(Constructor.NotMocked);
        }

        public void MockElementCall(string resource)
        {
            _mockRepository.ExpectAndReturn("GetElement", MockElement(resource));
        }
        public void MockSignatureCall()
        {
            _mockRepository.ExpectAndReturn("GetSignature", signature);
        }

        public void MockAutheticatedGetElement(string resource, bool validate, Permission permission)
        {
            this.MockSignatureCall();
            this.MockAuthenticateCall(validate, permission , 1);
            this.MockElementCall(resource);
        }

        public Mock FakeHttpRequestObject(Stream stream)
        {
            _httpRequestMock = MockManager.Mock(typeof(HttpWebRequest));

            _httpRequestMock.ExpectSet("ContentType");
            _httpRequestMock.ExpectAndReturn("GetRequestStream", stream);

            return _httpRequestMock;
        }

        public void FakeWebResponse_GetResponse()
        {
            _webResponseMock = MockManager.MockObject<WebResponse>();
            _httpRequestMock.ExpectAndReturn("GetResponse", _webResponseMock.Object);
        }

        public void FakeWebResponseObject(string resource)
        {
            _webResponseMock.ExpectAndReturn("GetResponseStream", GetResourceStream(resource));
            _webResponseMock.ExpectCall("Close");
        }

        private Stream GetResourceStream(string name)
        {
            return Assembly.GetAssembly(this.GetType()).GetManifestResourceStream(name);
        }

        public void MockAuthenticateCall(bool validate, Permission permission, int number)
        {
            _mockRepository.ExpectAndReturn("Authenticate", authToken, number).Args(validate, permission.ToString());
        }

        public void MockAuthenticateCall(Permission permission, int number)
        {
            _mockRepository.ExpectAndReturn("Authenticate", authToken, number).Args(permission.ToString());
        }

        public void MockAuthenticateCall(int number)
        {
            _mockRepository.ExpectAndReturn("Authenticate", authToken, number);
        }
        public void MockAuthenticateCallWithDelete()
        {
            _mockRepository.ExpectAndReturn("Authenticate", authToken).Args(Permission.Delete.ToString());
        }

        public void MockGetFrob(int number)
        {
            _mockRepository.ExpectAndReturn("GetFrob", "1", number);
        }


        public void MockAuthenticateCall(Permission permission)
        {
            _mockRepository.ExpectAndReturn("Authenticate", authToken).Args(permission.ToString());
        }

        public void MockRESTBuilderGetElement(string resource)
        {
            _httpCallBase = MockManager.Mock<RestToCollectionBuilder<Item>>(Constructor.NotMocked);
            _httpCallBase.ExpectAndReturn("GetElement", MockElement(resource));    
        }

        public void MockDoHttpPost(string resource)
        {
            _mockRepository.ExpectAndReturn("DoHTTPPost", MockElement(resource).ToString());
        }
        public void MockDoHttpPostAndReturnStringResult(string resource)
        {
            _mockRepository.ExpectAndReturn("DoHTTPPost", MockElement(resource).ToString());
        }

        public void MockCreateDesktopToken(bool validate, Permission permission)
        {
            _mockRepository.ExpectAndReturn("CreateDesktopToken", "xyz").Args("1", validate, permission.ToString().ToLower());
        }

        public void MockGetNSIDByUsername(string username)
        {
            _mockRepository.ExpectAndReturn("GetNSID", nsId).Args("flickr.people.findByUsername", "username", username);
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

        #region IDisposable Members

        public void Dispose()
        {
            _mockRepository = null;
            _httpCallBase = null;
        }

        #endregion
    }
}
