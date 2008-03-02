﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqExtender.Interface;
using LinqExtender;
using Linq.Flickr.Interface;
using Linq.Flickr.Repository;
using System.Drawing;

namespace Linq.Flickr
{
    public class PhotoQuery : Query<Photo>
    {
        private People _people = null;
        public CommentQuery Comments = new CommentQuery();
  
        protected override void AddItem(Bucket bucket)
        {
            using (IPhoto flickr = new PhotoRepository())
            {
                if (_people == null)
                    _people = flickr.GetUploadStatus();

                object[] args = new object[bucket.Items.Count + 4];

                ViewMode viewMode = bucket.Items[PhotoColumns.VIEWMODE].Value == null ? ViewMode.Public : (ViewMode)bucket.Items[PhotoColumns.VIEWMODE].Value;

                int is_public = viewMode == ViewMode.Public ? 1 : 0;
                int is_friend = viewMode == ViewMode.Friends || viewMode == ViewMode.FriendsFamily ? 1 : 0;
                int is_family = viewMode == ViewMode.Family || viewMode == ViewMode.FriendsFamily ? 1 : 0;
                
                args = new object[] { "is_public", is_public, 
                                    "is_friend", is_friend, 
                                    "is_family", is_family,
                                    bucket.Items[PhotoColumns.TITLE].Name, bucket.Items[PhotoColumns.TITLE].Value,
                                    bucket.Items[PhotoColumns.DESC].Name, bucket.Items[PhotoColumns.DESC].Value };


                string fileName = (string)bucket.Items[PhotoColumns.FILENAME].Value;

                if (string.IsNullOrEmpty(fileName))
                    throw new ApplicationException("Please key in the filename for the photo");
               
                byte[] postContnet = (byte[])bucket.Items[PhotoColumns.POST_CONTENT].Value;

                if (postContnet == null || postContnet.Length == 0)
                    throw new ApplicationException("Zero photo length detected, please key in a valid photo file");

                // check if the user has any storage.
                int kbTobUploaded = (int)Math.Ceiling((float)(postContnet.Length / 1024f));
                if (_people.BandWidth != null)
                {
                    int currentByte = _people.BandWidth.UsedKB + kbTobUploaded ;
                    if (currentByte >= _people.BandWidth.RemainingKB)
                    {
                        throw new ApplicationException("Storage limit excceded, try pro account!!");
                    }
                }

                try
                {
                     string photoId = flickr.Upload(args, fileName, postContnet);
                     // set the id.
                     bucket.Items[PhotoColumns.ID].Value = photoId;
                    
                    // do the math
                     _people.BandWidth.UsedKB += kbTobUploaded;
                     _people.BandWidth.RemainingKB -= kbTobUploaded;
                }
                catch
                {
                    throw new ApplicationException("Upload failed");
                }
            }
        }

        protected override void RemoveItem(Bucket bucket)
        {
            using (IPhoto flickr = new PhotoRepository())
            {
                if (!string.IsNullOrEmpty((string)bucket.Items[PhotoColumns.ID].Value))
                {
                    if (!flickr.Delete((string)bucket.Items[PhotoColumns.ID].Value))
                    {
                        throw new ApplicationException("Photo delete failed");
                    }
                }
                else
                {
                    throw new ApplicationException("Must have valid photo id to perform delete operation");
                }
            }
        }

        private class PhotoColumns
        {
            public const string ID = "Id";
            public const string USER = "User";
            public const string SEARCHTEXT = "SearchText";
            public const string PHOTOSIZE = "PhotoSize";
            public const string VIEWMODE = "ViewMode";
            public const string TITLE = "Title";
            public const string DESC = "Description";
            public const string FILENAME = "FileName";
            public const string POST_CONTENT = "PostContent";

        }

