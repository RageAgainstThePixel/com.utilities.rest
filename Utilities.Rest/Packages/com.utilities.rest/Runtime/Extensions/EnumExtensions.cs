// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace Utilities.Rest
{
    /// <summary>
    /// Extension methods for <see cref="Enum"/>.
    /// </summary>
    public static class EnumExtensions
    {
        private static readonly JsonSerializerSettings settings = new JsonSerializerSettings
        {
            Converters = { new StringEnumConverter() }
        };

        /// <summary>
        /// Serializes the enum value to its JSON string representation (e.g. enum member name without quotes).
        /// </summary>
        /// <typeparam name="T">Enum type.</typeparam>
        /// <param name="value">The enum value.</param>
        /// <returns>String representation suitable for JSON enum member.</returns>
        public static string ToEnumMemberString<T>(this T value) where T : Enum
        {
            const string empty = "";
            const string quote = "\"";
            return JsonConvert.SerializeObject(value, settings).Replace(quote, empty);
        }
    }
}
