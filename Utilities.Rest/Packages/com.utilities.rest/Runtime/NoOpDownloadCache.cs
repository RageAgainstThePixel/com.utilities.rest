// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Utilities.WebRequestRest.Interfaces;

namespace Utilities.Rest
{
    internal class NoOpDownloadCache : IDownloadCache
    {
        public void ValidateCacheDirectory() { }

        public Task ValidateCacheDirectoryAsync() => Task.CompletedTask;

        public bool TryGetDownloadCacheItem(string uri, out string filePath)
        {
            filePath = uri;
            return false;
        }

        public bool TryDeleteCacheItem(string uri) => true;

        public void DeleteDownloadCache() { }

        public Task WriteCacheItemAsync(byte[] data, string cachePath, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
