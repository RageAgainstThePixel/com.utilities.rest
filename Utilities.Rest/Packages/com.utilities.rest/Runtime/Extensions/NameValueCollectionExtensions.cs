// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Specialized;

namespace Utilities.Rest.Extensions
{
    public static class NameValueCollectionExtensions
    {
        /// <summary>
        /// Converts a <see cref="NameValueCollection"/> of parameters to a query string.
        /// </summary>
        /// <param name="collection"><see cref="NameValueCollection"/></param>
        /// <returns>a query string.</returns>
        public static string ToQuery(this NameValueCollection collection)
        {
            string Converter(string key) => $"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(collection[key])}";
            return string.Join('&', Array.ConvertAll(collection.AllKeys, Converter));
        }
    }
}
