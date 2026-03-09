// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Utilities.WebRequestRest
{
    /// <summary>
    /// Response to a REST Call.
    /// </summary>
    public sealed class Response : IDisposable
    {
        private static readonly Dictionary<string, string> invalidHeaders = new();

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
        /// Response data as a managed array. Prefer <see cref="NativeData"/> to avoid allocation; dispose this Response when done.
        /// </summary>
        [Obsolete("Use NativeData and dispose Response when done. Data returns NativeData.ToArray() when backed by native data.")]
        public byte[] Data => GetDataBytes();

        /// <summary>
        /// Response data as a native array. Valid until <see cref="Dispose"/>; do not dispose the array yourself.
        /// </summary>
        public NativeArray<byte>? NativeData { get; }

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
        /// Request parameters.
        /// </summary>
        public RestParameters? Parameters { get; }

        /// <summary>
        /// Full list of server sent events.
        /// </summary>
        public IReadOnlyList<ServerSentEvent> ServerSentEvents => Parameters?.ServerSentEvents;

        private byte[] GetDataBytes()
            => NativeData is { IsCreated: true }
                ? NativeData.Value.ToArray()
                : null;

        private int GetDataLength()
            => NativeData is { IsCreated: true }
                ? NativeData.Value.Length
                : 0;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="webRequest">The request that prompted the response.</param>
        /// <param name="requestBody">The request body that prompted the response.</param>
        /// <param name="successful">Was the request successful?</param>
        /// <param name="parameters">The parameters of the request.</param>
        /// <param name="responseBody">Optional, response body override.</param>
        public Response(UnityWebRequest webRequest, string requestBody, bool successful, RestParameters? parameters, string responseBody = null)
        {
            Request = webRequest.url;
            RequestBody = requestBody;
            Method = webRequest.method;
            Successful = successful;

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                byte[] rawData = null;
                switch (webRequest.downloadHandler)
                {
                    case DownloadHandlerFile:
                    case DownloadHandlerTexture:
                    case DownloadHandlerAudioClip:
                    case DownloadHandlerAssetBundle:
                        Body = null;
                        break;
                    case DownloadHandlerBuffer downloadHandlerBuffer:
                        Body = downloadHandlerBuffer.text;
                        rawData = downloadHandlerBuffer.data;
                        break;
                    case DownloadHandlerScript downloadHandlerScript:
                        Body = downloadHandlerScript.text;
                        rawData = downloadHandlerScript.data;
                        break;
                    default:
                        Body = webRequest.responseCode == 401 ? "Invalid Credentials" : webRequest.downloadHandler?.text;
                        rawData = webRequest.downloadHandler?.data;
                        break;
                }

                if (rawData is { Length: > 0 })
                {
                    NativeData = new NativeArray<byte>(rawData.Length, Allocator.Persistent);
                    NativeArray<byte>.Copy(rawData, NativeData.Value, rawData.Length);
                }
            }
            else
            {
                Body = responseBody;
            }

            Code = webRequest.responseCode;
            Headers = webRequest.GetResponseHeaders() ?? invalidHeaders;
            Parameters = parameters;
            Error = !successful ? $"{webRequest.error}\n{webRequest.downloadHandler?.error}" : null;
        }

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
        /// <param name="parameters">The parameters of the request.</param>
        /// <param name="error">Optional, error message from the resource.</param>
        public Response(string request, string method, string requestBody, bool successful, string body, byte[] data, long responseCode, IReadOnlyDictionary<string, string> headers, RestParameters? parameters, string error = null)
        {
            Request = request;
            RequestBody = requestBody;
            Method = method;
            Successful = successful;
            Body = body;
            Code = responseCode;
            Headers = headers;
            Error = error;
            Parameters = parameters;

            if (data is { Length: > 0 })
            {
                NativeData = new NativeArray<byte>(data.Length, Allocator.Persistent);
                NativeArray<byte>.Copy(data, NativeData.Value, data.Length);
            }
        }

        /// <summary>
        /// Constructor that takes ownership of the given native array. Caller must not dispose the array; this Response will dispose it.
        /// </summary>
        /// <param name="request">The request that prompted the response.</param>
        /// <param name="method">The request method.</param>
        /// <param name="requestBody">The request body.</param>
        /// <param name="successful">Was the request successful?</param>
        /// <param name="body">Response body text.</param>
        /// <param name="nativeData">Native buffer; this instance takes ownership and will dispose it.</param>
        /// <param name="responseCode">Response code.</param>
        /// <param name="headers">Response headers.</param>
        /// <param name="parameters">Request parameters.</param>
        /// <param name="error">Optional error message.</param>
        public Response(string request, string method, string requestBody, bool successful, string body, NativeArray<byte> nativeData, long responseCode, IReadOnlyDictionary<string, string> headers, RestParameters? parameters, string error = null)
        {
            Request = request;
            RequestBody = requestBody;
            Method = method;
            Successful = successful;
            Body = body;
            Code = responseCode;
            Headers = headers;
            Error = error;
            Parameters = parameters;
            NativeData = nativeData;
        }

        /// <summary>
        /// Releases the native buffer held by this response. Call when done with the response (e.g. use <c>using var response = ...</c>).
        /// </summary>
        public void Dispose()
        {
            if (NativeData == null) { return; }

            if (NativeData.Value.IsCreated)
            {
                NativeData.Value.Dispose();
            }
        }

        /// <inheritdoc />
        public override string ToString() => ToString(string.Empty);

        /// <summary>
        /// Returns a formatted string representation of the response (e.g. for debug logging).
        /// </summary>
        /// <param name="methodName">Optional method name to prefix the output.</param>
        /// <returns>A formatted debug string.</returns>
        public string ToString(string methodName)
        {
            var debugMessage = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(methodName))
            {
                debugMessage.Append($"{methodName} -> ");
            }

            debugMessage.Append($"<b>[{Method}:{(int)Code}]</b> <color=\"#{ColorUtility.ToHtmlStringRGB(Color.cyan)}\">{Request}</color>");
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

            debugMessageObject["response"] = new Dictionary<string, object>
            {
                ["code"] = Code
            };

            if (Headers != null)
            {
                debugMessageObject["response"]["headers"] = Headers;
            }

            var dataLength = GetDataLength();

            if (dataLength > 0)
            {
                debugMessageObject["response"]["data"] = dataLength;
            }

            if (string.IsNullOrWhiteSpace(Body))
            {
                if (dataLength > 0 &&
                    Headers != null &&
                    Headers.TryGetValue("Content-Type", out var contentType) &&
                    contentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase))
                {
                    var dataBytes = GetDataBytes();
                    var decoded = Encoding.UTF8.GetString(dataBytes);

                    try
                    {
                        debugMessageObject["response"]["body"] = JToken.Parse(decoded);
                    }
                    catch
                    {
                        debugMessageObject["response"]["body"] = decoded;
                    }
                }
            }
            else
            {
                if (Parameters?.ServerSentEvents?.Count > 0)
                {
                    var array = new JArray();

                    foreach (var @event in Parameters.Value.ServerSentEvents)
                    {
                        var eventObject = new JObject
                        {
                            [@event.Event.ToString().ToLower()] = @event.Value
                        };

                        if (@event.Data != null)
                        {
                            eventObject["data"] = @event.Data;
                        }

                        array.Add(eventObject);
                    }

                    debugMessageObject["response"]["body"] = new JObject
                    {
                        ["events"] = array
                    };
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
