using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Linq.Flickr.Interface
{
    public interface IPhotoList<T>
    {
        T this[int index] { get; set; }
        void Remove(T value);
        void Add(T item);
        void AddRange(IEnumerable<T> items);
        IEnumerator<T> GetPhotoEnumerator();
        void Clear();
        T Single();
        T First();
        T Last();
    }
}
