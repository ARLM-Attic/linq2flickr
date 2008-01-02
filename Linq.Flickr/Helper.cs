﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Linq.Flickr.Attribute;
using Linq.Flickr.Interface;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace Linq.Flickr
{
    internal static class Helper
    {
        public static class FlickrMethod
        {
            public const string GET_AUTH_TOKEN = "flickr.auth.getToken";
        }

        internal const string BASE_URL = "http://api.flickr.com/services/rest/";
        internal const string AUTH_URL = "http://flickr.com/services/auth/";
        internal const string UPLOAD_URL = "http://api.flickr.com/services/upload/";

        private readonly static IDictionary<string, string> _methodList = new Dictionary<string, string>();
        private static Regex _emailRegex = new Regex(@"^([0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*@([0-9a-zA-Z][-\w]*[0-9a-zA-Z]\.)+[a-zA-Z]{2,9})$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static object _lockHandler = new object();

        internal static bool IsValidEmail(this string inputString)
        {
            return _emailRegex.IsMatch(inputString);
        }

        internal static string GetHash(this string inputString)
        {
            MD5 md5 = MD5CryptoServiceProvider.Create();

            byte[] input = Encoding.UTF8.GetBytes(inputString);
            byte[] output = MD5.Create().ComputeHash(input);

            return BitConverter.ToString(output).Replace("-", "").ToLower();
        }

        internal static void RefreshExternalMethodList(Type interfaceType)
        {
            // not yet initialized.
            if (_methodList.Count == 0)
            {
                MethodInfo[] mInfos = interfaceType.GetMethods();

                foreach (MethodInfo mInfo in mInfos)
                {
                    if (mInfo != null)
                    {
                        object[] customArrtibute = mInfo.GetCustomAttributes(typeof(FlickrMethodAttribute), true);

                        if (customArrtibute != null && customArrtibute.Length == 1)
                        {
                            FlickrMethodAttribute mAtrribute = customArrtibute[0] as FlickrMethodAttribute;

                            string methodFullName = mInfo.ReflectedType.FullName + "." + mInfo.Name;

                            if (!_methodList.ContainsKey(methodFullName))
                            {
                                _methodList.Add(methodFullName, mAtrribute.MethodName);
                            }
                        }
                    }
                }

            }
        }

        internal static string GetExternalMethodName()
        {
            StackTrace trace = new StackTrace(1, true);
            MethodBase methodBase = trace.GetFrames()[0].GetMethod();
            return _methodList[methodBase.Name];
        }
 
    }
}
