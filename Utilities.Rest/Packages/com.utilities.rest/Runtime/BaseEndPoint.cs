using Utilities.WebRequestRest.Interfaces;

namespace Utilities.WebRequestRest
{
    public abstract class BaseEndPoint<TClient, TAuthentication, TSettings>
        where TAuthentication : IAuthentication
        where TSettings : ISettings
        where TClient : BaseClient<TAuthentication, TSettings>
    {
        protected BaseEndPoint(TClient client) => this.client = client;

        protected readonly TClient client;

        /// <summary>
        /// The root endpoint address.
        /// </summary>
        protected abstract string Root { get; }

        /// <summary>
        /// Gets the full formatted url for the API endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint url.</param>
        protected string GetUrl(string endpoint = "")
            => string.Format(client.Settings.BaseRequestUrlFormat, $"{Root}{endpoint}");
    }
}
