// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Utilities.WebRequestRest.Interfaces
{
    /// <summary>
    /// Authentication required by the <see cref="IClient"/>.
    /// </summary>
    public interface IAuthentication
    {
    }

    /// <inheritdoc />
    public interface IAuthentication<out TAuthInfo> : IAuthentication
        where TAuthInfo : IAuthInfo
    {
        /// <summary>
        /// Currently loaded <see cref="IAuthInfo"/>.
        /// </summary>
        TAuthInfo Info { get; }
    }
}
