// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Utilities.Async;
using Utilities.WebRequestRest.Interfaces;

namespace Utilities.WebRequestRest
{
    internal class DiskDownloadCache : IDownloadCache
    {
        public void ValidateCacheDirectory()
        {
            if (!Directory.Exists(Rest.DownloadCacheDirectory))
            {
                Directory.CreateDirectory(Rest.DownloadCacheDirectory);
            }
        }

        public async Task ValidateCacheDirectoryAsync()
        {
            await Awaiters.UnityMainThread;
            ValidateCacheDirectory();
        }

        [Obsolete]
        public bool TryGetDownloadCacheItem(string uri, out string filePath)
        {
            var result = TryGetDownloadCacheItem(new Uri(uri), out var local);
            filePath = local.LocalPath;
            return result;
        }

        public bool TryGetDownloadCacheItem(Uri uri, out Uri filePath)
        {
            ValidateCacheDirectory();
            bool exists;

            if (uri.IsFile)
            {
                filePath = uri;
                return File.Exists(uri.LocalPath);
            }

            if (Rest.TryGetFileNameFromUri(uri, out var fileName))
            {
                filePath = new Uri(Path.Combine(Rest.DownloadCacheDirectory, fileName));
                exists = File.Exists(filePath.LocalPath);
            }
            else
            {
                filePath = new Uri(Path.Combine(Rest.DownloadCacheDirectory, uri.GenerateGuidString()));
                exists = File.Exists(filePath.LocalPath);
            }

            if (exists)
            {
                filePath = new Uri(Path.GetFullPath(filePath.LocalPath));
            }

            return exists;
        }

        [Obsolete]
        public bool TryDeleteCacheItem(string uri)
            => TryDeleteCacheItem(new Uri(uri));

        public bool TryDeleteCacheItem(Uri uri)
        {
            if (!TryGetDownloadCacheItem(uri, out var filePath))
            {
                return false;
            }

            try
            {
                File.Delete(filePath.LocalPath);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return !File.Exists(filePath.LocalPath);
        }

        public void DeleteDownloadCache()
        {
            if (Directory.Exists(Rest.DownloadCacheDirectory))
            {
                Directory.Delete(Rest.DownloadCacheDirectory, true);
            }
        }

        [Obsolete]
        public Task WriteCacheItemAsync(byte[] data, string cachePath, CancellationToken cancellationToken)
            => WriteCacheItemAsync(data, new Uri(cachePath), cancellationToken);

        public async Task WriteCacheItemAsync(byte[] data, Uri cachePath, CancellationToken cancellationToken)
        {
            if (File.Exists(cachePath.LocalPath)) { return; }

            try
            {
                await File.WriteAllBytesAsync(cachePath.LocalPath, data, cancellationToken).ConfigureAwait(true);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to write asset to disk! {e}");
            }
        }
    }
}
