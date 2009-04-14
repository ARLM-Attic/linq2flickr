using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinqExtender;
using Linq.Flickr.Interface;
using Linq.Flickr.Repository;

namespace Linq.Flickr
{
    public class PeopleQuery : Query<People>
    {
        protected override bool AddItem()
        {
            throw new Exception("Add item not supported for People");
        }

        protected override bool RemoveItem()
        {
            throw new Exception("Remove item not supported for People");
        }

        private static class PeopleColumns
        {
            public const string Id = "Id";
            public const string Username = "Username";
        }

        protected override void Process(LinqExtender.Interface.IModify<People> items)
        {
            using (IPeopleRepository peopleRepositoryRepo = new PeopleRepository())
            {
                string userId = (string)Bucket.Instance.For.Item(PeopleColumns.Id).Value;
                string username = (string)Bucket.Instance.For.Item(PeopleColumns.Username).Value;

                People people = null;

                if (!string.IsNullOrEmpty(userId))
                {
                    people = peopleRepositoryRepo.GetInfo(userId);
                }
                else if (!string.IsNullOrEmpty(username))
                {
                    people = peopleRepositoryRepo.GetByUsername(username);
                }
                else
                {
                    // try to get autheticated person
                    AuthToken token = peopleRepositoryRepo.GetAuthenticatedToken();

                    if (token != null)
                        people = peopleRepositoryRepo.GetInfo(token.UserId);
                    else     
                        throw new Exception("Query must contain a valid user id or name");
                }

                items.Add(people);
            }
        }
    }
}
