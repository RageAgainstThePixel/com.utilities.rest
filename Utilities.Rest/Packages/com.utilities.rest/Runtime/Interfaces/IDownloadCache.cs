// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Utilities.WebRequestRest.Interfaces
{
    internal interface IDownloadCache
    {
        void ValidateCacheDirectory();

        Task ValidateCacheDirectoryAsync();

        bool TryGetDownloadCacheItem(string uri, out string filePath);

        bool TryDeleteCacheItem(string uri);

        void DeleteDownloadCache();

        Task WriteCacheItemAsync(byte[] data, string cachePath, CancellationToken cancellationToken);
    }
}
