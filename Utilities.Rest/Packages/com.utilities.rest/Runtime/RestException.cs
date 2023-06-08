using System;

namespace Utilities.WebRequestRest
{
    public class RestException : Exception
    {
        public RestException(long statusCode, string message, Exception innerException = null)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }

        public long StatusCode { get; }
    }
}
