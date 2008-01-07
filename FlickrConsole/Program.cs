using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Linq.Flickr;
using System.IO;
using LinqExtender;
using OpenLinqToSql;
using System.Data.SqlServerCe;
using System.Data;
using System.Configuration;
using System.Collections.Specialized;
using System.Collections;
namespace FlickrConsole
{
    /// <summary>
    /// The purpose of this test program is to show how query photos in flickr using LINQFlickr api.
    /// </summary>
    /// 
    class Program
    {
       
        private static PhotoManager _context = new PhotoManager();

        static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                throw new Exception("Too few command line argument provided, FlickrConsole.exe searchText user");
            }

            switch (args[0].ToLower())
            {
                case "search":
                    Search(args);
                    break;
                case "upload":
                    _context.PerformAction(Action.Upload, args);
                    break;
                case "add":
                    _context.PerformAction(Action.Add, args);
                    break;
            }
        }

        private static void Search(string [] args)
        {
          IList<Photo> list =  _context.SearchPhoto(args);

          foreach (Photo ph in list)
          {
              Console.WriteLine(ph.Title + "->" + ph.Url);
          }
        }

    }
}
