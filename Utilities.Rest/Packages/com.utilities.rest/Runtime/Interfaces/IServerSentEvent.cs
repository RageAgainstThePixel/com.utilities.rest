// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Utilities.WebRequestRest.Interfaces
{
    /// <summary>
    /// Common interface for streaming server sent events.
    /// </summary>
    public interface IServerSentEvent
    {
        /// <summary>Object type identifier (e.g. "stream.event").</summary>
        string Object { get; }

        /// <summary>Returns a JSON string representation of this event.</summary>
        string ToJsonString();
    }
}
