// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using UnityEngine.Scripting;
using Utilities.WebRequestRest.Interfaces;

namespace Utilities.WebRequestRest
{
    /// <summary>
    /// A single server-sent event (event type, value, and optional data payload).
    /// </summary>
    /// <remarks>
    /// <see cref="Value"/> and <see cref="Data"/> are parsed lazily on first access from the raw
    /// strings supplied by <see cref="TryParseEvent"/>; the parser does not allocate
    /// <see cref="JToken"/> instances upfront. Because this is a value type, copies of an
    /// instance carry their own lazy-cache state.
    /// </remarks>
    [Preserve]
    public struct ServerSentEvent : IServerSentEvent
    {
        private const char Space = ' ';
        private const char Bom = '\uFEFF';
        private const char NewLine = '\n';
        private const char Return = '\r';
        private const string DoneTag = "[DONE]";
        private const string DoneEvent = "done";
        private const string SseComment = "comment";
        private const string SseEvent = "event";
        private const string SseData = "data";
        private const string SseId = "id";
        private const string SseRetry = "retry";
        private const string StreamEventObjectType = "stream.event";

        [ThreadStatic]
        private static StringBuilder cachedDataBuilder;

        private readonly string rawValue;
        private readonly string rawData;
        private JToken cachedValue;
        private JToken cachedData;
        private bool valueResolved;
        private bool dataResolved;

        [Preserve]
        private ServerSentEvent(ServerSentEventKind @event, string value, string data)
        {
            Object = StreamEventObjectType;
            Event = @event;
            rawValue = value;
            rawData = string.IsNullOrWhiteSpace(data) ? null : data;
            cachedValue = null;
            cachedData = null;
            valueResolved = false;
            dataResolved = false;
        }

        /// <summary>
        /// Kind of server-sent event (comment, event, data, id, retry, unknown).
        /// </summary>
        [Preserve]
        public ServerSentEventKind Event { get; }

        /// <summary>
        /// Parsed value for the event field. Lazily initialized on first access.
        /// </summary>
        [Preserve]
        public JToken Value
        {
            get
            {
                if (!valueResolved)
                {
                    cachedValue = ParseToken(rawValue);
                    valueResolved = true;
                }

                return cachedValue;
            }
        }

        /// <summary>
        /// Parsed data payload, if present. Lazily initialized on first access.
        /// </summary>
        [Preserve]
        public JToken Data
        {
            get
            {
                if (!dataResolved)
                {
                    cachedData = rawData == null ? null : ParseToken(rawData);
                    dataResolved = true;
                }

                return cachedData;
            }
        }

        /// <summary>
        /// Object type identifier (e.g. "stream.event").
        /// </summary>
        [Preserve]
        [JsonIgnore]
        public string Object { get; }

        /// <inheritdoc />
        [Preserve]
        public override string ToString()
            => ToJsonString();

        /// <summary>
        /// Returns a JSON string representation of this event.
        /// </summary>
        [Preserve]
        public string ToJsonString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append($"{{\"{ToFieldName(Event)}\": {Value.ToString(Formatting.None)}");

            if (Data != null)
            {
                stringBuilder.Append($", \"data\": {Data.ToString(Formatting.None)}");
            }

            stringBuilder.Append('}');
            return stringBuilder.ToString();
        }

        [Preserve]
        private static JToken ParseToken(string source)
        {
            if (source == null)
            {
                return null;
            }

            try
            {
                return JToken.Parse(source);
            }
            catch
            {
                return new JValue(source);
            }
        }

        [Preserve]
        private static StringBuilder RentDataBuilder()
        {
            var builder = cachedDataBuilder;
            cachedDataBuilder = null;

            if (builder != null)
            {
                builder.Clear();
                return builder;
            }

            return new StringBuilder();
        }

        [Preserve]
        private static void ReturnDataBuilder(StringBuilder builder)
        {
            if (builder == null)
            {
                return;
            }

            builder.Clear();
            cachedDataBuilder = builder;
        }

        [Preserve]
        private static ServerSentEventKind ParseFieldKind(ReadOnlySpan<char> fieldName)
        {
            if (fieldName.Equals(SseComment, StringComparison.OrdinalIgnoreCase))
            {
                return ServerSentEventKind.Comment;
            }

            if (fieldName.Equals(SseEvent, StringComparison.OrdinalIgnoreCase))
            {
                return ServerSentEventKind.Event;
            }

            if (fieldName.Equals(SseData, StringComparison.OrdinalIgnoreCase))
            {
                return ServerSentEventKind.Data;
            }

            if (fieldName.Equals(SseId, StringComparison.OrdinalIgnoreCase))
            {
                return ServerSentEventKind.Id;
            }

            if (fieldName.Equals(SseRetry, StringComparison.OrdinalIgnoreCase))
            {
                return ServerSentEventKind.Retry;
            }

            return ServerSentEventKind.Unknown;
        }

