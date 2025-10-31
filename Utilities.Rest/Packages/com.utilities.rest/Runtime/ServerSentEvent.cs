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

        [Preserve]
        public ServerSentEventKind Event { get; }

        [Preserve]
        public JToken Value { get; }

        [Preserve]
        public JToken Data { get; }

        [Preserve]
        [JsonIgnore]
        public string Object { get; }

        [Preserve]
        public override string ToString()
            => ToJsonString();

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
