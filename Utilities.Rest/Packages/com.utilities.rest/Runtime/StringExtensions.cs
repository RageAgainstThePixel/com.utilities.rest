// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Security.Cryptography;
using System.Text;

namespace Utilities.WebRequestRest
{
    internal static class StringExtensions
    {
        /// <summary>
        /// Generates a <see cref="Guid"/> based on the string.
        /// </summary>
        /// <param name="string">
        /// The string to generate the <see cref="Guid"/>.
        /// </param>
        /// <returns>
        /// A new <see cref="Guid"/> that represents the string.
        /// </returns>
        public static Guid GenerateGuid(string @string)
        {
            using MD5 md5 = MD5.Create();
            return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(@string)));
        }

        /// <summary>
        /// Generates a string representation of a <see cref="Guid"/> based on the string.
        /// </summary>
        /// <param name="string">
        /// The string to generate the <see cref="Guid"/>.
        /// </param>
        /// <returns>
        /// A new string that represents the <see cref="Guid"/> generated from the string.
        /// </returns>
        public static string GenerateGuidString(string @string)
            => GenerateGuid(@string).ToString();

        /// <summary>
        /// Generates a string representation of a <see cref="Guid"/> based on the <see cref="Uri"/>.
        /// </summary>
        /// <param name="uri">
        /// The <see cref="Uri"/> to generate the <see cref="Guid"/> string from.
        /// </param>
        /// <returns>
        /// A new string that represents the <see cref="Guid"/> generated from the <see cref="Uri"/>.
        /// </returns>
        public static string GenerateGuidString(this Uri uri)
            => GenerateGuidString(uri.ToString());
    }
}
