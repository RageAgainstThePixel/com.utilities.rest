using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Utilities.Async;
using Utilities.WebRequestRest.Interfaces;

namespace Utilities.WebRequestRest
{
    public class DiskDownloadCache : IDownloadCache
    {
        private const string fileUriPrefix = "file://";
        private const string DOWNLOAD_CACHE = "download_cache";

        /// <summary>
        /// Generates a <see cref="Guid"/> based on the string.
        /// </summary>
        /// <param name="string">The string to generate the <see cref="Guid"/>.</param>
        /// <returns>A new <see cref="Guid"/> that represents the string.</returns>
        private static Guid GenerateGuid(string @string)
        {
            using MD5 md5 = MD5.Create();
            return new Guid(md5.ComputeHash(Encoding.Default.GetBytes(@string)));
        }

        /// <summary>
        /// The download cache directory.<br/>
        /// </summary>
        private static string DownloadCacheDirectory
            => Path.Combine(Application.temporaryCachePath, DOWNLOAD_CACHE);

        public void ValidateCacheDirectory()
        {
            if (!Directory.Exists(DownloadCacheDirectory))
            {
                Directory.CreateDirectory(DownloadCacheDirectory);
            }
        }

        public async Task ValidateCacheDirectoryAsync()
        {
            await Awaiters.UnityMainThread;
            ValidateCacheDirectory();
        }

        public bool TryGetDownloadCacheItem(string uri, out string filePath)
        {
            ValidateCacheDirectory();
            bool exists;

            if (uri.Contains(fileUriPrefix))
            {
                filePath = uri;
                return File.Exists(uri.Replace(fileUriPrefix, string.Empty));
            }

            if (Rest.TryGetFileNameFromUrl(uri, out var fileName))
            {
                filePath = Path.Combine(DownloadCacheDirectory, fileName);
                exists = File.Exists(filePath);
            }
            else
            {
                filePath = Path.Combine(DownloadCacheDirectory, GenerateGuid(uri).ToString());
                exists = File.Exists(filePath);
            }

            if (exists)
            {
                filePath = $"{fileUriPrefix}{Path.GetFullPath(filePath)}";
            }

            return exists;
        }

        public bool TryDeleteCacheItem(string uri)
        {
            if (!TryGetDownloadCacheItem(uri, out var filePath))
            {
                return false;
            }

            try
            {
                File.Delete(filePath);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return !File.Exists(filePath);
        }

        public void DeleteDownloadCache()
        {
            if (Directory.Exists(DownloadCacheDirectory))
            {
                Directory.Delete(DownloadCacheDirectory, true);
            }
        }

        public async Task CacheItemAsync(byte[] data, string cachePath, CancellationToken cancellationToken)
        {
            if (File.Exists(cachePath))
            {
                return;
            }

            var fileStream = File.OpenWrite(cachePath);

            try
            {
                await fileStream.WriteAsync(data, 0, data.Length, cancellationToken);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to write asset to disk! {e}");
            }
            finally
            {
                await fileStream.DisposeAsync();
            }
        }
    }
}
