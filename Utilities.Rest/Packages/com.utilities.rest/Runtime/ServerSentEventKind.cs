// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Utilities.WebRequestRest
{
    /// <summary>
    /// Kind of server-sent event field.
    /// </summary>
    public enum ServerSentEventKind
    {
        /// <summary>
        /// Field name was not recognized; producer should skip it.
        /// </summary>
        Unknown = -1,
        /// <summary>
        /// Comment line.
        /// </summary>
        Comment = 0,
        /// <summary>
        /// Event type.
        /// </summary>
        Event = 1,
        /// <summary>
        /// Data payload.
        /// </summary>
        Data = 2,
        /// <summary>
        /// Event id.
        /// </summary>
        Id = 3,
        /// <summary>
        /// Retry interval.
        /// </summary>
        Retry = 4,
    }
}
