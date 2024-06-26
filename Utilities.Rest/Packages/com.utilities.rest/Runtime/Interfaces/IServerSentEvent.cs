// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Utilities.WebRequestRest.Interfaces
{
    /// <summary>
    /// Common interface for streaming server sent events
    /// </summary>
    public interface IServerSentEvent
    {
        string Object { get; }

        string ToJsonString();
    }
}
