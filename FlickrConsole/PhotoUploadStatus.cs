using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqExtender.Attribute;
using OpenLinqToSql;
using LinqExtender;

namespace FlickrConsole
{
    public class PhotoUploadStatus : QueryObjectBase
    {
        [OriginalFieldName("ID"), Identity, LinqVisible]
        public int Id { get; set; }
        [OriginalFieldName("PhotoPath"), LinqVisible]
        public string Path { get; set; }
        [LinqVisible]
        public bool Synced { get; set; }
        [LinqVisible]
        public string Action { get; set; }

        public override bool IsNew
        {
            get
            {
                return (Id == 0);
            }
        }

    }
}
