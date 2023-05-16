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
