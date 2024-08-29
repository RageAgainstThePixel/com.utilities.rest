// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    internal class DiskDownloadCache : IDownloadCache
    {
        /// <summary>
        /// Generates a <see cref="Guid"/> based on the string.
        /// </summary>
        /// <param name="string">The string to generate the <see cref="Guid"/>.</param>
        /// <returns>A new <see cref="Guid"/> that represents the string.</returns>
        private static Guid GenerateGuid(string @string)
        {
            using MD5 md5 = MD5.Create();
            return new Guid(md5.ComputeHash(Encoding.UTF8.GetBytes(@string)));
        }

        /// <inheritdoc />
        public void ValidateCacheDirectory()
        {
            if (!Directory.Exists(Rest.DownloadCacheDirectory))
            {
                Directory.CreateDirectory(Rest.DownloadCacheDirectory);
            }
        }

        /// <inheritdoc />
        public async Task ValidateCacheDirectoryAsync()
        {
            await Awaiters.UnityMainThread;
            ValidateCacheDirectory();
        }

        /// <inheritdoc />
        public bool TryGetDownloadCacheItem(string uri, out string filePath)
        {
            ValidateCacheDirectory();
            bool exists;

            if (uri.Contains(Rest.FileUriPrefix))
            {
                filePath = uri;
                return File.Exists(uri.Replace(Rest.FileUriPrefix, string.Empty));
            }

            if (Rest.TryGetFileNameFromUrl(uri, out var fileName))
            {
                filePath = Path.Combine(Rest.DownloadCacheDirectory, fileName);
                exists = File.Exists(filePath);
            }
            else
            {
                filePath = Path.Combine(Rest.DownloadCacheDirectory, GenerateGuid(uri).ToString());
                exists = File.Exists(filePath);
            }

            if (exists)
            {
                filePath = $"{Rest.FileUriPrefix}{Path.GetFullPath(filePath)}";
            }

            return exists;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void DeleteDownloadCache()
        {
            if (Directory.Exists(Rest.DownloadCacheDirectory))
            {
                Directory.Delete(Rest.DownloadCacheDirectory, true);
            }
        }

        /// <inheritdoc />
        public async Task WriteCacheItemAsync(byte[] data, string cachePath, CancellationToken cancellationToken)
        {
            if (File.Exists(cachePath)) { return; }

            try
            {
                await File.WriteAllBytesAsync(cachePath, data, cancellationToken).ConfigureAwait(true);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to write asset to disk! {e}");
            }
        }
    }
}
