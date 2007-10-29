using System;
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
            User = "chschulz";
            // do query.
            var query = (from ph in context.Photos
                         where ph.User == User && ph.PhotoSize == PhotoSize.Thumbnail && ph.SearchText == "New york"
                         select ph).Take(5).Skip(0);

            try
            {
  
                foreach (Photo p in query)
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
