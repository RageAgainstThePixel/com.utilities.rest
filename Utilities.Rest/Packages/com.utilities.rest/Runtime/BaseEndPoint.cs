// Licensed under the MIT License. See LICENSE in the project root for license information.

using Utilities.WebRequestRest.Interfaces;

namespace Utilities.WebRequestRest
{
    /// <summary>
    /// Base endpoint.
    /// </summary>
    /// <typeparam name="TClient"></typeparam>
    /// <typeparam name="TAuthentication"></typeparam>
    /// <typeparam name="TSettings"></typeparam>
    public abstract class BaseEndPoint<TClient, TAuthentication, TSettings>
        where TAuthentication : IAuthentication
        where TSettings : ISettings
        where TClient : BaseClient<TAuthentication, TSettings>
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="client"></param>
        protected BaseEndPoint(TClient client) => this.client = client;

        /// <summary>
        /// <see cref="IClient"/> for this endpoint.
        /// </summary>
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