        [Preserve]
        private static string ToFieldName(ServerSentEventKind kind) => kind switch
        {
            ServerSentEventKind.Comment => SseComment,
            ServerSentEventKind.Event => SseEvent,
            ServerSentEventKind.Data => SseData,
            ServerSentEventKind.Id => SseId,
            ServerSentEventKind.Retry => SseRetry,
            ServerSentEventKind.Unknown => SseComment,
            _ => throw new ArgumentException("Invalid server sent event kind", nameof(kind)),
        };

        [Preserve]
        private static ReadOnlySpan<char> TrimValue(ReadOnlySpan<char> chars)
        {
            while (chars.Length > 0 && chars[0] == Space)
            {
                chars = chars[1..];
            }

            if (chars.Length > 0 && chars[0] == Bom)
            {
                chars = chars[1..];
            }

            return chars;
        }

        [Preserve]
        private static void AppendDataLine(ref StringBuilder builder, ReadOnlySpan<char> chunk)
        {
            const int defaultStringBuilderPadding = 16;
            builder ??= new StringBuilder(chunk.Length + defaultStringBuilderPadding);

            if (builder.Length > 0)
            {
                builder.Append(NewLine);
            }

            if (chunk.Length > 0)
            {
                builder.Append(chunk);
            }
        }

        [Preserve]
        private static bool TryReadLine(string source, int length, ref int position, out ReadOnlySpan<char> line)
        {
            if (position >= length)
            {
                line = default;
                return false;
            }

            var newlineIndex = source.IndexOf(NewLine, position);

            if (newlineIndex < 0 || newlineIndex >= length)
            {
                line = default;
                return false;
            }

            var lineLength = newlineIndex - position;
            line = source.AsSpan(position, lineLength);
            position = newlineIndex + 1;

            if (line.Length > 0 && line[^1] == Return)
            {
                line = line[..^1];
            }

            return true;
        }

        /// <summary>
        /// Parses one server-sent event from <paramref name="source"/> starting at <paramref name="position"/>.
        /// </summary>
        /// <param name="source">Backing buffer containing accumulated SSE text.</param>
        /// <param name="length">Length of valid data in <paramref name="source"/>.</param>
        /// <param name="position">In/out: read cursor; advanced past the parsed event on success.</param>
        /// <param name="event">The parsed event when this method returns <see langword="true"/>; otherwise <c>default</c>.</param>
        /// <param name="isDone">Set to <see langword="true"/> when the parsed event signals end-of-stream (<c>[DONE]</c> or <c>done</c>).</param>
        /// <returns>
        /// <see langword="true"/> if a complete event boundary was reached (regardless of payload);
        /// <see langword="false"/> when more data is needed (caller should resume from the original
        /// <paramref name="position"/> on the next iteration).
        /// </returns>
        /// <remarks>
        /// When the return value is <see langword="true"/>, <paramref name="event"/> may still be
        /// <c>default</c> (and <see cref="Value"/> / <see cref="Data"/> both <see langword="null"/>):
        /// this signals "valid event boundary but empty payload"—the caller should <c>continue</c>
        /// to the next iteration without invoking the handler.
        /// </remarks>
        [Preserve]
        internal static bool TryParseEvent(string source, int length, ref int position, out ServerSentEvent @event, out bool isDone)
        {
            @event = default;
            isDone = false;

            var eventKind = ServerSentEventKind.Comment;
            var dataBuilder = RentDataBuilder();
            var typeAssigned = false;
            ReadOnlySpan<char> value = default;

            while (true)
            {
                if (!TryReadLine(source, length, ref position, out var line))
                {
                    ReturnDataBuilder(dataBuilder);
                    return false;
                }

                if (line.Length == 0)
                {
                    break;
                }

                var colonIndex = line.IndexOf(':');

                if (colonIndex < 0)
                {
                    continue;
                }

                var fieldName = line[..colonIndex].Trim();
                var isCommentLine = colonIndex == 0 && fieldName.Length == 0;
                var fieldValue = TrimValue(line[(colonIndex + 1)..]);
                eventKind = isCommentLine ? ServerSentEventKind.Comment : ParseFieldKind(fieldName);

                if (!typeAssigned)
                {
                    value = fieldValue;
                    typeAssigned = true;

                    if (eventKind == ServerSentEventKind.Data)
                    {
                        AppendDataLine(ref dataBuilder, fieldValue);
                    }

                    continue;
                }

                if (isCommentLine)
                {
                    continue;
                }

                if (eventKind == ServerSentEventKind.Data)
                {
                    AppendDataLine(ref dataBuilder, fieldValue);
                }
            }

            if (!typeAssigned)
            {
                ReturnDataBuilder(dataBuilder);
                return true;
            }

            if (value.Equals(DoneTag, StringComparison.Ordinal) ||
                value.Equals(DoneEvent, StringComparison.Ordinal))
            {
                isDone = true;
                ReturnDataBuilder(dataBuilder);
                return true;
            }

            var data = dataBuilder.ToString();

            if (data.Equals(DoneTag, StringComparison.Ordinal))
            {
                isDone = true;
            }
            else
            {
                @event = new ServerSentEvent(eventKind, value.ToString(), data);
            }

            ReturnDataBuilder(dataBuilder);
            return true;
        }
    }
}
