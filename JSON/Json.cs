﻿using System;
using System.Text.Json.Serialization;
using System.Text.Json;
using ExtraObjectiveSetup.Utils;
using GTFO.API.JSON.Converters;

namespace ExtraObjectiveSetup.JSON
{
    internal static class Json
    {
        private static readonly JsonSerializerOptions _setting = new()
        {
            ReadCommentHandling = JsonCommentHandling.Skip,
            IncludeFields = false,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            IgnoreReadOnlyProperties = true,

        };


        static Json()
        {
            _setting.Converters.Add(new JsonStringEnumConverter());
            if(MTFOPartialDataUtil.IsLoaded)
            {
                _setting.Converters.Add(MTFOPartialDataUtil.PersistentIDConverter);
                _setting.Converters.Add(MTFOPartialDataUtil.LocalizedTextConverter);
                EOSLogger.Log("PartialData support found!");
            }

            else
            {
                _setting.Converters.Add(new LocalizedTextConverter());
            }

            if(AWOUtil.IsLoaded)
            {
                _setting.Converters.Add(AWOUtil.AWOEventDataConverter);
                EOSLogger.Log("AWO support found!");
            }
        }

        public static T Deserialize<T>(string json)
        {
            return JsonSerializer.Deserialize<T>(json, _setting);
        }

        public static object Deserialize(Type type, string json)
        {
            return JsonSerializer.Deserialize(json, type, _setting);
        }

        public static string Serialize<T>(T value)
        {
            return JsonSerializer.Serialize(value, _setting);
        }
    }
}
