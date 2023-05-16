using System.Net.Http;
using System.Text;

namespace Utilities.Rest.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Attempts to get the event data from the string data.
        /// Returns false once the stream is done.
        /// </summary>
        /// <param name="streamData">Raw stream data.</param>
        /// <param name="eventData">Parsed stream data.</param>
        /// <returns>True, if the stream is not done. False if stream is done.</returns>
        public static bool TryGetEventStreamData(this string streamData, out string eventData)
        {
            const string dataTag = "data: ";
            eventData = string.Empty;

            if (streamData.StartsWith(dataTag))
            {
                eventData = streamData[dataTag.Length..].Trim();
            }

            const string doneTag = "[DONE]";
            return eventData != doneTag;
        }

        /// <summary>
        /// Converts a json string to <see cref="StringContent"/>.
        /// </summary>
        /// <param name="json">Json string input.</param>
        /// <returns><see cref="StringContent"/></returns>
        public static StringContent ToJsonStringContent(this string json)
        {
            const string jsonContent = "application/json";
            return new StringContent(json, Encoding.UTF8, jsonContent);
        }
    }
}
