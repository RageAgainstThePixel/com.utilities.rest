// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Utilities.WebRequestRest.Interfaces;

namespace Utilities.Rest
{
    internal class NoOpDownloadCache : IDownloadCache
    {
        public void ValidateCacheDirectory() { }

        public Task ValidateCacheDirectoryAsync() => Task.CompletedTask;

        public bool TryGetDownloadCacheItem(Uri uri, out Uri filePath)
        {
            filePath = null;
            return false;
        }

        public bool TryDeleteCacheItem(Uri uri) => true;

        public void DeleteDownloadCache() { }

        public Task WriteCacheItemAsync(byte[] data, Uri cachePath, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
