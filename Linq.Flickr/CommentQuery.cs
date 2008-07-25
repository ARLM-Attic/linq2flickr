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
        protected override bool AddItem(Bucket bucket)
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

            using (ICommentRepository commentRepositoryRepo = new CommentRepository())
            {
                string commentId = commentRepositoryRepo.AddComment(photoId, text);
                // set the id.
                bucket.Items[CommentColumns.ID].Value = commentId;

                return (string.IsNullOrEmpty(commentId) == false);
            }
        }

        protected override bool UpdateItem(Bucket bucket)
        {
            string commentId = (string)bucket.Items[CommentColumns.ID].Value;
            string text = (string)bucket.Items[CommentColumns.TEXT].Value;

            if (string.IsNullOrEmpty(commentId))
                throw new Exception("Invalid comment Id");

            if (string.IsNullOrEmpty(text))
                throw new Exception("Blank comment is not allowed");

            using (ICommentRepository commentRepositoryRepo = new CommentRepository())
            {
                return commentRepositoryRepo.EditComment(commentId, text); 
            }
        }

        protected override bool RemoveItem(Bucket bucket)
        {
            string commentId = (string)bucket.Items[CommentColumns.ID].Value;

            if (string.IsNullOrEmpty(commentId))
            {
                throw new Exception("Must provide a comment_id");
            }
            using (ICommentRepository commentRepositoryRepo = new CommentRepository())
            {
                return commentRepositoryRepo.DeleteComment(commentId);
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
            using (ICommentRepository commentRepositoryRepo = new CommentRepository())
            {
                string photoId = (string) bucket.Items[CommentColumns.PHOTO_ID].Value;
                string commentId = (string)bucket.Items[CommentColumns.ID].Value;

                if (string.IsNullOrEmpty(photoId))
                {
                    throw new Exception("Must have a valid photoId");
                }

                int index = bucket.ItemsToSkip ;
                int itemsToTake = int.MaxValue;

                if (bucket.ItemsToTake != null)
                {
                    itemsToTake = (int)bucket.ItemsToTake;
                }

                // get comments
                IEnumerable<Comment> comments = commentRepositoryRepo.GetComments(photoId);
                // filter 
                if (!string.IsNullOrEmpty(commentId))
                {
                    var query = (from comment in comments
                                where comment.Id == commentId
                                select comment).Skip(index).Take(itemsToTake);
                    comments = query;
                }
                else
                {
                    var query = (from comment in comments
                                 select comment).Skip(index).Take(itemsToTake);
                    comments = query;
                }
                items.AddRange(comments, true);
            }
        }
    }
}
