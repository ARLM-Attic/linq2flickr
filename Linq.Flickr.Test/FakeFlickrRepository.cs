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

        private const string signature = "xyz";
        private const string authToken = "1234";
        private const string nsId = "xUser";

        public FakeFlickrRepository()
        {
            _mockRepository = MockManager.Mock<T>(Constructor.NotMocked);
        }

        public void FakeElementCall(string resource)
        {
            _mockRepository.ExpectAndReturn("GetElement", MockElement(resource));
        }
        public void FakeSignatureCall()
        {
            _mockRepository.ExpectAndReturn("GetSignature", signature);
        }

        public void FakeSignatureCall(string method, bool doInclude, params object[] args)
        {
            _mockRepository.ExpectAndReturn("GetSignature", signature).Args(method, doInclude, args);
        }

        public void MockAutheticatedGetElement(string resource, bool validate, Permission permission)
        {
            this.FakeSignatureCall();
            this.FakeAuthenticateCall(validate, permission , 1);
            this.FakeElementCall(resource);
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

        public void FakeAuthenticateCall(Permission permission, int number)
        {
            _mockRepository.ExpectAndReturn("Authenticate", authToken, number).Args(permission.ToString());
        }

        public void FakeAuthenticateCall(bool validate, Permission permission, int number)
        {
            _mockRepository.ExpectAndReturn("Authenticate", authToken, number).Args(permission.ToString(), validate);
        }

        public void FakeAuthenticateCall(int number)
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


        public void FakeAuthenticateCall(Permission permission)
        {
            _mockRepository.ExpectAndReturn("Authenticate", authToken).Args(permission.ToString());
        }

        public void MockRESTBuilderGetElement(string resource)
        {
            _httpCallBase = MockManager.Mock<CollectionBuilder<Item>>(Constructor.NotMocked);
            _httpCallBase.ExpectAndReturn("GetElement", MockElement(resource));    
        }

        public void FakeDoHttpPost(string resource)
        {
            _mockRepository.ExpectAndReturn("DoHTTPPost", MockElement(resource).OwnerDocument.InnerXml);
        }
        public void FakeDoHttpPostAndReturnStringResult(string resource)
        {
            _mockRepository.ExpectAndReturn("DoHTTPPost", MockElement(resource).OwnerDocument.InnerXml);
        }

        public void MockMethodCalll(string method, object ret, int timesToRun,  params object [] args)
        {
            _mockRepository.ExpectAndReturn(method, ret, timesToRun).Args(args);
        }

        public void FakeCreateAndStoreNewToken(Permission permission)
        {
            _mockRepository.ExpectAndReturn("CreateAndStoreNewToken", new AuthToken { Id = "xxx", Perm = "write", UserId = "x@y" }).Args("flickr.auth.getToken", permission.ToString().ToLower());
        }

        public void FakeGetNSIDByUsername(string username)
        {
            _mockRepository.ExpectAndReturn("GetNSID", nsId).Args("flickr.people.findByUsername", "username", username);
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

        #region IDisposable Members

        public void Dispose()
        {
            _mockRepository = null;
            _httpCallBase = null;
        }

        #endregion
    }
}
