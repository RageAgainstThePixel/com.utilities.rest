using UnityEngine;

namespace Utilities.WebRequestRest.Interfaces
{
    public interface IAuthentication
    {
    }

    public interface IAuthentication<out TAuthInfo> : IAuthentication
        where TAuthInfo : IAuthInfo
    {
        TAuthInfo Info { get; }
    }

    public abstract class AbstractAuthentication<TAuthentication, TAuthInfo> : IAuthentication<TAuthInfo>
        where TAuthentication : IAuthentication
        where TAuthInfo : IAuthInfo
    {
        /// <summary>
        /// Attempts to load the authentication from a <see cref="ScriptableObject"/> asset that implements <see cref="IConfiguration"/>.
        /// </summary>
        /// <returns>
        /// The loaded <see cref="IAuthentication{T}"/> or <see langword="null"/>.
        /// </returns>
        public abstract TAuthentication LoadFromAsset<T>() where T : ScriptableObject, IConfiguration;

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
        public abstract TAuthentication LoadFromPath(string path);

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

        /// <inheritdoc />
        public abstract TAuthInfo Info { get; }
    }
}
