﻿using System;
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
        protected override void AddItem(Bucket item)
        {
            throw new Exception("Add item not supported for People");
        }

        protected override void RemoveItem(Bucket item)
        {
            throw new Exception("Remove item not supported for People");
        }

        private class PeopleColumns
        {
            public const string ID = "Id";
            public const string USERNAME = "Username";
        }

        protected override void Process(LinqExtender.Interface.IModify<People> items, Bucket bucket)
        {
            using (IPeople peopleRepo = new PeopleRepository())
            {
                string userId = (string)bucket.Items[PeopleColumns.ID].Value;
                string username = (string)bucket.Items[PeopleColumns.USERNAME].Value;

                People people = null;

                if (!string.IsNullOrEmpty(userId))
                {
                    people = peopleRepo.GetInfo(userId);
                }
                else if (!string.IsNullOrEmpty(username))
                {
                    people = peopleRepo.GetByUsername(username);
                }
                else
                {
                    throw new Exception("Must provider a valid user Id or name");
                }

                items.Add(people);
            }
        }
    }
}