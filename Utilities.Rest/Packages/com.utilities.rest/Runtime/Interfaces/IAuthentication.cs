// Licensed under the MIT License. See LICENSE in the project root for license information.

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
}
