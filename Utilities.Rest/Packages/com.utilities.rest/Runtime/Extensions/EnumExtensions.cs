// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Utilities.Rest
{
    public static class EnumExtensions
    {
        private static readonly JsonSerializerSettings settings = new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter() }
        };

        public static string ToEnumMemberString<T>(this T value) where T : Enum
        {
            const string empty = "";
            const string quote = "\"";
            return JsonConvert.SerializeObject(value, settings).Replace(quote, empty);
        }
    }
}
