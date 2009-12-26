using System;
using System.Collections.Generic;
using System.Xml;
using Linq.Flickr.Interface;
using Linq.Flickr.Authentication;

namespace Linq.Flickr.Repository
{
    public class CommentRepository : BaseRepository, ICommentRepository
    {
        public CommentRepository() : base(typeof(ICommentRepository))
        {
            authRepo = new AuthRepository();
        }

        public CommentRepository(AuthenticationInformation authenticationInformation)
            : base (authenticationInformation, typeof(ICommentRepository))
        {
            authRepo = new AuthRepository(authenticationInformation);
        }

        private IEnumerable<Comment> GetComments(string requestUrl)
        {
            CollectionBuilder<Comment> builder = new CollectionBuilder<Comment>("comments");
            return builder.ToCollection(requestUrl, null);
        }

        IEnumerable<Comment> ICommentRepository.GetComments(string photoId)
        {
            string method = Helper.GetExternalMethodName();
            AuthToken token = authRepo.Authenticate(false, Permission.Delete);

            string authenitcatedToken = string.Empty;

            if (token != null)
            {
                authenitcatedToken = token.Id;
            }
            string sig = GetSignature(method, true, "photo_id", photoId, "auth_token", authenitcatedToken);
            string requestUrl = BuildUrl(method, "photo_id", photoId, "api_sig", sig, "auth_token", authenitcatedToken);
            return GetComments(requestUrl);
        }
       
        string ICommentRepository.AddComment(string photoId, string text)
        {
            string authenitcatedToken =  authRepo.Authenticate(Permission.Delete);

            string method = Helper.GetExternalMethodName();

            string sig = GetSignature(method, true, "photo_id", photoId, "auth_token", authenitcatedToken, "comment_text", text);
            string requestUrl = BuildUrl(method, "photo_id", photoId, "comment_text", text, "auth_token", authenitcatedToken, "api_sig", sig);

            string reposnse = DoHTTPPost(requestUrl);
            // get the photo id.
            XmlElement element = ParseElement(reposnse);
            return element.Element("comment").Attribute("id").Value ?? string.Empty;
        }

        bool ICommentRepository.DeleteComment(string commentId)
        {
            string authenitcatedToken = authRepo.Authenticate(Permission.Delete);
            string method = Helper.GetExternalMethodName();

            string sig = GetSignature(method, true, "comment_id", commentId, "auth_token", authenitcatedToken);
            string requestUrl = BuildUrl(method, "comment_id", commentId, "auth_token", authenitcatedToken, "api_sig", sig);

            try
            {
                string responseFromServer = DoHTTPPost(requestUrl);
                XmlElement element = ParseElement(responseFromServer);
                return true;
            }
            catch
            {
                return false;
            }
        }

        bool ICommentRepository.EditComment(string commentId, string text)
        {
            string method = Helper.GetExternalMethodName();
            string authenitcatedToken = authRepo.Authenticate(Permission.Delete);

            string sig = GetSignature(method, true, "comment_id", commentId, "comment_text", text, "auth_token", authenitcatedToken);
            string requestUrl = BuildUrl(method, "comment_id", commentId, "comment_text", text, "auth_token", authenitcatedToken, "api_sig", sig);

            try
            {
                string responseFromServer = DoHTTPPost(requestUrl);
                XmlElement element = ParseElement(responseFromServer);
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

        private IAuthRepository authRepo;
    }
}
