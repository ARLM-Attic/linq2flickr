using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Linq.Flickr;

namespace FlickrConsole
{
    /// <summary>
    /// The purpose of this test program is to show how query photos in flickr using LINQFlickr api.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // create the context
            FlickrContext context = new FlickrContext();

            if (args.Length == 0)
            {
                throw new Exception("Command line argument is not provided, FlickrConsole.exe searchText user");
            }
            // search only text.
            try
            {
            // do query.
                var query = (from ph in context.Photos
                             where ph.PhotoSize == PhotoSize.Medium && ph.SearchText == args[0] && ph.SearchMode == SearchMode.FreeText
                             && ph.User == (args.Length > 1 ? args[1] : string.Empty)
                             orderby PhotoOrder.Date_Posted descending
                             select new { ph.Title, ph.Url }).Take(10).Skip(0);
              

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
