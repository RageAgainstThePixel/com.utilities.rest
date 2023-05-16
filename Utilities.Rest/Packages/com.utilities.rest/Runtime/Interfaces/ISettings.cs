// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Utilities.WebRequestRest.Interfaces
{
    /// <summary>
    /// Individual settings per <see cref="IClient"/> instance.
    /// </summary>
    public interface ISettings
    {
        /// <summary>
        /// The base url format to use when making requests.
        /// </summary>
        string BaseRequestUrlFormat { get; }
    }

    /// <inheritdoc />
    public interface ISettings<out TSettingsInfo> : ISettings
        where TSettingsInfo : ISettingsInfo
    {
        /// <summary>
        /// Currently loaded <see cref="ISettingsInfo"/>.
        /// </summary>
        TSettingsInfo Info { get; }
    }
}
