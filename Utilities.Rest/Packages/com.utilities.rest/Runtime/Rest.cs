// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Utilities.Async;

namespace Utilities.WebRequestRest
{
    /// <summary>
    /// REST Class for CRUD Transactions.
    /// </summary>
    public static class Rest
    {
        private const string khttpVerbPATCH = "PATCH";

        private const string fileUriPrefix = "file://";

        #region Authentication

        /// <summary>
        /// Gets the Basic auth header.
        /// </summary>
        /// <param name="username">The Username.</param>
        /// <param name="password">The password.</param>
        /// <returns>The Basic authorization header encoded to base 64.</returns>
        public static string GetBasicAuthentication(string username, string password)
            => $"Basic {Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes($"{username}:{password}"))}";

        /// <summary>
        /// Gets the Bearer auth header.
        /// </summary>
        /// <param name="authToken">OAuth Token to be used.</param>
        /// <returns>The Bearer authorization header.</returns>
        public static string GetBearerOAuthToken(string authToken)
            => $"Bearer {authToken}";

        #endregion Authentication

        #region GET

        /// <summary>
        /// Rest GET.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="headers">Optional, header information for the request.</param>
        /// <param name="progress">Optional, <see cref="IProgress{T}"/> handler.</param>
        /// <param name="timeout">Optional, time in seconds before request expires.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> GetAsync(
            string query,
            Dictionary<string, string> headers = null,
            IProgress<Progress> progress = null,
            int timeout = -1,
            CancellationToken cancellationToken = default)
        {
            using var webRequest = UnityWebRequest.Get(query);
            return await ProcessRequestAsync(webRequest, headers, progress, timeout, cancellationToken);
        }

        #endregion GET

        #region POST

