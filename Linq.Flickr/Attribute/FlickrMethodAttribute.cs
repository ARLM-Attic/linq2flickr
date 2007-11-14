﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Linq.Flickr.Attribute
{
    [AttributeUsage(AttributeTargets.Method)]
    class FlickrMethodAttribute : System.Attribute
    {
        private string _methodName = string.Empty;

        public FlickrMethodAttribute(string name)
        {
            _methodName = name;
        }

        public string MethodName
        {
            get
            {
                return _methodName;
            }
        }

    }

}
