// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Utilities.WebRequestRest.Interfaces
{
    internal interface IDownloadCache
    {
        void ValidateCacheDirectory();

        Task ValidateCacheDirectoryAsync();

        [Obsolete]
        bool TryGetDownloadCacheItem(string uri, out string filePath);

        bool TryGetDownloadCacheItem(Uri uri, out Uri filePath);

        [Obsolete]
        bool TryDeleteCacheItem(string uri);

        bool TryDeleteCacheItem(Uri uri);

        void DeleteDownloadCache();

        Task WriteCacheItemAsync(byte[] data, string cachePath, CancellationToken cancellationToken);
    }
}
