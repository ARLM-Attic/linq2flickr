using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Flickr.Core.Interface;

namespace Flickr.Core
{
    [Serializable]
    public abstract class Photos : IPhotoList<Photo>
    {

        private List<Photo> list = new List<Photo>();

        #region IPhotoList<Photo> Members

        public Photo this[int index]
        {
            get
            {
                return list[index];
            }
            set
            {
                list[index] = value;
            }
        }

        protected List<Photo> Items
        {
            get
            {
                return list;
            }
        }

        public Photo Single()
        {
            return list.Single<Photo>();
        }
        public Photo First()
        {
            return list.First<Photo>();
        }

        public Photo Last()
        {
            return list.Last<Photo>();
        }

        public void Remove(Photo value)
        {
            value.IsDeleted = true;
        }

        public void Clear()
        {
            list.Clear();
        }

        public void Add(Photo item)
        {
            list.Add(item);
        }

        public void AddRange(IEnumerable<Photo> items)
        {
            list.AddRange(items);
        }

        #endregion

        public IEnumerator<Photo> GetPhotoEnumerator()
        {
            return list.GetEnumerator();
        }

    }
}
