// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;
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
        /// ReSharper disable once InconsistentNaming
        protected readonly TClient client;

        /// <summary>
        /// The root endpoint address.
        /// </summary>
        protected abstract string Root { get; }

        /// <summary>
        /// Gets the full formatted url for the API endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint url.</param>
        /// <param name="queryParameters">Optional, parameters to add to the endpoint.</param>
        protected virtual string GetUrl(string endpoint = "", Dictionary<string, string> queryParameters = null)
        {
            var result = string.Format(client.Settings.BaseRequestUrlFormat, $"{Root}{endpoint}");

            if (queryParameters is { Count: not 0 })
            {
                result += $"?{string.Join("&", queryParameters.Select(parameter => $"{UnityWebRequest.EscapeURL(parameter.Key)}={UnityWebRequest.EscapeURL(parameter.Value)}"))}";
            }

            return result;
        }

        private bool enableDebug;

        /// <summary>
        /// Enables or disables the logging of all http responses of header and body information for this endpoint.<br/>
        /// WARNING! Enabling this in your production build, could potentially leak sensitive information!
        /// </summary>
        public bool EnableDebug
        {
            get => enableDebug || client.EnableDebug;
            set => enableDebug = value;
        }
    }
}
