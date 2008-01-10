using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Linq.Flickr
{
    public class BandWidth
    {
        public int RemainingKB { get; set; }
        public int UsedKB { get; set; }
    }

    public class People
    {
        public string Id { get; set; }
        public bool IsPro { get; set; }
        public string username { get; set; }

        

        public BandWidth BandWidth = new BandWidth();
    }
}
