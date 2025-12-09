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

        bool TryGetDownloadCacheItem(string fileName, out Uri filePath);

        bool TryGetDownloadCacheItem(Uri uri, out Uri filePath);

        bool TryDeleteCacheItem(Uri uri);

        void DeleteDownloadCache();

        Task WriteCacheItemAsync(byte[] data, Uri cachePath, CancellationToken cancellationToken);
    }
}
