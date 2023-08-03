using System.Threading;
using System.Threading.Tasks;
using Utilities.WebRequestRest.Interfaces;

namespace Utilities.Rest
{
    public class NoOpDownloadCache : IDownloadCache
    {
        public void ValidateCacheDirectory()
        {

        }

        public Task ValidateCacheDirectoryAsync()
        {
            return Task.CompletedTask;
        }

        public bool TryGetDownloadCacheItem(string uri, out string filePath)
        {
            filePath = uri;
            return false;
        }

        public bool TryDeleteCacheItem(string uri)
        {
            return true;
        }

        public void DeleteDownloadCache()
        {

        }

        public Task CacheItemAsync(byte[] data, string cachePath, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
