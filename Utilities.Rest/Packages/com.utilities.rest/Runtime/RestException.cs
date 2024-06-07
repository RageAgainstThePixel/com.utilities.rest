// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Utilities.WebRequestRest
{
    public sealed class RestException : Exception
    {
        public RestException(Response response, string message = null, Exception innerException = null)
            : base(string.IsNullOrWhiteSpace(message) ? response.ToString() : message, innerException)
        {
            Response = response;
        }

        public Response Response { get; }
    }
}
