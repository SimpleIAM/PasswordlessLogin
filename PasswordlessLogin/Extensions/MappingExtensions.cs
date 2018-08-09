// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace SimpleIAM.PasswordlessLogin
{
    public static class MappingExtensions
    {
        public static Entities.OneTimeCode ToEntity(this Models.OneTimeCode source)
        {
            return new Entities.OneTimeCode()
            {
                SentTo = source.SentTo,
                ExpiresUTC = source.ExpiresUTC,
                FailedAttemptCount = source.FailedAttemptCount,
                ClientNonceHash = source.ClientNonceHash,
                LongCode = source.LongCode,
                ShortCode = source.ShortCode,
                SentCount = source.SentCount,
                RedirectUrl = source.RedirectUrl,
            };
        }

        public static Models.OneTimeCode ToModel(this Entities.OneTimeCode source)
        {
            return new Models.OneTimeCode()
            {
                SentTo = source.SentTo,
                ExpiresUTC = source.ExpiresUTC,
                FailedAttemptCount = source.FailedAttemptCount,
                ClientNonceHash = source.ClientNonceHash,
                LongCode = source.LongCode,
                ShortCode = source.ShortCode,
                SentCount = source.SentCount,
                RedirectUrl = source.RedirectUrl,
            };
        }

        public static Models.PasswordHash ToModel(this Entities.PasswordHash source)
        {
            return new Models.PasswordHash()
            {
                SubjectId = source.SubjectId,
                Hash = source.Hash,
                LastChangedUTC = source.LastChangedUTC,
                FailedAttemptCount = source.FailedAttemptCount,
                TempLockUntilUTC = source.TempLockUntilUTC,
            };
        }

        public static Models.UserClaim ToModel(this Entities.UserClaim source)
        {
            return new Models.UserClaim()
            {
                Type = source.Type,
                Value = source.Value                
            };
        }

        public static Models.User ToModel(this Entities.User source)
        {
            return new Models.User()
            {
                SubjectId = source.SubjectId,
                Email = source.Email,
                Claims = source.Claims?.Select(x => x.ToModel()) ?? new Models.UserClaim[] { }
            };
        }

        public static API.GetUserViewModel ToGetUserViewModel(this Models.User source)
        {
            return new API.GetUserViewModel()
            {
                SubjectId = source.SubjectId,
                Email = source.Email,
                AdditionalProperties = source.Claims?.GroupBy(x => x.Type).ToDictionary(
                    x => x.Key,
                    x => x.Count() == 1 ? (JToken)new JValue(x.FirstOrDefault()?.Value) : (JToken)JArray.FromObject(x.Select(y => y.Value))
                )
            };
        }

        public static Orchestrators.PatchUserModel ToPatchUserModel(this API.PatchUserInputModel source, string subjectId)
        {
            var target = new Orchestrators.PatchUserModel()
            {
                SubjectId = subjectId                
            };

            var properties = new List<KeyValuePair<string, string>>();
            foreach(var item in source.Properties)
            {
                if (item.Value is JArray)
                {
                    var values = item.Value.ToArray();
                    foreach (var value in values)
                    {
                        properties.Add(new KeyValuePair<string, string>(item.Key, NormalizeEmptyString(value.ToString())));
                    }
                }
                else
                {
                    properties.Add(new KeyValuePair<string, string>(item.Key, NormalizeEmptyString(item.Value.ToString())));
                }
            }
            target.Properties = properties.ToLookup(x => x.Key, x => x.Value);
            return target;
        }

        public static Models.AuthorizedDevice ToModel(this Entities.AuthorizedDevice source)
        {
            return new Models.AuthorizedDevice
            {
                RecordId = source.Id,
                Description = source.Description,
                AddedOn = source.AddedOn,
            };
        }

        public static string NormalizeEmptyString(string input)
        {
            if(input == "")
            {
                return null;
            }
            return input;
        }
    }
}
