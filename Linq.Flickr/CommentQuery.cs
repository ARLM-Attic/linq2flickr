using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqExtender;
using Linq.Flickr.Interface;
using Linq.Flickr.Repository;

namespace Linq.Flickr
{
    public class CommentQuery : Query<Comment>
    {
        protected override void AddItem(Bucket bucket)
        {
            string photoId = (string)bucket.Items[CommentColumns.PHOTO_ID].Value;
            string text = (string)bucket.Items[CommentColumns.TEXT].Value;

            if (string.IsNullOrEmpty(photoId))
            {
                throw new Exception("Must have valid photoId");
            }

            if (string.IsNullOrEmpty(text))
            {
                throw new Exception("Must have some text for the comment");
            }

            using (IComment commentRepo = new CommentRepository())
            {
                try
                {
                    string commentId = commentRepo.AddComment(photoId, text);
                    // set the id.
                    bucket.Items[CommentColumns.ID].Value = commentId;
                }
                catch (Exception ex)
                {
                    throw new Exception("Comment add failed", ex);
                }
            }
        }

        protected override void RemoveItem(Bucket bucket)
        {
            string commentId = (string)bucket.Items[CommentColumns.ID].Value;

            if (string.IsNullOrEmpty(commentId))
            {
                throw new Exception("Must provide a comment_id");
            }
            using (IComment commentRepo = new CommentRepository())
            {
                commentRepo.DeleteComment(commentId);
            }
        }

        private class CommentColumns
        {
            public const string ID = "Id";
            public const string PHOTO_ID = "PhotoId";
            public const string TEXT = "Text";
        }

        protected override void Process(LinqExtender.Interface.IModify<Comment> items, Bucket bucket)
        {
            using (IComment commentRepo = new CommentRepository())
            {
                string photoId = (string) bucket.Items[CommentColumns.PHOTO_ID].Value;
                string commentId = (string)bucket.Items[CommentColumns.ID].Value;

                if (string.IsNullOrEmpty(photoId))
                {
                    throw new Exception("Must have a valid photoId");
                }
                // get comments
                IEnumerable<Comment> comments = commentRepo.GetComments(photoId);
                // filter 
                if (!string.IsNullOrEmpty(commentId))
                {
                    var query = from comment in comments
                                where comment.Id == commentId
                                select comment;
                    comments = query;
                }
                items.AddRange(comments);
            }
        }
    }
}
