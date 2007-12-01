using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqExtender.Interface;
using LinqExtender;
using Linq.Flickr.Interface;

namespace Linq.Flickr
{
    public class FlickrPhotoQuery : Query<Photo>
    {
  
        protected override void AddItem(Photo item)
        {
            using (IFlickr flickr = new DataAccess())
            {
                flickr.Upload(item);
            }
        }

        protected override void RemoveItem(Photo item)
        {
            using (IFlickr flickr = new DataAccess())
            {
                flickr.Delete(item.Id);
            }
        }

        protected override void Process(IQuery<Photo> items, Photo bucket, int itemsToTake, int itemsToSkip, bool isParamCall)
        {
            using (IFlickr flickr = new DataAccess())
            {
                if (!string.IsNullOrEmpty(bucket.Id))
                {
                    Photo photo = flickr.GetPhotoDetail(bucket.Id, bucket.PhotoSize);

                    if (photo != null)
                    {
                        items.Add(photo);
                    }

                }
                else
                {
                    int index = itemsToSkip + 1;
                    if (index == 0) index = index + 1;

                    bool authenticate = false;
                    string token = string.Empty;
                    // for private or semi-private photo do authenticate.
                    if (bucket.ViewMode != ViewMode.Public)
                    {
                        authenticate = true;
                    }

                    if (authenticate)
                    {
                        token = flickr.Authenticate(authenticate);
                    }

                    bool getRecent = isParamCall ? true : false;

                    // addition to parameterless search, if there is no token and searchtext , get recent photos.
                    if (string.IsNullOrEmpty(token) && string.IsNullOrEmpty(bucket.SearchText) && string.IsNullOrEmpty(bucket.Tags))
                    {
                        getRecent = true;
                    }

                    // if authenticated call, without params , then get my photos.
                    if (!string.IsNullOrEmpty(token) && getRecent)
                    {
                        bucket.ViewMode = ViewMode.Owner;
                    }

                    if (!string.IsNullOrEmpty(token) || (!getRecent))
                    {
                        if (bucket.SearchMode == SearchMode.TagsOnly)
                        {
                            // process tags
                            bucket.Tags = bucket.SearchText;
                            items.AddRange(flickr.Search(bucket.User, string.Empty, bucket.Tags, TagMode.OR, bucket.PhotoSize, bucket.ViewMode, bucket.SortOrder, index, itemsToTake));
                        }
                        else
                        {
                            items.AddRange(flickr.Search(bucket.User, bucket.SearchText, bucket.PhotoSize, bucket.ViewMode, bucket.SortOrder, index, itemsToTake));
                        }
                    }
                    else
                    {

                        items.AddRange(flickr.GetRecent(index, itemsToTake, bucket.PhotoSize));
                    }
                }
            }
        }
    }
}