        /// <summary>
        /// Rest POST.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="headers">Optional, header information for the request.</param>
        /// <param name="progress">Optional, <see cref="IProgress{T}"/> handler.</param>
        /// <param name="timeout">Optional, time in seconds before request expires.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PostAsync(
            string query,
            Dictionary<string, string> headers = null,
            IProgress<Progress> progress = null,
            int timeout = -1,
            CancellationToken cancellationToken = default)
        {
#if UNITY_2022_2_OR_NEWER
            using var webRequest = UnityWebRequest.PostWwwForm(query, null);
#else
            using var webRequest = UnityWebRequest.Post(query, null as string);
#endif
            return await ProcessRequestAsync(webRequest, headers, progress, timeout, cancellationToken);
        }

        /// <summary>
        /// Rest POST.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="formData">Form Data.</param>
        /// <param name="headers">Optional, header information for the request.</param>
        /// <param name="progress">Optional, <see cref="IProgress{T}"/> handler.</param>
        /// <param name="timeout">Optional, time in seconds before request expires.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PostAsync(
            string query,
            WWWForm formData,
            Dictionary<string, string> headers = null,
            IProgress<Progress> progress = null,
            int timeout = -1,
            CancellationToken cancellationToken = default)
        {
            using var webRequest = UnityWebRequest.Post(query, formData);
            return await ProcessRequestAsync(webRequest, headers, progress, timeout, cancellationToken);
        }

        /// <summary>
        /// Rest POST.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="jsonData">JSON data for the request.</param>
        /// <param name="headers">Optional, header information for the request.</param>
        /// <param name="progress">Optional, <see cref="IProgress{T}"/> handler.</param>
        /// <param name="timeout">Optional, time in seconds before request expires.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PostAsync(
            string query,
            string jsonData,
            Dictionary<string, string> headers = null,
            IProgress<Progress> progress = null,
            int timeout = -1,
            CancellationToken cancellationToken = default)
        {
#if UNITY_2022_2_OR_NEWER
            using var webRequest = UnityWebRequest.PostWwwForm(query, UnityWebRequest.kHttpVerbPOST);
#else
            using var webRequest = UnityWebRequest.Post(query, UnityWebRequest.kHttpVerbPOST);
#endif
            var data = new UTF8Encoding().GetBytes(jsonData);
            webRequest.uploadHandler = new UploadHandlerRaw(data);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Accept", "application/json");
            return await ProcessRequestAsync(webRequest, headers, progress, timeout, cancellationToken);
        }

        /// <summary>
        /// Rest POST.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="bodyData">The raw data to post.</param>
        /// <param name="headers">Optional, header information for the request.</param>
        /// <param name="progress">Optional, <see cref="IProgress{T}"/> handler.</param>
        /// <param name="timeout">Optional, time in seconds before request expires.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PostAsync(
            string query,
            byte[] bodyData,
            Dictionary<string, string> headers = null,
            IProgress<Progress> progress = null,
            int timeout = -1,
            CancellationToken cancellationToken = default)
        {
#if UNITY_2022_2_OR_NEWER
            using var webRequest = UnityWebRequest.PostWwwForm(query, UnityWebRequest.kHttpVerbPOST);
#else
            using var webRequest = UnityWebRequest.Post(query, UnityWebRequest.kHttpVerbPOST);
#endif
            webRequest.uploadHandler = new UploadHandlerRaw(bodyData);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.SetRequestHeader("Content-Type", "application/octet-stream");
            return await ProcessRequestAsync(webRequest, headers, progress, timeout, cancellationToken);
        }

        #endregion POST

        #region PUT

        /// <summary>
        /// Rest PUT.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="jsonData">Data to be submitted.</param>
        /// <param name="headers">Optional, header information for the request.</param>
        /// <param name="progress">Optional, <see cref="IProgress{T}"/> handler.</param>
        /// <param name="timeout">Optional, time in seconds before request expires.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PutAsync(
            string query,
            string jsonData,
            Dictionary<string, string> headers = null,
            IProgress<Progress> progress = null,
            int timeout = -1,
            CancellationToken cancellationToken = default)
        {
            using var webRequest = UnityWebRequest.Put(query, jsonData);
            webRequest.SetRequestHeader("Content-Type", "application/json");
            return await ProcessRequestAsync(webRequest, headers, progress, timeout, cancellationToken);
        }

        /// <summary>
        /// Rest PUT.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="bodyData">Data to be submitted.</param>
        /// <param name="headers">Optional, header information for the request.</param>
        /// <param name="progress">Optional, <see cref="IProgress{T}"/> handler.</param>
        /// <param name="timeout">Optional, time in seconds before request expires.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PutAsync(
            string query,
            byte[] bodyData,
            Dictionary<string, string> headers = null,
            IProgress<Progress> progress = null,
            int timeout = -1,
            CancellationToken cancellationToken = default)
        {
            using var webRequest = UnityWebRequest.Put(query, bodyData);
            webRequest.SetRequestHeader("Content-Type", "application/octet-stream");
            return await ProcessRequestAsync(webRequest, headers, progress, timeout, cancellationToken);
        }

        #endregion PUT

        #region PATCH

        /// <summary>
        /// Rest PATCH.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="jsonData">Data to be submitted.</param>
        /// <param name="headers">Optional, header information for the request.</param>
        /// <param name="progress">Optional, <see cref="IProgress{T}"/> handler.</param>
        /// <param name="timeout">Optional, time in seconds before request expires.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PatchAsync(
            string query,
            string jsonData,
            Dictionary<string, string> headers = null,
            IProgress<Progress> progress = null,
            int timeout = -1,
            CancellationToken cancellationToken = default)
        {
            using var webRequest = UnityWebRequest.Put(query, jsonData);
            webRequest.method = khttpVerbPATCH;
            webRequest.SetRequestHeader("Content-Type", "application/json");
            return await ProcessRequestAsync(webRequest, headers, progress, timeout, cancellationToken);
        }

        /// <summary>
        /// Rest PATCH.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="bodyData">Data to be submitted.</param>
        /// <param name="headers">Optional, header information for the request.</param>
        /// <param name="progress">Optional, <see cref="IProgress{T}"/> handler.</param>
        /// <param name="timeout">Optional, time in seconds before request expires.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PatchAsync(
            string query,
            byte[] bodyData,
            Dictionary<string, string> headers = null,
            IProgress<Progress> progress = null,
            int timeout = -1,
            CancellationToken cancellationToken = default)
        {
            using var webRequest = UnityWebRequest.Put(query, bodyData);
            webRequest.method = khttpVerbPATCH;
            webRequest.SetRequestHeader("Content-Type", "application/octet-stream");
            return await ProcessRequestAsync(webRequest, headers, progress, timeout, cancellationToken);
        }

        #endregion PATCH

        #region DELETE

        /// <summary>
        /// Rest DELETE.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="headers">Optional, header information for the request.</param>
        /// <param name="progress">Optional, <see cref="IProgress{T}"/> handler.</param>
        /// <param name="timeout">Optional, time in seconds before request expires.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> DeleteAsync(
            string query,
            Dictionary<string, string> headers = null,
            IProgress<Progress> progress = null,
            int timeout = -1,
            CancellationToken cancellationToken = default)
        {
            using var webRequest = UnityWebRequest.Delete(query);
            return await ProcessRequestAsync(webRequest, headers, progress, timeout, cancellationToken);
        }

        #endregion DELETE

        #region Get Multimedia Content

        #region Download Cache

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
        public static string DownloadCacheDirectory
            => Path.Combine(Application.temporaryCachePath, DOWNLOAD_CACHE);

        /// <summary>
        /// Creates the <see cref="DownloadCacheDirectory"/> if it doesn't exist.
        /// </summary>
        public static void ValidateCacheDirectory()
        {
            if (!Directory.Exists(DownloadCacheDirectory))
            {
                Directory.CreateDirectory(DownloadCacheDirectory);
            }
        }

        /// <summary>
        /// Creates the <see cref="DownloadCacheDirectory"/> if it doesn't exist.
        /// </summary>
        public static async Task ValidateCacheDirectoryAsync()
        {
            await Awaiters.UnityMainThread;
            ValidateCacheDirectory();
        }

        /// <summary>
        /// Try to get a file out of the download cache by uri reference.
        /// </summary>
        /// <param name="uri">The uri key of the item.</param>
        /// <param name="filePath">The file path to the cached item.</param>
        /// <returns>True, if the item was in cache, otherwise false.</returns>
        public static bool TryGetDownloadCacheItem(string uri, out string filePath)
        {
            ValidateCacheDirectory();
            bool exists;

            if (uri.Contains(fileUriPrefix))
            {
                filePath = uri;
                return File.Exists(uri.Replace(fileUriPrefix, string.Empty));
            }

            if (TryGetFileNameFromUrl(uri, out var fileName))
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

        /// <summary>
        /// Try to delete the cached item at the uri.
        /// </summary>
        /// <param name="uri">The uri key of the item.</param>
        /// <returns>True, if the cached item was successfully deleted.</returns>
        public static bool TryDeleteCacheItem(string uri)
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

        /// <summary>
        /// Deletes all the files in the download cache.
        /// </summary>
        public static void DeleteDownloadCache()
        {
            if (Directory.Exists(DownloadCacheDirectory))
            {
                Directory.Delete(DownloadCacheDirectory, true);
            }
        }

        /// <summary>
        /// We will try go guess the name based on the url.
        /// </summary>
        /// <param name="url">The url to parse to try to guess file name.</param>
        /// <param name="fileName">The filename if found.</param>
        /// <returns>True, if a valid filename is found.</returns>
        private static bool TryGetFileNameFromUrl(string url, out string fileName)
        {
            var baseUrl = UnityWebRequest.UnEscapeURL(url);
            var rootUrl = baseUrl.Split("?")[0];
            var index = rootUrl.LastIndexOf('/') + 1;
            fileName = rootUrl.Substring(index, rootUrl.Length - index);
            return Path.HasExtension(fileName);
        }

        #endregion Download Cache

        /// <summary>
        /// Download a <see cref="Texture2D"/> from the provided <see cref="url"/>.
        /// </summary>
        /// <param name="url">The url to download the <see cref="Texture2D"/> from.</param>
        /// <param name="fileName">Optional, file name to download (including extension).</param>
        /// <param name="headers">Optional, header information for the request.</param>
        /// <param name="progress">Optional, <see cref="IProgress{T}"/> handler.</param>
        /// <param name="timeout">Optional, time in seconds before request expires.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>A new <see cref="Texture2D"/> instance.</returns>
        public static async Task<Texture2D> DownloadTextureAsync(
            string url,
            string fileName = null,
            Dictionary<string, string> headers = null,
            IProgress<Progress> progress = null,
            int timeout = -1,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;

            if (string.IsNullOrWhiteSpace(fileName))
            {
                TryGetFileNameFromUrl(url, out fileName);
            }

            bool isCached;
            string cachePath;

            if (url.Contains(fileUriPrefix))
            {
                isCached = true;
                cachePath = url;
            }
            else
            {
                isCached = TryGetDownloadCacheItem(fileName, out cachePath);
            }

            if (isCached)
            {
                url = cachePath;
            }

            using var webRequest = UnityWebRequestTexture.GetTexture(url);
            var response = await ProcessRequestAsync(webRequest, headers, progress, timeout, cancellationToken);

            if (!response.Successful)
            {
                Debug.LogError($"Failed to download texture from \"{url}\"!\n[{response.ResponseCode}] {response.ResponseBody}");
                return null;
            }

            var downloadHandler = (DownloadHandlerTexture)webRequest.downloadHandler;

            if (!isCached &&
                !File.Exists(cachePath))
            {
                var fileStream = File.OpenWrite(cachePath);

                try
                {
                    await fileStream.WriteAsync(downloadHandler.data, 0, downloadHandler.data.Length, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to write texture to disk!\n{e}");
                }
                finally
                {
                    await fileStream.DisposeAsync().ConfigureAwait(false);
                }
            }

            await Awaiters.UnityMainThread;
            var texture = downloadHandler.texture;
            downloadHandler.Dispose();
            texture.name = Path.GetFileNameWithoutExtension(cachePath);
            return texture;
        }

        /// <summary>
        /// Download a <see cref="AudioClip"/> from the provided <see cref="url"/>.
        /// </summary>
        /// <param name="url">The url to download the <see cref="AudioClip"/> from.</param>
        /// <param name="audioType"><see cref="AudioType"/> to download.</param>
        /// <param name="fileName">Optional, file name to download (including extension).</param>
        /// <param name="headers">Optional, header information for the request.</param>
        /// <param name="progress">Optional, <see cref="IProgress{T}"/> handler.</param>
        /// <param name="timeout">Optional, time in seconds before request expires.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>A new <see cref="AudioClip"/> instance.</returns>
        public static async Task<AudioClip> DownloadAudioClipAsync(
            string url,
            AudioType audioType,
            string fileName = null,
            Dictionary<string, string> headers = null,
            IProgress<Progress> progress = null,
            int timeout = -1,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;

            if (string.IsNullOrWhiteSpace(fileName))
            {
                TryGetFileNameFromUrl(url, out fileName);
            }

            bool isCached;
            string cachePath;

            if (url.Contains(fileUriPrefix))
            {
                isCached = true;
                cachePath = url;
            }
            else
            {
                isCached = TryGetDownloadCacheItem(fileName, out cachePath);
            }

            if (isCached)
            {
                url = cachePath;
            }

            using var webRequest = UnityWebRequestMultimedia.GetAudioClip(url, audioType);
            var response = await ProcessRequestAsync(webRequest, headers, progress, timeout, cancellationToken);

            if (!response.Successful)
            {
                Debug.LogError($"Failed to download audio clip from \"{url}\"!\n[{response.ResponseCode}] {response.ResponseBody}");

                return null;
            }

            var downloadHandler = (DownloadHandlerAudioClip)webRequest.downloadHandler;

            if (!isCached &&
                !File.Exists(cachePath))
            {
                var fileStream = File.OpenWrite(cachePath);

                try
                {
                    await fileStream.WriteAsync(downloadHandler.data, 0, downloadHandler.data.Length, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to write audio asset to disk! {e}");
                }
                finally
                {
                    await fileStream.DisposeAsync().ConfigureAwait(false);
                }
            }

            await Awaiters.UnityMainThread;
            var clip = downloadHandler.audioClip;
            clip.name = Path.GetFileNameWithoutExtension(cachePath);
            return clip;
        }

        /// <summary>
        /// Download a <see cref="AudioClip"/> from the provided <see cref="url"/>.
        /// </summary>
        /// <param name="url">The url to download the <see cref="AudioClip"/> from.</param>
        /// <param name="audioType"><see cref="AudioType"/> to download.</param>
        /// <param name="onStreamPlaybackReady"><see cref="Action{T}"/> callback raised when stream is ready to be played.</param>
        /// <param name="httpMethod">Optional, must be either GET or POST.</param>
        /// <param name="jsonData">Optional, json payload. Only <see cref="jsonData"/> OR <see cref="payload"/> can be supplied.</param>
        /// <param name="payload">Optional, raw byte payload. Only <see cref="payload"/> OR <see cref="jsonData"/> can be supplied.</param>
        /// <param name="fileName">Optional, file name to download (including extension).</param>
        /// <param name="playbackAmountThreshold">Optional, the amount of data to to download before signaling that streaming is ready.</param>
        /// <param name="headers">Optional, header information for the request.</param>
        /// <param name="timeout">Optional, time in seconds before request expires.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>Raw downloaded bytes from the stream.</returns>
        public static async Task<AudioClip> StreamAudioAsync(
            string url,
            AudioType audioType,
            Action<AudioClip> onStreamPlaybackReady,
            string httpMethod = UnityWebRequest.kHttpVerbGET,
            string fileName = null,
            string jsonData = null,
            byte[] payload = null,
            ulong playbackAmountThreshold = 10000,
            Dictionary<string, string> headers = null,
            int timeout = -1,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;

            if (string.IsNullOrWhiteSpace(fileName))
            {
                TryGetFileNameFromUrl(url, out fileName);
            }

            bool isCached;
            string cachePath;

            if (url.Contains(fileUriPrefix))
            {
                isCached = true;
                cachePath = url;
            }
            else
            {
                isCached = TryGetDownloadCacheItem(fileName, out cachePath);
            }

            if (isCached)
            {
                url = cachePath;
                // override the httpMethod
                httpMethod = UnityWebRequest.kHttpVerbGET;
            }

            AudioClip clip = null;
            var streamStarted = false;
            UploadHandler uploadHandler = null;
            Progress<Progress> progress = null;

            if (httpMethod == UnityWebRequest.kHttpVerbPOST)
            {
                if (!string.IsNullOrWhiteSpace(jsonData))
                {
                    if (payload != null)
                    {
                        throw new ArgumentException($"{nameof(payload)} and {nameof(jsonData)} cannot be supplied in the same request. Choose either one or the other.", nameof(jsonData));
                    }

                    payload = new UTF8Encoding().GetBytes(jsonData);

                    if (headers != null)
                    {
                        headers.Add("Content-Type", "application/json");
                    }
                    else
                    {
                        headers = new Dictionary<string, string>
                        {
                            { "Content-Type", "application/json" }
                        };
                    }
                }

                uploadHandler = new UploadHandlerRaw(payload);
            }

            var downloadHandler = new DownloadHandlerAudioClip(string.Empty, audioType)
            {
                streamAudio = true // Due to a Unity bug this is actually totally non-functional... https://forum.unity.com/threads/downloadhandleraudioclip-streamaudio-is-ignored.699908/
            };
            using var webRequest = new UnityWebRequest(url, httpMethod, downloadHandler, uploadHandler);
            webRequest.disposeDownloadHandlerOnDispose = false;

            if (!isCached)
            {
                progress = new Progress<Progress>(report =>
                {
                    // only raise stream ready if we haven't assigned a clip yet.
                    if (clip != null)
                    {
                        return;
                    }

                    try
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        if (report.Position > playbackAmountThreshold || downloadHandler.isDone)
                        {
                            // ReSharper disable once AccessToDisposedClosure
                            var tempClip = downloadHandler.audioClip;
                            clip = tempClip;
                            clip.name = Path.GetFileNameWithoutExtension(fileName);
                            streamStarted = true;
                            onStreamPlaybackReady?.Invoke(clip);
                        }
                    }
                    catch (Exception)
                    {
                        // Ignored
                    }
                });
            }

            var response = await ProcessRequestAsync(webRequest, headers, progress, timeout, cancellationToken);

            if (!response.Successful)
            {
                Debug.LogError($"Failed to download audio clip from \"{url}\"!\n[{response.ResponseCode}] {response.ResponseBody}");
                return null;
            }

            if (!isCached &&
                !File.Exists(cachePath))
            {
                var fileStream = File.OpenWrite(cachePath);

                try
                {
                    await fileStream.WriteAsync(downloadHandler.data, 0, downloadHandler.data.Length, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to write audio asset to disk! {e}");
                }
                finally
                {
                    await fileStream.DisposeAsync().ConfigureAwait(false);
                }
            }

            await Awaiters.UnityMainThread;
            var loadedClip = downloadHandler.audioClip;
            loadedClip.name = Path.GetFileNameWithoutExtension(fileName);

            if (!streamStarted)
            {
                streamStarted = true;
                onStreamPlaybackReady?.Invoke(loadedClip);
            }

            downloadHandler.Dispose();
            return loadedClip;
        }

#if UNITY_ADDRESSABLES

        /// <summary>
        /// Download a <see cref="AssetBundle"/> from the provided <see cref="url"/>.
        /// </summary>
        /// <param name="url">The url to download the <see cref="AssetBundle"/> from.</param>
        /// <param name="options">Asset bundle request options.</param>
        /// <param name="headers">Optional, header information for the request.</param>
        /// <param name="progress">Optional, <see cref="IProgress{T}"/> handler.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>A new <see cref="AssetBundle"/> instance.</returns>
        public static async Task<AssetBundle> DownloadAssetBundleAsync(
            string url,
            UnityEngine.ResourceManagement.ResourceProviders.AssetBundleRequestOptions options,
            Dictionary<string, string> headers = null,
            IProgress<Progress> progress = null,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;

            UnityWebRequest webRequest;

            if (options == null)
            {
                webRequest = UnityWebRequestAssetBundle.GetAssetBundle(url);
            }
            else
            {
                if (!string.IsNullOrEmpty(options.Hash))
                {
                    CachedAssetBundle cachedBundle = new CachedAssetBundle(options.BundleName, Hash128.Parse(options.Hash));
#if ENABLE_CACHING
                    if (options.UseCrcForCachedBundle || !Caching.IsVersionCached(cachedBundle))
                    {
                        webRequest = UnityWebRequestAssetBundle.GetAssetBundle(url, cachedBundle, options.Crc);
                    }
                    else
                    {
                        webRequest = UnityWebRequestAssetBundle.GetAssetBundle(url, cachedBundle);
                    }
#else
                    webRequest = UnityWebRequestAssetBundle.GetAssetBundle(url, cachedBundle, options.Crc);
#endif
                }
                else
                {
                    webRequest = UnityWebRequestAssetBundle.GetAssetBundle(url, options.Crc);
                }

                if (options.Timeout > 0)
                {
                    webRequest.timeout = options.Timeout;
                }

                if (options.RedirectLimit > 0)
                {
                    webRequest.redirectLimit = options.RedirectLimit;
                }
            }

            using (webRequest)
            {
                Response response;

                try
                {
                    response = await ProcessRequestAsync(webRequest, headers, progress, options?.Timeout ?? -1, cancellationToken);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    throw;
                }

                if (!response.Successful)
                {
                    Debug.LogError($"Failed to download asset bundle from \"{url}\"!\n{response.ResponseCode}:{response.ResponseBody}");
                    return null;
                }

                var downloadHandler = (DownloadHandlerAssetBundle)webRequest.downloadHandler;
                return downloadHandler.assetBundle;
            }
        }

#endif // UNITY_ADDRESSABLES

        /// <summary>
        /// Download a file from the provided <see cref="url"/>.
        /// </summary>
        /// <param name="url">The url to download the file from.</param>
        /// <param name="fileName">Optional, file name to download (including extension).</param>
        /// <param name="headers">Optional, header information for the request.</param>
        /// <param name="progress">Optional, <see cref="IProgress{T}"/> handler.</param>
        /// <param name="timeout">Optional, time in seconds before request expires.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The path to the downloaded file.</returns>
        public static async Task<string> DownloadFileAsync(
            string url,
            string fileName = null,
            Dictionary<string, string> headers = null,
            IProgress<Progress> progress = null,
            int timeout = -1,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;

            if (string.IsNullOrWhiteSpace(fileName))
            {
                TryGetFileNameFromUrl(url, out fileName);
            }

            if (TryGetDownloadCacheItem(fileName, out var filePath))
            {
                return filePath;
            }

            using var webRequest = UnityWebRequest.Get(url);
            using var fileDownloadHandler = new DownloadHandlerFile(filePath)
            {
                removeFileOnAbort = true
            };

            webRequest.downloadHandler = fileDownloadHandler;
            var response = await ProcessRequestAsync(webRequest, headers, progress, timeout, cancellationToken);

            if (!response.Successful)
            {
                Debug.LogError($"Failed to download file from \"{url}\"!\n[{response.ResponseCode}] {response.ResponseBody}");

                return null;
            }

            return filePath;
        }

        #endregion Get Multimedia Content

        private static async Task<Response> ProcessRequestAsync(
            UnityWebRequest webRequest,
            Dictionary<string, string> headers,
            IProgress<Progress> progress,
            int timeout,
            CancellationToken cancellationToken)
        {
            await Awaiters.UnityMainThread;

            if (timeout > 0)
            {
                webRequest.timeout = timeout;
            }

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    webRequest.SetRequestHeader(header.Key, header.Value);
                }
            }

            var isUpload = webRequest.method is
                UnityWebRequest.kHttpVerbPOST or
                UnityWebRequest.kHttpVerbPUT or
                khttpVerbPATCH;

            // HACK: Workaround for extra quotes around boundary.
            if (isUpload)
            {
                var contentType = webRequest.GetRequestHeader("Content-Type");

                if (contentType != null)
                {
                    contentType = contentType.Replace("\"", "");
                    webRequest.SetRequestHeader("Content-Type", contentType);
                }
            }

            webRequest.disposeCertificateHandlerOnDispose = true;
            webRequest.disposeDownloadHandlerOnDispose = true;
            webRequest.disposeUploadHandlerOnDispose = true;

            Thread backgroundThread = null;

            if (progress != null)
            {
                async void ProgressReportingThread()
                {
                    var frame = 0;

                    try
                    {
                        await Awaiters.UnityMainThread;

                        // Define constants for data units
                        const double kbSize = 1e+3;
                        const double mbSize = 1e+6;
                        const double gbSize = 1e+9;
                        const double tbSize = 1e+12;

                        while (!webRequest.isDone)
                        {
                            // Calculate the amount of bytes downloaded during this frame
                            var bytesThisFrame = (webRequest.downloadedBytes * 8) / (frame++ * 0.5f);
                            // Determine the appropriate data unit for the speed based on the size of bytes downloaded this frame
                            var unit = bytesThisFrame switch
                            {
                                _ when bytesThisFrame > tbSize => Progress.DataUnit.TB,
                                _ when bytesThisFrame > gbSize => Progress.DataUnit.GB,
                                _ when bytesThisFrame > mbSize => Progress.DataUnit.MB,
                                _ when bytesThisFrame > kbSize => Progress.DataUnit.kB,
                                _ => Progress.DataUnit.b
                            };
                            // Calculate the speed based on the size of bytes downloaded this frame and the appropriate data unit
                            var speed = bytesThisFrame switch
                            {
                                _ when bytesThisFrame > tbSize => (float)Math.Round(bytesThisFrame / tbSize),
                                _ when bytesThisFrame > gbSize => (float)Math.Round(bytesThisFrame / gbSize),
                                _ when bytesThisFrame > mbSize => (float)Math.Round(bytesThisFrame / mbSize),
                                _ when bytesThisFrame > kbSize => (float)Math.Round(bytesThisFrame / kbSize),
                                _ => bytesThisFrame
                            };
                            // Determine the percentage of the download or upload that has been completed
                            var percentage = isUpload ? webRequest.uploadProgress : webRequest.downloadProgress * 100f;
                            // Get the content length of the download
                            const string contentLength = "Content-Length";

                            if (!ulong.TryParse(webRequest.GetResponseHeader(contentLength), out var length))
                            {
                                length = webRequest.downloadedBytes;
                            }

                            // Report the progress using the progress handler provided by the caller
                            progress.Report(new Progress(webRequest.downloadedBytes, length, percentage, speed, unit));

                            if (cancellationToken.IsCancellationRequested)
                            {
                                webRequest.Abort();
                            }

                            await Awaiters.UnityMainThread;
                        }
                    }
                    catch (Exception)
                    {
                        // Throw away
                    }
                }

                backgroundThread = new Thread(ProgressReportingThread)
                {
                    IsBackground = true
                };
            }

            backgroundThread?.Start();

            try
            {
                await webRequest.SendWebRequest();
            }
            catch (Exception e)
            {
                Debug.LogError($"{nameof(Rest)}.{nameof(ProcessRequestAsync)}::Send Web Request Failed!\n{e}");
            }

            backgroundThread?.Join();
            progress?.Report(new Progress(webRequest.downloadedBytes, webRequest.downloadedBytes, 100f, 0, Progress.DataUnit.b));

            if (webRequest.result is
                UnityWebRequest.Result.ConnectionError or
                UnityWebRequest.Result.ProtocolError)
            {
                if (webRequest.responseCode == 401)
                {
                    return new Response(false, "Invalid Credentials", null, webRequest.responseCode);
                }

                if (webRequest.GetResponseHeaders() == null)
                {
                    return new Response(false, "Invalid Headers", null, webRequest.responseCode);
                }

                var responseHeaders = webRequest.GetResponseHeaders().Aggregate(string.Empty, (_, header) => $"\n{header.Key}: {header.Value}");
                Debug.LogError($"REST Error {webRequest.responseCode}:{webRequest.downloadHandler?.error}{responseHeaders}");
                return new Response(false, $"{responseHeaders}\n{webRequest.downloadHandler?.error}", null, webRequest.responseCode);
            }

            if (!string.IsNullOrEmpty(webRequest.downloadHandler.error))
            {
                return new Response(false, webRequest.downloadHandler.error, webRequest.downloadHandler.data, webRequest.responseCode);
            }

            switch (webRequest.downloadHandler)
            {
                case DownloadHandlerFile:
                case DownloadHandlerScript:
                case DownloadHandlerTexture:
                case DownloadHandlerAudioClip:
                case DownloadHandlerAssetBundle:
                    return new Response(true, null, null, webRequest.responseCode);
                case DownloadHandlerBuffer bufferDownloadHandler:
                    return new Response(true, bufferDownloadHandler.text, bufferDownloadHandler.data, webRequest.responseCode);
                default:
                    return new Response(true, webRequest.downloadHandler?.text, webRequest.downloadHandler?.data, webRequest.responseCode);
            }
        }
    }
}
