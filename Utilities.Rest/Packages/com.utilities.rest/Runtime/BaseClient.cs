using System;
using System.Net.Http;
using System.Security.Authentication;
using Utilities.WebRequestRest.Interfaces;

namespace Utilities.WebRequestRest
{
    public abstract class BaseClient<TAuthentication, TSettings> : IClient
        where TAuthentication : IAuthentication
        where TSettings : ISettings
    {
        protected BaseClient(TAuthentication authentication, TSettings settings, HttpClient httpClient)
            : this(authentication, settings)
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            // Called from base type initializer
            Client = SetupClient(httpClient);
        }

        protected BaseClient(TAuthentication authentication, TSettings settings)
        {
            Authentication = authentication;

            if (Authentication is null)
            {
                throw new AuthenticationException($"Missing {nameof(Authentication)} for {GetType().Name}");
            }

            Settings = settings;

            if (Settings is null)
            {
                throw new ArgumentNullException($"Missing {nameof(Settings)} for {GetType().Name}");
            }

            // ReSharper disable once VirtualMemberCallInConstructor
            // Called from base type initializer
            ValidateAuthentication();

            // ReSharper disable once VirtualMemberCallInConstructor
            // Called from base type initializer
            Client = SetupClient();
        }

        /// <summary>
        /// Setup the <see cref="HttpClient"/>
        /// </summary>
        /// <param name="httpClient"></param>
        /// <returns><see cref="HttpClient"/></returns>
        protected abstract HttpClient SetupClient(HttpClient httpClient = null);

        /// <summary>
        /// Validate the <see cref="IAuthentication"/> for this client.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException"></exception>
        protected abstract void ValidateAuthentication();

        public abstract bool HasValidAuthentication { get; }

        protected TAuthentication Authentication { get; }

        public TSettings Settings { get; }

        /// <summary>
        /// <see cref="HttpClient"/> to use when making calls to the API.
        /// </summary>
        public HttpClient Client { get; private set; }
    }
}
