// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Utilities.WebRequestRest
{
    /// <summary>
    /// Kind of server-sent event field.
    /// </summary>
    public enum ServerSentEventKind
    {
        /// <summary>Comment line.</summary>
        Comment,
        /// <summary>Event type.</summary>
        Event,
        /// <summary>Data payload.</summary>
        Data,
        /// <summary>Event id.</summary>
        Id,
        /// <summary>Retry interval.</summary>
        Retry,
    }
}
