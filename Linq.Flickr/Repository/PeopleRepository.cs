using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Linq.Flickr.Interface;
using System.Xml.Linq;

namespace Linq.Flickr.Repository
{
    public class PeopleRepository : BaseRepository, IPeopleRepository
    {
        public PeopleRepository() : base(typeof(IPeopleRepository)) { }

        #region IPeopleRepository Members

        People IPeopleRepository.GetInfo(string userId)
        {
            string method = Helper.GetExternalMethodName();
            string sig = GetSignature(method, true, "user_id", userId);
            string requestUrl = BuildUrl(method, "user_id", userId, "api_sig", sig);
            return GetPeople(requestUrl).Single();
        }

        People IPeopleRepository.GetByUsername(string username)
        {
            string nsId = string.Empty;

            using (IPhotoRepository photoRepository = new PhotoRepository())
            {
                nsId = photoRepository.GetNSIDByUsername(username);

                if (!string.IsNullOrEmpty(nsId))
                {
                    return (this as IPeopleRepository).GetInfo(nsId);
                }
                else
                {
                    throw new Exception("Invalid user Id");
                }
            }
        }

        AuthToken IPeopleRepository.GetAuthenticatedToken()
        {
            string method = Helper.GetExternalMethodName();
            return  (this as IRepositoryBase).GetAuthenticatedToken(Permission.Delete.ToString(), false);
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
