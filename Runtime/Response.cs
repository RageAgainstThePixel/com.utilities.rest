// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Utilities.WebRequestRest
{
    /// <summary>
    /// Response to a REST Call.
    /// </summary>
    public class Response
    {
        /// <summary>
        /// The original request that prompted the response.
        /// </summary>
        public string Request { get; }

        /// <summary>
        /// Was the REST call successful?
        /// </summary>
        public bool Successful { get; }

        [Obsolete("Use Response.Body")]
        public string ResponseBody => Body;

        /// <summary>
        /// Response body from the resource.
        /// </summary>
        public string Body { get; }

        [Obsolete("Use Response.Data")]
        public byte[] ResponseData => Data;

        /// <summary>
        /// Response data from the resource.
        /// </summary>
        public byte[] Data { get; }

        [Obsolete("Use Response.Code")]
        public long ResponseCode => Code;

        /// <summary>
        /// Response code from the resource.
        /// </summary>
        public long Code { get; }

        /// <summary>
        /// Response headers from the resource.
        /// </summary>
        public IReadOnlyDictionary<string, string> Headers { get; }

        /// <summary>
        /// Error string
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="request">The request that prompted the response.</param>
        /// <param name="successful">Was the REST call successful?</param>
        /// <param name="body">Response body from the resource.</param>
        /// <param name="data">Response data from the resource.</param>
        /// <param name="responseCode">Response code from the resource.</param>
        /// <param name="headers">Response headers from the resource.</param>
        /// <param name="error">Optional, error message from the resource.</param>
        public Response(string request, bool successful, string body, byte[] data, long responseCode, IReadOnlyDictionary<string, string> headers, string error = null)
        {
            Request = request;
            Successful = successful;
            Body = body;
            Data = data;
            Code = responseCode;
            Headers = headers;
            Error = error;
        }

        public override string ToString() => ToString(string.Empty);

        public string ToString(string methodName)
        {
            var debugMessage = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(methodName))
            {
                debugMessage.Append($"{methodName} -> ");
            }

            debugMessage.Append($"<b>[{(int)Code}]</b> <color=\"cyan\">{Request}</color>");

            if (!Successful)
            {
                debugMessage.Append(" <color=\"red\">Failed!</color>");
            }

            debugMessage.Append("\n");

            if (Headers != null)
            {
                debugMessage.AppendLine("[Headers]");

                foreach (var header in Headers)
                {
                    debugMessage.AppendLine($"{header.Key}: {header.Value}");
                }
            }

            if (!string.IsNullOrWhiteSpace(Body))
            {
                debugMessage.AppendLine("[Body]");
                debugMessage.Append(Body);
                debugMessage.Append("\n");
            }

            if (!string.IsNullOrWhiteSpace(Error))
            {
                debugMessage.AppendLine("[Errors]");
                debugMessage.Append(Error);
                debugMessage.Append("\n");
            }

            return debugMessage.ToString();
        }
    }
}
