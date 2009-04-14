using System;
using LinqExtender.Interface;
using TypeMock;
using System.IO;
using System.Xml;
using System.Reflection;
using Linq.Flickr.Repository;
using System.Net;

namespace Linq.Flickr.Test
{
    public class FakeFlickrRepository<T,TItem> : IDisposable where TItem : IQueryObject
    {
        private readonly Mock mockRepository;
        private MockObject webResponseMock;
        private Mock httpRequestMock;
        private Mock httpCallBase;

        private const string signature = "xyz";
        private const string authToken = "1234";
        private const string nsId = "xUser";

        public FakeFlickrRepository()
        {
            mockRepository = MockManager.Mock<T>(Constructor.NotMocked);
        }

        public void FakeElementCall(string resource)
        {
            mockRepository.ExpectAndReturn("GetElement", MockElement(resource));
        }
        public void FakeSignatureCall()
        {
            mockRepository.ExpectAndReturn("GetSignature", signature);
        }

        public void FakeSignatureCall(string method, bool doInclude, params object[] args)
        {
            mockRepository.ExpectAndReturn("GetSignature", signature).Args(method, doInclude, args);
        }

        public void MockAutheticatedGetElement(string resource, bool validate, Permission permission)
        {
            this.FakeSignatureCall();
            this.FakeAuthenticateCall(validate, permission , 1);
            this.FakeElementCall(resource);
        }

        public Mock FakeHttpRequestObject(Stream stream)
        {
            httpRequestMock = MockManager.Mock(typeof(HttpWebRequest));

            httpRequestMock.ExpectSet("ContentType");
            httpRequestMock.ExpectAndReturn("GetRequestStream", stream);

            return httpRequestMock;
        }

        public void FakeWebResponseGetResponse()
        {
            webResponseMock = MockManager.MockObject<WebResponse>();
            httpRequestMock.ExpectAndReturn("GetResponse", webResponseMock.Object);
        }

        public void FakeWebResponseObject(string resource)
        {
            webResponseMock.ExpectAndReturn("GetResponseStream", GetResourceStream(resource));
            webResponseMock.ExpectCall("Close");
        }

        private Stream GetResourceStream(string name)
        {
            return Assembly.GetAssembly(this.GetType()).GetManifestResourceStream(name);
        }

        public void FakeAuthenticateCall(Permission permission, int number)
        {
            mockRepository.ExpectAndReturn("Authenticate", authToken, number).Args(permission.ToString());
        }

        public void FakeAuthenticateCall(bool validate, Permission permission, int number)
        {
            mockRepository.ExpectAndReturn("Authenticate", authToken, number).Args(permission.ToString(), validate);
        }

        public void FakeAuthenticateCall(int number)
        {
            mockRepository.ExpectAndReturn("Authenticate", authToken, number);
        }
        public void MockAuthenticateCallWithDelete()
        {
            mockRepository.ExpectAndReturn("Authenticate", authToken).Args(Permission.Delete.ToString());
        }

        public void MockGetFrob(int number)
        {
            mockRepository.ExpectAndReturn("GetFrob", "1", number);
        }


        public void FakeAuthenticateCall(Permission permission)
        {
            mockRepository.ExpectAndReturn("Authenticate", authToken).Args(permission.ToString());
        }

        public void MockRESTBuilderGetElement(string resource)
        {
            httpCallBase = MockManager.Mock<CollectionBuilder<TItem>>(Constructor.NotMocked);
            httpCallBase.ExpectAndReturn("GetElement", MockElement(resource));    
        }

        public void FakeDoHttpPost(string resource)
        {
            mockRepository.ExpectAndReturn("DoHTTPPost", MockElement(resource).OwnerDocument.InnerXml);
        }
        public void FakeDoHttpPostAndReturnStringResult(string resource)
        {
            mockRepository.ExpectAndReturn("DoHTTPPost", MockElement(resource).OwnerDocument.InnerXml);
        }

        public void MockMethodCalll(string method, object ret, int timesToRun,  params object [] args)
        {
            mockRepository.ExpectAndReturn(method, ret, timesToRun).Args(args);
        }

        public void FakeCreateAndStoreNewToken(Permission permission)
        {
            mockRepository.ExpectAndReturn("CreateAndStoreNewToken", new AuthToken { Id = "xxx", Perm = "write", UserId = "x@y" }).Args("flickr.auth.getToken", permission.ToString().ToLower());
        }

        public void FakeGetNsidByUsername(string username)
        {
            mockRepository.ExpectAndReturn("GetNSID", nsId).Args("flickr.people.findByUsername", "username", username);
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

        public void Dispose()
        {
            mockRepository.Verify();
            MockManager.ClearAll();
        }
    }
}
