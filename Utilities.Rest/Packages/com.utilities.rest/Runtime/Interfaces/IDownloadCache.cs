using System.Threading.Tasks;

namespace Utilities.WebRequestRest.Interfaces
{
    public interface IDownloadCache
    {
        void ValidateCacheDirectory();
        Task ValidateCacheDirectoryAsync();
        bool TryGetDownloadCacheItem(string uri, out string filePath);
        bool TryDeleteCacheItem(string uri);
        void DeleteDownloadCache();
    }
}
