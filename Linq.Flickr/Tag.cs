using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqExtender.Attribute;
using LinqExtender;
using LinqExtender.Interface;

namespace Linq.Flickr
{
    public class Tag : IQueryObject
    {
        [Ignore]
        public string Title { get; set; }
        [Ignore]
        public string Id { get; set; }
    }
}
