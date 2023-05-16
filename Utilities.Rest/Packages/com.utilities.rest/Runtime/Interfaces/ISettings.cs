// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Utilities.WebRequestRest.Interfaces
{
    /// <summary>
    /// Individual client settings per instance.
    /// </summary>
    public interface ISettings
    {
        string BaseRequestUrlFormat { get; }
    }

    /// <inheritdoc />
    public interface ISettings<out TSettingsInfo> : ISettings
        where TSettingsInfo : ISettingsInfo
    {
        TSettingsInfo Info { get; }
    }
}
