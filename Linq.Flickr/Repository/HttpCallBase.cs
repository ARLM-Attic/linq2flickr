using System.Xml;
using Linq.Flickr.Interface;
using System.Net;
using System.IO;

namespace Linq.Flickr.Repository
{
    /// <summary>
    /// Provides interface implementation for Doing HTTP calls.
    /// </summary>
    public class HttpCallBase : IHttpCallBase
    {
        public XmlElement GetElement(string requestUrl)
        {
            XmlReaderSettings readerSettings = new XmlReaderSettings();

            readerSettings.IgnoreWhitespace = true;
            readerSettings.IgnoreProcessingInstructions = true;
            readerSettings.IgnoreComments = true;

            XmlElement element = RestExtension.Load(XmlReader.Create(requestUrl, readerSettings));
            return element.ValidateResponse();
        }

        public XmlElement ParseElement(string response)
        {
            XmlElement element = RestExtension.Parse(response);
            return element.ValidateResponse();
        }

        public string DoHTTPPost(string requestUrl)
        {
            // Create a request using a URL that can receive a post. 
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUrl);
            // Set the Method property of the request to POST.
            request.Method = "POST";

            request.KeepAlive = true;
            //// Set the ContentType property of the WebRequest.
            request.ContentType = "charset=UTF-8";
            request.ContentLength = 0;
            // Get the request stream.
            Stream dataStream;
            // Get the response.
            WebResponse response = request.GetResponse();

            dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseFromServer = reader.ReadToEnd();
            // Clean up the streams.
            reader.Close();
            // validate the response.
            // clean up garbage charecters.
            responseFromServer = responseFromServer.Replace("\r", string.Empty).Replace("\n", string.Empty);
            return responseFromServer;
        }
    }
}
