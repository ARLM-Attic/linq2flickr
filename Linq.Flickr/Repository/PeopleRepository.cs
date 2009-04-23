﻿using System;
using System.Collections.Generic;
using System.Linq;
using Linq.Flickr.Interface;

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
                nsId = photoRepository.GetNsidByUsername(username);

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
            return  (this as IRepositoryBase).CreateAuthTokeIfNecessary(Permission.Delete.ToString(), false);
        }

        #endregion

        private IEnumerable<People> GetPeople(string requestUrl)
        {
            CollectionBuilder<People> rest = new CollectionBuilder<People>();
            return rest.ToCollection(requestUrl, null);
        }

        #region IDisposable Members

        void IDisposable.Dispose()
        {
        }

        #endregion

    }
}
