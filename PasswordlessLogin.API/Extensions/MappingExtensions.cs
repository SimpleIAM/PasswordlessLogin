// Copyright (c) Ryan Foster. All rights reserved. 
// Licensed under the Apache License, Version 2.0.

using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace SimpleIAM.PasswordlessLogin.API
{
    public static class MappingExtensions
    {
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

        public static Orchestrators.PatchUserModel ToPatchUserModel(this API.PatchUserInputModel source)
        {
            var target = new Orchestrators.PatchUserModel();
            var properties = new List<KeyValuePair<string, string>>();
            foreach (var item in source.Properties)
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
