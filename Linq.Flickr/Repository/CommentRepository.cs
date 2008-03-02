using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Linq.Flickr.Interface;
using System.Xml.Linq;

namespace Linq.Flickr.Repository
{
    public class CommentRepository : Base, IComment
    {
        public CommentRepository() : base(typeof(IComment)){}

        private IEnumerable<Comment> GetComments(string requestUrl)
        {
            XElement doc = GetElement(requestUrl);

            string photoId = doc.Element("comments").Attribute("photo_id").Value ?? string.Empty;

            var query = from comments in doc.Descendants("comment")
                        select new Comment
                        {
                            PhotoId = photoId,
                            Id = comments.Attribute("id").Value ?? string.Empty,
                            PermaLink = comments.Attribute("permalink").Value ?? string.Empty,
                            PDateCreated = comments.Attribute("datecreate").Value ?? string.Empty,
                            Author =
                            {
                                Id = comments.Attribute("author").Value ?? string.Empty,
                                Name = comments.Attribute("authorname").Value ?? string.Empty,
                            },
                            Text = comments.Value ?? string.Empty
                        };
            return query;
        }

        IEnumerable<Comment> IComment.GetComments(string photoId)
        {
            string method = Helper.GetExternalMethodName();
            string sig = GetSignature(method, true, "photo_id", photoId);
            string requestUrl = BuildUrl(method, "photo_id", photoId, "api_sig", sig);
            return GetComments(requestUrl);
        }
       
        string IComment.AddComment(string photoId, string text)
        {
            string authenitcatedToken =  base.Authenticate(Permission.Delete.ToString());

            string method = Helper.GetExternalMethodName();

            string sig = GetSignature(method, true, "photo_id", photoId, "auth_token", authenitcatedToken, "comment_text", text);
            string requestUrl = BuildUrl(method, "photo_id", photoId, "comment_text", text, "auth_token", authenitcatedToken, "api_sig", sig);

            string reposnse = DoHTTPPost(requestUrl);

            // get the photo id.
            XElement elemnent = XElement.Parse(reposnse);
            return elemnent.Element("comment").Attribute("id").Value ?? string.Empty;
        }

        bool IComment.DeleteComment(string commentId)
        {
            string authenitcatedToken = base.Authenticate(Permission.Delete.ToString());
            string method = Helper.GetExternalMethodName();

            string sig = GetSignature(method, true, "comment_id", commentId, "auth_token", authenitcatedToken);
            string requestUrl = BuildUrl(method, "comment_id", commentId, "auth_token", authenitcatedToken, "api_sig", sig);

            try
            {
                string responseFromServer = DoHTTPPost(requestUrl);
                XElement element = XElement.Parse(responseFromServer, LoadOptions.None);
                ParseElement(element);

                return true;
            }
            catch
            {
                return false;
            }
        }

        void IDisposable.Dispose()
        {

        }

    }
}
