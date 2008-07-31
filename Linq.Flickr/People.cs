using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqExtender;
using System.Xml;
using LinqExtender.Attribute;
using Linq.Flickr.Attribute;

namespace Linq.Flickr
{
    public class BandWidth
    {
        public int RemainingKB { get; set; }
        public int UsedKB { get; set; }
    }
   
    [XElement("person")]
    public class People : QueryObjectBase
    {
        [LinqVisible, XAttribute("nsid"), UniqueIdentifier]
        public string Id { get; set; }
        [XAttribute("ispro")]
        public bool IsPro { get; set; }
        [XElement("username"), LinqVisible]
        public string Username { get; set; }
        [XElement("realname")]
        public string RealName { get; set; }
        [XElement("location")]
        public string Location { get; internal set; }
        [XElement("photosurl")]
        public string PhotoUrl  { get; internal set; }
        [XElement("profileurl")]
        public string ProfileUrl { get;internal set; }
        [XAttribute("iconserver")]
        internal string IconServer { get; set; }
        [XAttribute("iconfarm")]
        internal string IconFarm { get; set; }

        private string _iconUrl = "http://farm{0}.static.flickr.com/{1}/buddyicons/{2}.jpg";
        private string _defaultIconUrl = "http://www.flickr.com/images/buddyicon.jpg";

       /// <summary>
       /// Returns a buddy icon of 48x48, if there is any.
       /// </summary>
        public string IconUrl
        {
            get
            {
                int iconServer = 0;
                int.TryParse(IconServer, out iconServer);

                if (iconServer > 0)
                {
                    return string.Format(_iconUrl, IconFarm, IconServer, Id);
                }
                else
                {
                    return _defaultIconUrl;
                }
            }
        }

        public BandWidth BandWidth = new BandWidth();
    }
}
