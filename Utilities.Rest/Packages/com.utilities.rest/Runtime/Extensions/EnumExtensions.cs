// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Concurrent;

namespace Utilities.Rest
{
    /// <summary>
    /// Extension methods for <see cref="Enum"/>.
    /// </summary>
    public static class EnumExtensions
    {
        private static readonly JsonSerializerSettings settings = new()
        {
            Converters = { new StringEnumConverter() }
        };

        private static readonly ConcurrentDictionary<Enum, string> cache = new();

        /// <summary>
        /// Serializes the enum value to its JSON string representation (e.g. enum member name without quotes).
        /// </summary>
        /// <typeparam name="T">Enum type.</typeparam>
        /// <param name="value">The enum value.</param>
        /// <returns>String representation suitable for JSON enum member.</returns>
        /// <remarks>
        /// Results are cached by enum value to avoid repeated <see cref="JsonConvert.SerializeObject(object)"/>
        /// allocations on hot paths.
        /// </remarks>
        public static string ToEnumMemberString<T>(this T value) where T : Enum
            => cache.GetOrAdd(value, static e => JsonConvert.SerializeObject(e, settings).Replace("\"", string.Empty));
    }
}