        protected override void Process(IModify<Photo> items, Bucket bucket)
        {
            using (IPhoto flickr = new PhotoRepository())
            {
                // default values
                PhotoSize size = bucket.Items[PhotoColumns.PHOTOSIZE].Value == null ? PhotoSize.Square : (PhotoSize)bucket.Items[PhotoColumns.PHOTOSIZE].Value;
                ViewMode viewMode = bucket.Items[PhotoColumns.VIEWMODE].Value == null ? ViewMode.Public : (ViewMode)bucket.Items[PhotoColumns.VIEWMODE].Value;

                int index = bucket.ItemsToSkip + 1;
                if (index == 0) index = index + 1;

                int itemsToTake = 100;

                if (bucket.ItemsToTake != null)
                {
                    itemsToTake = (int)bucket.ItemsToTake;
                }
               
                bool fetchRecent = false;

                // if there is not tag text, tag or id methioned in search , also want to get my list of images,
                if (string.IsNullOrEmpty((string)bucket.Items[PhotoColumns.ID].Value)
                    && string.IsNullOrEmpty((string)bucket.Items[PhotoColumns.SEARCHTEXT].Value)
                    && viewMode != ViewMode.Owner 
                    && string.IsNullOrEmpty((string)bucket.Items[PhotoColumns.USER].Value))
                {
                    fetchRecent = true;
                }

                if (fetchRecent)
                {
                    items.AddRange(flickr.GetRecent(index, itemsToTake, size));
                    //items.AddRange();
                }
                else
                {
                    bool authenticate = false;
                    string token = string.Empty;
                    // for private or semi-private photo do authenticate.
                    if (viewMode != ViewMode.Public)
                        authenticate = true;
                    if (authenticate)
                        token = flickr.Authenticate(authenticate, Permission.Delete);

                    if (bucket.Items[PhotoColumns.ID].Value != null)
                    {
                        Photo photo = flickr.GetPhotoDetail((string)bucket.Items[PhotoColumns.ID].Value, size);

                        if (photo != null)
                        {
                            items.Add(photo);
                        }
                    }
                    else
                    {
                        string[] args = BuildSearchQuery(bucket, viewMode, false);
                        items.AddRange(flickr.Search(index, itemsToTake, size, args));
                    }
                }
      
            }
        }

        private string[] BuildSearchQuery(Bucket bucket, ViewMode viewMode, bool includeNonVisibleItems)
        {
            string query = string.Empty;
            StringBuilder builder = new StringBuilder();
            string nsId = string.Empty;
            // build the query string
            string[] args = new string[bucket.Items.Count + 4];
           
            int itemIndex = 0;

            foreach (string key in bucket.Items.Keys)
            {
                BucketItem item = bucket.Items[key];

                if (item.Name != PhotoColumns.PHOTOSIZE) // PhotoSize is for internal use.
                {
                    if (item.Value != null && ((item.QueryVisible) || includeNonVisibleItems))
                    {
                        string value = Convert.ToString(item.Value);

                        if (!string.IsNullOrEmpty(value))
                        {
                            using (IPhoto flickr = new PhotoRepository())
                            {
                                if ((item.Name == PhotoColumns.USER))
                                {
                                    args[itemIndex] = "user_id";
                                    if (value.IsValidEmail())
                                    {
                                        nsId = flickr.GetNSIDByEmail(value);
                                    }
                                    else
                                    {
                                        nsId = flickr.GetNSIDByUsername(value);
                                    }
                                    // set the new nslid
                                    if (!string.IsNullOrEmpty(nsId))
                                    {
                                        value = nsId;
                                    }
                                }
                                else
                                {
                                    args[itemIndex] = item.Name;
                                }
                            }
                            args[itemIndex + 1] = value;
                        }
                        itemIndex += 2;
                    } // end if (item.Value != null && ((item.QueryVisible) || includeNonVisibleItems))
                }// end if (item.Name != PhotoColumns.PHOTOSIZE)
            }

            if (bucket.OrderByClause != null)
            {
                args[itemIndex] = "sort";
                args[itemIndex + 1] = GetSortOrder(bucket.OrderByClause.FieldName, bucket.OrderByClause.IsAscending);
                itemIndex += 2;
            }
            // not user id is provided and , owner is specified then get my photos.
            if (viewMode == ViewMode.Owner && string.IsNullOrEmpty((string)bucket.Items[PhotoColumns.USER].Value))
            {
                args[itemIndex] = "user_id";
                args[itemIndex + 1] = "me";
            }

            return args;
        }

        private string GetSortOrder(string orderBy , bool asc)
        {
            string order = orderBy.ToLower().Replace('_', '-');
            // if order by is defined in system , then do the following, or less just return the item.
            if (string.Compare(orderBy, PhotoOrder.Date_Taken.ToString(), true) ==0 
                || string.Compare(orderBy, PhotoOrder.Date_Posted.ToString(), true) ==0)
            {
                if (!asc)
                {
                    order += "-desc";
                }
                else
                {
                    order += "-asc";
                }
            }
            return order;
        }
    }
}