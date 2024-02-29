// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        /// The original request body.
        /// </summary>
        public string RequestBody { get; }

        /// <summary>
        /// The request method that prompted the response.
        /// </summary>
        public string Method { get; }

        /// <summary>
        /// Was the REST call successful?
        /// </summary>
        public bool Successful { get; }

        /// <summary>
        /// Response body from the resource.
        /// </summary>
        public string Body { get; }

        /// <summary>
        /// Response data from the resource.
        /// </summary>
        public byte[] Data { get; }

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
        /// <param name="method">The request method that prompted the response.</param>
        /// <param name="requestBody">The request body that prompted the response.</param>
        /// <param name="successful">Was the REST call successful?</param>
        /// <param name="body">Response body from the resource.</param>
        /// <param name="data">Response data from the resource.</param>
        /// <param name="responseCode">Response code from the resource.</param>
        /// <param name="headers">Response headers from the resource.</param>
        /// <param name="error">Optional, error message from the resource.</param>
        public Response(string request, string method, string requestBody, bool successful, string body, byte[] data, long responseCode, IReadOnlyDictionary<string, string> headers, string error = null)
        {
            Request = request;
            RequestBody = requestBody;
            Method = method;
            Successful = successful;
            Body = body;
            Data = data;
            Code = responseCode;
            Headers = headers;
            Error = error;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="request">The request that prompted the response.</param>
        /// <param name="method">The request method that prompted the response.</param>
        /// <param name="successful">Was the REST call successful?</param>
        /// <param name="body">Response body from the resource.</param>
        /// <param name="data">Response data from the resource.</param>
        /// <param name="responseCode">Response code from the resource.</param>
        /// <param name="headers">Response headers from the resource.</param>
        /// <param name="error">Optional, error message from the resource.</param>
        [Obsolete("Use new .ctr with requestBody")]
        public Response(string request, string method, bool successful, string body, byte[] data, long responseCode, IReadOnlyDictionary<string, string> headers, string error = null)
        {
            Request = request;
            Method = method;
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

            debugMessage.Append($"<b>[{Method}:{(int)Code}]</b> <color=\"cyan\">{Request}</color>");
            debugMessage.Append(!Successful ? " <color=\"red\">Failed!</color>" : " <color=\"green\">Success!</color>");
            debugMessage.Append("\n");

            var debugMessageObject = new Dictionary<string, Dictionary<string, object>>
            {
                ["request"] = new()
                {
                    ["url"] = Request
                }
            };

            if (!string.IsNullOrWhiteSpace(RequestBody))
            {
                try
                {
                    debugMessageObject["request"]["body"] = JToken.Parse(RequestBody);
                }
                catch
                {
                    debugMessageObject["request"]["body"] = RequestBody;
                }
            }

            debugMessageObject["response"] = new()
            {
                ["code"] = Code
            };

            if (Headers != null)
            {
                debugMessageObject["response"]["headers"] = Headers;
            }

            if (Data is { Length: > 0 })
            {
                debugMessageObject["response"]["data"] = Data.Length;
            }

            if (!string.IsNullOrWhiteSpace(Body))
            {
                var parts = Body.Split("data: ");

                if (parts.Length > 0)
                {
                    debugMessageObject["response"]["body"] = new JArray();

                    foreach (var part in parts)
                    {
                        if (string.IsNullOrWhiteSpace(part) || part.Contains("[DONE]\n\n")) { continue; }

                        try
                        {
                            ((JArray)debugMessageObject["response"]["body"]).Add(JToken.Parse(part));
                        }
                        catch
                        {
                            ((JArray)debugMessageObject["response"]["body"]).Add(new JValue(part));
                        }
                    }
                }
                else
                {
                    try
                    {
                        debugMessageObject["response"]["body"] = JToken.Parse(Body);
                    }
                    catch
                    {
                        debugMessageObject["response"]["body"] = Body;
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(Error))
            {
                debugMessageObject["response"]["error"] = Error;
            }

            var jsonSettings = new JsonSerializerSettings
            {
                StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
                Formatting = Formatting.Indented
            };
            debugMessage.Append(JsonConvert.SerializeObject(debugMessageObject, jsonSettings));
            return debugMessage.ToString();
        }
    }
}
