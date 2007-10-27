using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Flickr.Core.Attribute
{
    public class UseInExpressionAttribute : System.Attribute
    {
        public bool Supported { get;set;}

        public UseInExpressionAttribute(bool supported)
        {
            Supported = supported;
        }
    }
}
