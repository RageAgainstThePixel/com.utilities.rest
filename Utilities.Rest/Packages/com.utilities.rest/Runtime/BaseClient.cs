// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Authentication;
using Utilities.WebRequestRest.Interfaces;

namespace Utilities.WebRequestRest
{
    public abstract class BaseClient<TAuthentication, TSettings> : IClient
        where TAuthentication : IAuthentication
        where TSettings : ISettings
    {
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
            SetupDefaultRequestHeaders();
        }

        /// <summary>
        /// Validate the <see cref="TAuthentication"/> for this client.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException"></exception>
        protected abstract void ValidateAuthentication();

        /// <summary>
        /// Setup the <see cref="DefaultRequestHeaders"/> for this client.
        /// </summary>
        protected abstract void SetupDefaultRequestHeaders();

        /// <summary>
        /// The default request headers for this <see cref="IClient"/>.
        /// </summary>
        public IReadOnlyDictionary<string, string> DefaultRequestHeaders { get; protected set; }

        /// <summary>
        /// Does the client currently have a valid loaded <see cref="TAuthentication"/>?
        /// </summary>
        public abstract bool HasValidAuthentication { get; }

        protected TAuthentication Authentication { get; }

        /// <summary>
        /// The <see cref="TSettings"/> for this <see cref="IClient"/>.
        /// </summary>
        public TSettings Settings { get; }

        /// <inheritdoc />
        public bool EnableDebug { get; set; }
    }
}
