// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using UnityEngine;

namespace Utilities.WebRequestRest.Interfaces
{
    public abstract class AbstractAuthentication<TAuthentication, TAuthInfo, TConfiguration> : IAuthentication<TAuthInfo>
        where TAuthentication : IAuthentication
        where TAuthInfo : IAuthInfo
        where TConfiguration : ScriptableObject, IConfiguration
    {
        /// <inheritdoc />
        public abstract TAuthInfo Info { get; }

        /// <summary>
        /// Attempts to load the default authentication based on order of precedence.<br/>
        /// 1. ScriptableObject<br/>
        /// 2. Directory<br/>
        /// 3. User Directory<br/>
        /// 4. Environment Variables<br/>
        /// </summary>
        /// <returns>
        /// The loaded <see cref="IAuthentication{T}"/> or <see langword="null"/>.
        /// </returns>
        public virtual TAuthentication LoadDefault()
            => LoadFromAsset() ??
               LoadFromDirectory() ??
               LoadFromDirectory(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)) ??
               LoadFromEnvironment();

        /// <summary>
        /// Attempts to load the default authentication based on order of precedence.<br/>
        /// 1. Environment Variables<br/>
        /// 2. User Directory<br/>
        /// 3. Directory<br/>
        /// 4. ScriptableObject<br/>
        /// </summary>
        /// <returns>
        /// The loaded <see cref="IAuthentication{T}"/> or <see langword="null"/>.
        /// </returns>
        public virtual TAuthentication LoadDefaultsReversed()
            => LoadFromEnvironment() ??
               LoadFromDirectory(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)) ??
               LoadFromDirectory() ??
               LoadFromAsset();

        /// <summary>
        /// Attempts to load the authentication from a <see cref="ScriptableObject"/> asset that implements <see cref="IConfiguration"/>.
        /// </summary>
        /// <returns>
        /// The loaded <see cref="IAuthentication{T}"/> or <see langword="null"/>.
        /// </returns>
        public abstract TAuthentication LoadFromAsset(TConfiguration configuration = null);

        /// <summary>
        /// Attempts to load the authentication from the system environment variables.
        /// </summary>
        /// <returns>
        /// The loaded <see cref="IAuthentication{T}"/> or <see langword="null"/>.
        /// </returns>
        public abstract TAuthentication LoadFromEnvironment();

        /// <summary>
        /// Attempts to load the authentication from a specified configuration file.
        /// </summary>
        /// <param name="path">
        /// The specified path to the configuration file.
        /// </param>
        /// <returns>
        /// The loaded <see cref="IAuthentication{T}"/> or <see langword="null"/>.
        /// </returns>
        public TAuthentication LoadFromPath(string path)
            => LoadFromDirectory(Path.GetDirectoryName(path), Path.GetFileName(path), false);

        /// <summary>
        /// Attempts to load the authentication from the specified directory,
        /// optionally traversing up the directory tree.
        /// </summary>
        /// <param name="directory">
        /// The directory to look in, or <see langword="null"/> for the current directory.
        /// </param>
        /// <param name="filename">
        /// The filename of the config file.
        /// </param>
        /// <param name="searchUp">
        /// Whether to recursively traverse up the directory tree if the <paramref name="filename"/> is not found in the <paramref name="directory"/>.
        /// </param>
        /// <returns>
        /// The loaded <see cref="IAuthentication{T}"/> or <see langword="null"/>.
        /// </returns>
        public abstract TAuthentication LoadFromDirectory(string directory = null, string filename = null, bool searchUp = true);
    }
}
