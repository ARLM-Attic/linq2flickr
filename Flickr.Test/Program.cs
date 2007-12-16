﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Linq.Flickr;

namespace Flickr.Test
{
    /// <summary>
    /// The purpose of this test program is to show how query photos in flickr using LINQFlickr api.
    /// </summary>
    class Program
    {

        private static string User {get;set;}
        
        static void Main(string[] args)
        {
            // create the context
            FlickrContext context = new FlickrContext();
            // set the user.
            User = "jcl";
            // do query.
            var query = (from ph in context.Photos
                         where ph.PhotoSize == PhotoSize.Medium && ph.SearchText == "iphone" && ph.SearchMode == SearchMode.TagsOnly
                         orderby PhotoOrder.Date_Posted descending
                         select new { ph.Title, ph.Url }).Take(10).Skip(0);

            try
            {
  
                foreach (var p in query)
                {
                    Console.WriteLine(p.Title + "\r\n" + p.Url);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.ReadLine();
        }
    }
}
