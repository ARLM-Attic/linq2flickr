using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Linq.Flickr.Interface;
using System.Xml.Linq;

namespace Linq.Flickr.Repository
{
    public class CommentRepository : BaseRepository, IComment
    {
        public CommentRepository() : base(typeof(IComment)){}

        private IEnumerable<Comment> GetComments(string requestUrl)
        {
            RestToCollectionBuilder<Comment> builder = new RestToCollectionBuilder<Comment>("comments");
            return builder.ToCollection(requestUrl);
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
            XElement elemnent = ParseElement(reposnse);
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
                XElement element = ParseElement(responseFromServer);
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
