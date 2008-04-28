using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Linq.Flickr.Interface;
using System.Xml.Linq;

namespace Linq.Flickr.Repository
{
    public class PeopleRepository : BaseRepository, IPeople
    {
        public PeopleRepository() : base(typeof(IPeople)) { }

        #region IPeople Members

        People IPeople.GetInfo(string userId)
        {
            string method = Helper.GetExternalMethodName();
            string sig = GetSignature(method, true, "user_id", userId);
            string requestUrl = BuildUrl(method, "user_id", userId, "api_sig", sig);
            return GetPeople(requestUrl).Single();
        }

        People IPeople.GetByUsername(string username)
        {
            string nsId = string.Empty;

            using (IPhoto photo = new PhotoRepository())
            {
                nsId = photo.GetNSIDByUsername(username);

                if (!string.IsNullOrEmpty(nsId))
                {
                    return (this as IPeople).GetInfo(nsId);
                }
                else
                {
                    throw new Exception("Invalid user Id");
                }
            }
        }

        #endregion

        private IEnumerable<People> GetPeople(string requestUrl)
        {
            RestToCollectionBuilder<People> rest = new RestToCollectionBuilder<People>();
            return rest.ToCollection(requestUrl);
        }

        #region IDisposable Members

        void IDisposable.Dispose()
        {
        }

        #endregion

    }
}
