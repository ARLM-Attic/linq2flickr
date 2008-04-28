using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Linq.Flickr.Interface;
using System.Xml.Linq;
using System.Net;
using System.IO;

namespace Linq.Flickr.Repository
{
    public class HttpCallBase : IHttpCall
    {
        public XElement GetElement(string requestUrl)
        {
            XElement element = XElement.Load(requestUrl);
            return element.ValidateResponse();
        }

        public XElement ParseElement(string response)
        {
            XElement element = XElement.Parse(response);
            return element.ValidateResponse();
        }

        public string DoHTTPPost(string requestUrl)
        {
            // Create a request using a URL that can receive a post. 
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(requestUrl);
            // Set the Method property of the request to POST.
            request.Method = "POST";

            request.KeepAlive = true;
            //// Set the ContentType property of the WebRequest.
            request.ContentType = "charset=UTF-8";
            request.ContentLength = 0;
            // Get the request stream.
            Stream dataStream = request.GetRequestStream();
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
