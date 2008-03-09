using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqExtender.Attribute;
using LinqExtender;

namespace Linq.Flickr
{
    public class Tag : QueryObjectBase
    {
        [LinqVisible(false)]
        public string Title { get; set; }
        [LinqVisible(false)]
        public string Id { get; set; }
    }
}
