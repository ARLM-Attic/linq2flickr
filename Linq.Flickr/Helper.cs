﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Linq.Flickr.Attribute;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace Linq.Flickr
{
    internal static class Helper
    {
        internal const string BASE_URL = "http://api.flickr.com/services/rest/";
        internal const string AUTH_URL = "http://flickr.com/services/auth/";
        internal const string UPLOAD_URL = "http://api.flickr.com/services/upload/";

        private readonly static IDictionary<string, string> _methodList = new Dictionary<string, string>();
        private readonly static IDictionary<string, string> _interfaceList = new Dictionary<string, string>();
        private static readonly Regex _emailRegex = new Regex(@"^([0-9a-zA-Z]([-.\w]*[0-9a-zA-Z])*@([0-9a-zA-Z][-\w]*[0-9a-zA-Z]\.)+[a-zA-Z]{2,9})$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static object _lockHandler = new object();

        internal static bool IsValidEmail(this string inputString)
        {
            return _emailRegex.IsMatch(inputString);
        }

        internal static string GetHash(this string inputString)
        {
            MD5 md5 = MD5.Create();

            byte[] input = Encoding.UTF8.GetBytes(inputString);
            byte[] output = MD5.Create().ComputeHash(input);

            return BitConverter.ToString(output).Replace("-", "").ToLower();
        }

        internal static DateTime GetDate(this string timeStamp)
        {
            long ticks = 0;
            long.TryParse(timeStamp, out ticks);
            // First make a System.DateTime equivalent to the UNIX Epoch.
            System.DateTime dateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, 0);

            // Add the number of seconds in UNIX timestamp to be converted.
            dateTime = dateTime.AddSeconds(ticks);

            // The dateTime now contains the right date/time so to format the string,
            // use the standard formatting methods of the DateTime object.
            return dateTime;
        }


        internal static string GetExtrasString(this ExtrasOption extras)
        {
            StringBuilder builder = new StringBuilder();

            if ((extras & ExtrasOption.Date_Taken) == ExtrasOption.Date_Taken)
            {
                builder.Append(ExtrasOption.Date_Taken.ToString());
            }
            if ((extras & ExtrasOption.Date_Upload) == ExtrasOption.Date_Upload)
            {
                if (builder.Length > 0) builder.Append(",");
                builder.Append(ExtrasOption.Date_Upload.ToString());
            }
            if ((extras & ExtrasOption.Icon_Server) == ExtrasOption.Icon_Server)
            {
                if (builder.Length > 0) builder.Append(",");
                builder.Append(ExtrasOption.Icon_Server.ToString());
            }
            if ((extras & ExtrasOption.License) == ExtrasOption.License)
            {
                if (builder.Length > 0) builder.Append(",");
                builder.Append(ExtrasOption.License.ToString());
            }
            if ((extras & ExtrasOption.Owner_Name) == ExtrasOption.Owner_Name)
            {
                if (builder.Length > 0) builder.Append(",");
                builder.Append(ExtrasOption.Owner_Name.ToString());
            }
            if ((extras & ExtrasOption.Original_Format) == ExtrasOption.Original_Format)
            {
                if (builder.Length > 0) builder.Append(",");
                builder.Append(ExtrasOption.Owner_Name.ToString());
            }

            if ((extras & ExtrasOption.Last_Update) == ExtrasOption.Last_Update)
            {
                if (builder.Length > 0) builder.Append(",");
                builder.Append(ExtrasOption.Last_Update.ToString());
            }

            if ((extras & ExtrasOption.Tags) == ExtrasOption.Tags)
            {
                if (builder.Length > 0) builder.Append(",");
                builder.Append(ExtrasOption.Tags.ToString());
            }

            if ((extras & ExtrasOption.Geo) == ExtrasOption.Geo)
            {
                if (builder.Length > 0) builder.Append(",");
                builder.Append(ExtrasOption.Geo.ToString());
            }
            if ((extras & ExtrasOption.Views) == ExtrasOption.Views)
            {
                if (builder.Length > 0) builder.Append(",");
                builder.Append(ExtrasOption.Views.ToString());
            }
            return builder.ToString().ToLower();
        }

        public static XElement ValidateResponse(this XElement element)
        {
            if (element.Attribute("stat").Value == "ok")
            {
                return element;
            }
            else
            {
               var error = (from erros in element.Descendants("err")
                          select new
                          {
                              Code = erros.Attribute("code").Value,
                              Message = erros.Attribute("msg").Value
                          }).Single();

                throw new FlickrException(error.Code, error.Message);
            }
        }

        public static XElement FindElement(this XElement element, XName name)
        {
            return element.Element(name);
        }

        internal static void RefreshExternalMethodList(this Type interfaceType)
        {
            // not yet initialized for a particular interface type.
            if (!_interfaceList.ContainsKey(interfaceType.FullName))
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
                _interfaceList.Add(interfaceType.FullName, interfaceType.FullName);
            }//if (!_interfaceList.ContainsKey(interfaceType.FullName))
        }

        internal static string GetExternalMethodName()
        {
            StackTrace trace = new StackTrace(1, true);
            MethodBase methodBase = trace.GetFrames()[0].GetMethod();
            return _methodList[methodBase.Name];
        }
 
    }
}
