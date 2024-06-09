// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Scripting;
using Utilities.WebRequestRest.Interfaces;

namespace Utilities.WebRequestRest
{
    [Preserve]
    public sealed class ServerSentEvent : IServerSentEvent
    {
        [Preserve]
        internal static readonly Dictionary<string, ServerSentEventKind> EventMap = new()
        {
            { "comment", ServerSentEventKind.Comment },
            { "event", ServerSentEventKind.Event },
            { "data", ServerSentEventKind.Data },
            { "id", ServerSentEventKind.Id },
            { "retry", ServerSentEventKind.Retry },
        };

        [Preserve]
        internal ServerSentEvent(ServerSentEventKind @event) => Event = @event;

        [Preserve]
        public ServerSentEventKind Event { get; }

        [Preserve]
        public JToken Value { get; internal set; }

        [Preserve]
        public JToken Data { get; internal set; }

        [Preserve]
        [JsonIgnore]
        public string Object => "stream.event";

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
