using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Linq.Flickr;
using System.IO;
using System.Reflection;

namespace Linq.Flickr.Test
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class SequenceAttribute : System.Attribute
    {
        private int order;

        public int Order
        {
            get { return order; }
        }

        public SequenceAttribute(int i)
        {
            order = i;
        }
    }
}
