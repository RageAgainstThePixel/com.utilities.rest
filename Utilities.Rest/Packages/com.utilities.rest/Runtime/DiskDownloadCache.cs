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

        public bool TryGetDownloadCacheItem(string fileName, out Uri fileUri)
            => TryGetDownloadCacheItem(Rest.GetCacheItemUri(fileName), out fileUri);

        public bool TryGetDownloadCacheItem(Uri uri, out Uri fileUri)
        {
            ValidateCacheDirectory();
            bool exists;

            if (uri.Scheme == Uri.UriSchemeFile)
            {
                fileUri = uri;
                return File.Exists(uri.LocalPath);
            }

            if (Rest.TryGetFileNameFromUri(uri, out var fileName))
            {
                fileUri = new Uri(Path.Combine(Rest.DownloadCacheDirectory, fileName));
                exists = File.Exists(fileUri.LocalPath);
            }
            else
            {
                fileUri = new Uri(Path.Combine(Rest.DownloadCacheDirectory, uri.GenerateGuidString()));
                exists = File.Exists(fileUri.LocalPath);
            }

            if (exists)
            {
                fileUri = new Uri(Path.GetFullPath(fileUri.LocalPath));
            }

            return exists;
        }

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
