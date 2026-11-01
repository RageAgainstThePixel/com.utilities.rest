// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Utilities.WebRequestRest
{
    /// <summary>
    /// Exception thrown when a REST request fails (e.g. <see cref="ResponseExtensions.Validate"/> when the response is unsuccessful).
    /// </summary>
    public sealed class RestException : Exception
    {
        /// <summary>
        /// Creates an exception for the given response, with optional message and inner exception.
        /// </summary>
        /// <param name="response">The response that caused the failure.</param>
        /// <param name="message">Optional message; if null or whitespace, <paramref name="response"/>.<see cref="object.ToString"/> is used.</param>
        /// <param name="innerException">Optional inner exception.</param>
        public RestException(Response response, string message = null, Exception innerException = null)
            : base(string.IsNullOrWhiteSpace(message) ? response.ToString() : message, innerException)
        {
            Response = response;
        }

        /// <summary>The response that caused this exception.</summary>
        public Response Response { get; }
    }
}
