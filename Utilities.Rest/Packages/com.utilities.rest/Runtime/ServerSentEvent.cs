// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Scripting;
using Utilities.WebRequestRest.Interfaces;

namespace Utilities.WebRequestRest
{
    /// <summary>
    /// A single server-sent event (event type, value, and optional data payload).
    /// </summary>
    [Preserve]
    public readonly struct ServerSentEvent : IServerSentEvent
    {
        [Preserve]
        internal static readonly IReadOnlyDictionary<string, ServerSentEventKind> EventMap = new Dictionary<string, ServerSentEventKind>(StringComparer.OrdinalIgnoreCase)
        {
            { "comment", ServerSentEventKind.Comment },
            { "event", ServerSentEventKind.Event },
            { "data", ServerSentEventKind.Data },
            { "id", ServerSentEventKind.Id },
            { "retry", ServerSentEventKind.Retry },
        };

        [Preserve]
        internal ServerSentEvent(ServerSentEventKind @event, string value, string data)
        {
            Object = "stream.event";
            Event = @event;

            try
            {
                Value = JToken.Parse(value);
            }
            catch
            {
                Value = new JValue(value);
            }

            if (!string.IsNullOrWhiteSpace(data))
            {
                try
                {
                    Data = JToken.Parse(data);
                }
                catch
                {
                    Data = new JValue(data);
                }
            }
            else
            {
                Data = null;
            }
        }

        /// <summary>Kind of server-sent event (comment, event, data, id, retry).</summary>
        [Preserve]
        public ServerSentEventKind Event { get; }

        /// <summary>Parsed value for the event field.</summary>
        [Preserve]
        public JToken Value { get; }

        /// <summary>Parsed data payload, if present.</summary>
        [Preserve]
        public JToken Data { get; }

        /// <summary>Object type identifier (e.g. "stream.event").</summary>
        [Preserve]
        [JsonIgnore]
        public string Object { get; }

        /// <inheritdoc />
        [Preserve]
        public override string ToString()
            => ToJsonString();

        /// <summary>Returns a JSON string representation of this event.</summary>
        [Preserve]
        public string ToJsonString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append($"{{\"{Event.ToString().ToLower()}\": {Value.ToString(Formatting.None)}");

            if (Data != null)
            {
                stringBuilder.Append($", \"data\": {Data.ToString(Formatting.None)}");
            }

            stringBuilder.Append('}');
            return stringBuilder.ToString();
        }
    }
}
