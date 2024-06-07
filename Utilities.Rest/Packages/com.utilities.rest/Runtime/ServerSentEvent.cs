// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Scripting;

namespace Utilities.WebRequestRest
{
    [Preserve]
    public sealed class ServerSentEvent
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
        public override string ToString()
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
