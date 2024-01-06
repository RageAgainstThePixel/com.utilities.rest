// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Utilities.Async;
using Utilities.Rest;
using Utilities.WebRequestRest.Interfaces;
using Debug = UnityEngine.Debug;

namespace Utilities.WebRequestRest
{
    /// <summary>
    /// REST Class for basic CRUD transactions.
    /// </summary>
    public static class Rest
    {
        internal const string FileUriPrefix = "file://";
        private const string kHttpVerbPATCH = "PATCH";
        private const string eventDelimiter = "data: ";
        private const string stopEventDelimiter = "[DONE]";

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
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> GetAsync(
            string query,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
            using var webRequest = UnityWebRequest.Get(query);
            return await webRequest.SendAsync(parameters, cancellationToken);
        }

        /// <summary>
        /// Rest GET.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="serverSentEventCallback"><see cref="Action{T}"/> server sent event callback.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> GetAsync(
            string query,
            Action<string> serverSentEventCallback,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
            using var webRequest = UnityWebRequest.Get(query);
            return await webRequest.SendAsync(parameters, serverSentEventCallback, cancellationToken);
        }

        /// <summary>
        /// Rest GET.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="dataReceivedEventCallback"><see cref="Action{T}"/> data received event callback.</param>
        /// <param name="eventChunkSize"></param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> GetAsync(
            string query,
            Action<Response> dataReceivedEventCallback,
            int? eventChunkSize = null,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
            using var webRequest = UnityWebRequest.Get(query);
            using var downloadHandler = eventChunkSize.HasValue
                ? new DownloadHandlerCallback(webRequest, eventChunkSize.Value)
                : new DownloadHandlerCallback(webRequest);
            downloadHandler.OnDataReceived += dataReceivedEventCallback;

            try
            {
                return await webRequest.SendAsync(parameters, cancellationToken);
            }
            finally
            {
                downloadHandler.OnDataReceived -= dataReceivedEventCallback;
            }
        }

        #endregion GET

        #region POST

        /// <summary>
        /// Rest POST.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PostAsync(
            string query,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
#if UNITY_2022_2_OR_NEWER
            using var webRequest = new UnityWebRequest(query, UnityWebRequest.kHttpVerbPOST);
#else
            using var webRequest = new UnityWebRequest(query, UnityWebRequest.kHttpVerbPOST);
#endif
            return await webRequest.SendAsync(parameters, cancellationToken);
        }

        /// <summary>
        /// Rest POST.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="formData">Form Data.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PostAsync(
            string query,
            WWWForm formData,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
            using var webRequest = UnityWebRequest.Post(query, formData);
            return await webRequest.SendAsync(parameters, cancellationToken);
        }

        /// <summary>
        /// Rest POST.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="jsonData">JSON data for the request.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PostAsync(
            string query,
            string jsonData,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
#if UNITY_2022_2_OR_NEWER
            using var webRequest = new UnityWebRequest(query, UnityWebRequest.kHttpVerbPOST);
#else
            using var webRequest = new UnityWebRequest(query, UnityWebRequest.kHttpVerbPOST);
#endif
            var data = new UTF8Encoding().GetBytes(jsonData);
            using var uploadHandler = new UploadHandlerRaw(data);
            webRequest.uploadHandler = uploadHandler;
            using var downloadHandler = new DownloadHandlerBuffer();
            webRequest.downloadHandler = downloadHandler;
            webRequest.SetRequestHeader("Content-Type", "application/json");
            return await webRequest.SendAsync(parameters, cancellationToken);
        }

        /// <summary>
        /// Rest POST.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="jsonData">JSON data for the request.</param>
        /// <param name="serverSentEventCallback"><see cref="Action{T}"/> server sent event callback.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PostAsync(
            string query,
            string jsonData,
            Action<string> serverSentEventCallback,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
#if UNITY_2022_2_OR_NEWER
            using var webRequest = new UnityWebRequest(query, UnityWebRequest.kHttpVerbPOST);
#else
            using var webRequest = new UnityWebRequest(query, UnityWebRequest.kHttpVerbPOST);
#endif
            var data = new UTF8Encoding().GetBytes(jsonData);
            using var uploadHandler = new UploadHandlerRaw(data);
            webRequest.uploadHandler = uploadHandler;
            using var downloadHandler = new DownloadHandlerBuffer();
            webRequest.downloadHandler = downloadHandler;
            webRequest.SetRequestHeader("Content-Type", "application/json");
            return await webRequest.SendAsync(parameters, serverSentEventCallback, cancellationToken);
        }

        /// <summary>
        /// Rest POST.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="jsonData">JSON data for the request.</param>
        /// <param name="dataReceivedEventCallback"><see cref="Action{T}"/> data received event callback.</param>
        /// <param name="eventChunkSize">Optional, <see cref="dataReceivedEventCallback"/> event chunk size in bytes (Defaults to 512 bytes).</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PostAsync(
            string query,
            string jsonData,
            Action<Response> dataReceivedEventCallback,
            int? eventChunkSize = null,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
#if UNITY_2022_2_OR_NEWER
            using var webRequest = new UnityWebRequest(query, UnityWebRequest.kHttpVerbPOST);
#else
            using var webRequest = new UnityWebRequest(query, UnityWebRequest.kHttpVerbPOST);
#endif
            var data = new UTF8Encoding().GetBytes(jsonData);
            using var uploadHandler = new UploadHandlerRaw(data);
            webRequest.uploadHandler = uploadHandler;
            using var downloadHandler = eventChunkSize.HasValue
                ? new DownloadHandlerCallback(webRequest, eventChunkSize.Value)
                : new DownloadHandlerCallback(webRequest);
            downloadHandler.OnDataReceived += dataReceivedEventCallback;
            webRequest.downloadHandler = downloadHandler;
            webRequest.SetRequestHeader("Content-Type", "application/json");

            try
            {
                return await webRequest.SendAsync(parameters, null, cancellationToken);
            }
            finally
            {
                downloadHandler.OnDataReceived -= dataReceivedEventCallback;
            }
        }

        /// <summary>
        /// Rest POST.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="bodyData">The raw data to post.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PostAsync(
            string query,
            byte[] bodyData,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
#if UNITY_2022_2_OR_NEWER
            using var webRequest = new UnityWebRequest(query, UnityWebRequest.kHttpVerbPOST);
#else
            using var webRequest = new UnityWebRequest(query, UnityWebRequest.kHttpVerbPOST);
#endif
            using var uploadHandler = new UploadHandlerRaw(bodyData);
            webRequest.uploadHandler = uploadHandler;
            using var downloadHandler = new DownloadHandlerBuffer();
            webRequest.downloadHandler = downloadHandler;
            webRequest.SetRequestHeader("Content-Type", "application/octet-stream");
            return await webRequest.SendAsync(parameters, cancellationToken);
        }

        /// <summary>
        /// Rest POST.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="form">The <see cref="IMultipartFormSection"/> to post.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PostAsync(
            string query,
            List<IMultipartFormSection> form,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
#if UNITY_2022_2_OR_NEWER
            using var webRequest = new UnityWebRequest(query, UnityWebRequest.kHttpVerbPOST);
#else
            using var webRequest = new UnityWebRequest(query, UnityWebRequest.kHttpVerbPOST);
#endif
            var boundary = UnityWebRequest.GenerateBoundary();
            var formSections = UnityWebRequest.SerializeFormSections(form, boundary);
            using var uploadHandler = new UploadHandlerRaw(formSections);
            uploadHandler.contentType = $"multipart/form-data; boundary={Encoding.UTF8.GetString(boundary)}";
            webRequest.uploadHandler = uploadHandler;
            using var downloadHandler = new DownloadHandlerBuffer();
            webRequest.downloadHandler = downloadHandler;
            return await webRequest.SendAsync(parameters, cancellationToken);
        }

        #endregion POST

        #region PUT

        /// <summary>
        /// Rest PUT.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="jsonData">Data to be submitted.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PutAsync(
            string query,
            string jsonData,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
            using var webRequest = UnityWebRequest.Put(query, jsonData);
            webRequest.SetRequestHeader("Content-Type", "application/json");
            return await webRequest.SendAsync(parameters, cancellationToken);
        }

        /// <summary>
        /// Rest PUT.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="bodyData">Data to be submitted.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PutAsync(
            string query,
            byte[] bodyData,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
            using var webRequest = UnityWebRequest.Put(query, bodyData);
            webRequest.SetRequestHeader("Content-Type", "application/octet-stream");
            return await webRequest.SendAsync(parameters, cancellationToken);
        }

        #endregion PUT

        #region PATCH

        /// <summary>
        /// Rest PATCH.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="jsonData">Data to be submitted.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PatchAsync(
            string query,
            string jsonData,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
            using var webRequest = UnityWebRequest.Put(query, jsonData);
            webRequest.method = kHttpVerbPATCH;
            webRequest.SetRequestHeader("Content-Type", "application/json");
            return await webRequest.SendAsync(parameters, cancellationToken);
        }

        /// <summary>
        /// Rest PATCH.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="bodyData">Data to be submitted.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PatchAsync(
            string query,
            byte[] bodyData,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
            using var webRequest = UnityWebRequest.Put(query, bodyData);
            webRequest.method = kHttpVerbPATCH;
            webRequest.SetRequestHeader("Content-Type", "application/octet-stream");
            return await webRequest.SendAsync(parameters, cancellationToken);
        }

        #endregion PATCH

        #region DELETE

        /// <summary>
        /// Rest DELETE.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> DeleteAsync(
            string query,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
            using var webRequest = UnityWebRequest.Delete(query);
            using var downloadHandler = new DownloadHandlerBuffer();
            webRequest.downloadHandler = downloadHandler;
            return await webRequest.SendAsync(parameters, cancellationToken);
        }

        #endregion DELETE

        #region Get Multimedia Content

        #region Download Cache

        private const string download_cache = nameof(download_cache);

        private static IDownloadCache cache;

        private static IDownloadCache Cache
            => cache ??= Application.platform switch
            {
                RuntimePlatform.WebGLPlayer => new NoOpDownloadCache(),
                _ => new DiskDownloadCache()
            };

        /// <summary>
        /// The download cache directory.<br/>
        /// </summary>
        public static string DownloadCacheDirectory
            => Path.Combine(Application.temporaryCachePath, download_cache);

        /// <summary>
        /// Creates the <see cref="DownloadCacheDirectory"/> if it doesn't exist.
        /// </summary>
        public static void ValidateCacheDirectory()
            => Cache.ValidateCacheDirectory();

        /// <summary>
        /// Creates the <see cref="DownloadCacheDirectory"/> if it doesn't exist.
        /// </summary>
        public static Task ValidateCacheDirectoryAsync()
            => Cache.ValidateCacheDirectoryAsync();

        /// <summary>
        /// Try to get a file out of the download cache by uri reference.
        /// </summary>
        /// <param name="uri">The uri key of the item.</param>
        /// <param name="filePath">The file path to the cached item.</param>
        /// <returns>True, if the item was in cache, otherwise false.</returns>
        public static bool TryGetDownloadCacheItem(string uri, out string filePath)
            => Cache.TryGetDownloadCacheItem(uri, out filePath);

        /// <summary>
        /// Try to delete the cached item at the uri.
        /// </summary>
        /// <param name="uri">The uri key of the item.</param>
        /// <returns>True, if the cached item was successfully deleted.</returns>
        public static bool TryDeleteCacheItem(string uri)
            => Cache.TryDeleteCacheItem(uri);

        /// <summary>
        /// Deletes all the files in the download cache.
        /// </summary>
        public static void DeleteDownloadCache()
            => Cache.DeleteDownloadCache();

        /// <summary>
        /// We will try go guess the name based on the url.
        /// </summary>
        /// <param name="url">The url to parse to try to guess file name.</param>
        /// <param name="fileName">The filename if found.</param>
        /// <returns>True, if a valid filename is found.</returns>
        public static bool TryGetFileNameFromUrl(string url, out string fileName)
        {
            var baseUrl = UnityWebRequest.UnEscapeURL(url);
            var rootUrl = baseUrl.Split('?')[0];
            var index = rootUrl.LastIndexOf('/') + 1;
            fileName = rootUrl.Substring(index, rootUrl.Length - index);
            return Path.HasExtension(fileName);
        }

        #endregion Download Cache

        [Obsolete("use new overload with debug support")]
        public static async Task<Texture2D> DownloadTextureAsync(
            string url,
            string fileName = null,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
            return await DownloadTextureAsync(url, fileName, parameters, false, cancellationToken);
        }

        /// <summary>
        /// Download a <see cref="Texture2D"/> from the provided <see cref="url"/>.
        /// </summary>
        /// <param name="url">The url to download the <see cref="Texture2D"/> from.</param>
        /// <param name="fileName">Optional, file name to download (including extension).</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="debug">Optional, debug http request.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>A new <see cref="Texture2D"/> instance.</returns>
        public static async Task<Texture2D> DownloadTextureAsync(
        string url,
        string fileName = null,
        RestParameters parameters = null,
        bool debug = false,
        CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;

            if (string.IsNullOrWhiteSpace(fileName))
            {
                TryGetFileNameFromUrl(url, out fileName);
            }

            bool isCached;
            string cachePath;

            if (url.Contains(FileUriPrefix))
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

            Texture2D texture;
            parameters ??= new RestParameters();
            parameters.DisposeDownloadHandler = true;
            using var webRequest = UnityWebRequestTexture.GetTexture(url);

            try
            {
                var response = await webRequest.SendAsync(parameters, cancellationToken);
                response.Validate(debug);

                if (!isCached)
                {
                    await Cache.WriteCacheItemAsync(webRequest.downloadHandler.data, cachePath, cancellationToken).ConfigureAwait(true);
                }

                texture = ((DownloadHandlerTexture)webRequest.downloadHandler).texture;

                if (texture == null)
                {
                    throw new RestException(response, $"Failed to load texture from \"{url}\"!");
                }
            }
            finally
            {
                webRequest.downloadHandler?.Dispose();
            }

            texture.name = Path.GetFileNameWithoutExtension(cachePath);
            return texture;
        }

        [Obsolete("Use new overload with debug support")]
        public static async Task<AudioClip> DownloadAudioClipAsync(
            string url,
            AudioType audioType,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
            return await DownloadAudioClipAsync(url, audioType, httpMethod: UnityWebRequest.kHttpVerbGET, parameters: parameters, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Download a <see cref="AudioClip"/> from the provided <see cref="url"/>.
        /// </summary>
        /// <param name="url">The url to download the <see cref="AudioClip"/> from.</param>
        /// <param name="audioType"><see cref="AudioType"/> to download.</param>
        /// <param name="fileName">Optional, file name to download (including extension).</param>
        /// <param name="httpMethod">Optional, must be either GET or POST.</param>
        /// <param name="jsonData">Optional, json payload. Only <see cref="jsonData"/> OR <see cref="payload"/> can be supplied.</param>
        /// <param name="payload">Optional, raw byte payload. Only <see cref="payload"/> OR <see cref="jsonData"/> can be supplied.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="debug">Optional, debug http request.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>A new <see cref="AudioClip"/> instance.</returns>
        public static async Task<AudioClip> DownloadAudioClipAsync(
            string url,
            AudioType audioType,
            string httpMethod = UnityWebRequest.kHttpVerbGET,
            string fileName = null,
            string jsonData = null,
            byte[] payload = null,
            RestParameters parameters = null,
            bool debug = false,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;

            if (string.IsNullOrWhiteSpace(fileName))
            {
                TryGetFileNameFromUrl(url, out fileName);
            }

            bool isCached;
            string cachePath;

            if (url.Contains(FileUriPrefix))
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

            UploadHandler uploadHandler = null;
            using var downloadHandler = new DownloadHandlerAudioClip(url, audioType);

            if (httpMethod == UnityWebRequest.kHttpVerbPOST)
            {
                if (!string.IsNullOrWhiteSpace(jsonData))
                {
                    if (payload != null)
                    {
                        throw new ArgumentException($"{nameof(payload)} and {nameof(jsonData)} cannot be supplied in the same request. Choose either one or the other.", nameof(jsonData));
                    }

                    payload = new UTF8Encoding().GetBytes(jsonData);

                    var jsonHeaders = new Dictionary<string, string>
                    {
                        { "Content-Type", "application/json" }
                    };

                    if (parameters is { Headers: not null })
                    {
                        foreach (var header in parameters.Headers)
                        {
                            jsonHeaders.Add(header.Key, header.Value);
                        }
                    }

                    if (parameters != null)
                    {
                        parameters.Headers = jsonHeaders;
                    }
                }

                uploadHandler = new UploadHandlerRaw(payload);
            }

            AudioClip clip;
            parameters ??= new RestParameters();
            parameters.DisposeUploadHandler = false;
            parameters.DisposeDownloadHandler = false;
            using var webRequest = new UnityWebRequest(url, httpMethod, downloadHandler, uploadHandler);

            try
            {
                var response = await webRequest.SendAsync(parameters, cancellationToken);
                response.Validate(debug);

                if (!isCached)
                {
                    await Cache.WriteCacheItemAsync(downloadHandler.data, cachePath, cancellationToken);
                }

                await Awaiters.UnityMainThread;
                clip = downloadHandler.audioClip;

                if (clip == null)
                {
                    throw new RestException(response, $"Failed to download audio clip from \"{url}\"!");
                }
            }
            finally
            {
                downloadHandler.Dispose();
                uploadHandler?.Dispose();
            }

            clip.name = Path.GetFileNameWithoutExtension(cachePath);
            return clip;
        }

        /// <summary>
        /// Stream a <see cref="AudioClip"/> from the provided <see cref="url"/>.
        /// </summary>
        /// <param name="url">The url to download the <see cref="AudioClip"/> from.</param>
        /// <param name="audioType"><see cref="AudioType"/> to download.</param>
        /// <param name="onStreamPlaybackReady"><see cref="Action{T}"/> callback raised when stream is ready to be played.</param>
        /// <param name="httpMethod">Optional, must be either GET or POST.</param>
        /// <param name="jsonData">Optional, json payload. Only <see cref="jsonData"/> OR <see cref="payload"/> can be supplied.</param>
        /// <param name="payload">Optional, raw byte payload. Only <see cref="payload"/> OR <see cref="jsonData"/> can be supplied.</param>
        /// <param name="fileName">Optional, file name to download (including extension).</param>
        /// <param name="playbackAmountThreshold">Optional, the amount of data to to download before signaling that streaming is ready.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="debug">Optional, debug http request.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>A new <see cref="AudioClip"/> instance.</returns>
        public static async Task<AudioClip> StreamAudioAsync(
            string url,
            AudioType audioType,
            Action<AudioClip> onStreamPlaybackReady,
            string httpMethod = UnityWebRequest.kHttpVerbPOST,
            string fileName = null,
            string jsonData = null,
            byte[] payload = null,
            ulong playbackAmountThreshold = 10000,
            RestParameters parameters = null,
            bool debug = false,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;

            if (string.IsNullOrWhiteSpace(fileName))
            {
                TryGetFileNameFromUrl(url, out fileName);
            }

            if (url.Contains(FileUriPrefix))
            {
                // override the httpMethod
                httpMethod = UnityWebRequest.kHttpVerbGET;
            }

            AudioClip clip = null;
            var streamStarted = false;
            UploadHandler uploadHandler = null;

            if (httpMethod == UnityWebRequest.kHttpVerbPOST)
            {
                if (!string.IsNullOrWhiteSpace(jsonData))
                {
                    if (payload != null)
                    {
                        throw new ArgumentException($"{nameof(payload)} and {nameof(jsonData)} cannot be supplied in the same request. Choose either one or the other.", nameof(jsonData));
                    }

                    payload = new UTF8Encoding().GetBytes(jsonData);

                    var jsonHeaders = new Dictionary<string, string>
                    {
                        { "Content-Type", "application/json" }
                    };

                    if (parameters is { Headers: not null })
                    {
                        foreach (var header in parameters.Headers)
                        {
                            jsonHeaders.Add(header.Key, header.Value);
                        }
                    }

                    if (parameters != null)
                    {
                        parameters.Headers = jsonHeaders;
                    }
                }

                uploadHandler = new UploadHandlerRaw(payload);
            }

            parameters ??= new RestParameters();
            parameters.DisposeUploadHandler = false;
            parameters.DisposeDownloadHandler = false;
            using var downloadHandler = new DownloadHandlerAudioClip(url, audioType);
            downloadHandler.streamAudio = true; // BUG: Due to a Unity bug this does not work with mp3s of indeterminate length. https://forum.unity.com/threads/downloadhandleraudioclip-streamaudio-is-ignored.699908/
            using var webRequest = new UnityWebRequest(url, httpMethod, downloadHandler, uploadHandler);
            IProgress<Progress> progress = null;

            if (parameters.Progress != null)
            {
                progress = parameters.Progress;
            }

            parameters.Progress = new Progress<Progress>(report =>
            {
                progress?.Report(report);

                // only raise stream ready if we haven't assigned a clip yet.
                if (clip != null) { return; }

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

            var response = await webRequest.SendAsync(parameters, cancellationToken);
            uploadHandler?.Dispose();
            response.Validate(debug);

            var loadedClip = downloadHandler.audioClip;

            if (loadedClip != null)
            {
                loadedClip.name = Path.GetFileNameWithoutExtension(fileName);
            }

            if (!streamStarted)
            {
                streamStarted = true;
                onStreamPlaybackReady?.Invoke(loadedClip);
            }

            return loadedClip;
        }

#if UNITY_ADDRESSABLES

        /// <summary>
        /// Download a <see cref="AssetBundle"/> from the provided <see cref="url"/>.
        /// </summary>
        /// <param name="url">The url to download the <see cref="AssetBundle"/> from.</param>
        /// <param name="options">Asset bundle request options.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>A new <see cref="AssetBundle"/> instance.</returns>
        public static async Task<AssetBundle> DownloadAssetBundleAsync(
            string url,
            UnityEngine.ResourceManagement.ResourceProviders.AssetBundleRequestOptions options,
            RestParameters parameters = null,
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
                    parameters ??= new RestParameters();
                    parameters.Timeout = options?.Timeout ?? -1;
                    parameters.DisposeDownloadHandler = false;
                    response = await webRequest.SendAsync(parameters, cancellationToken);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    throw;
                }

                if (!response.Successful)
                {
                    Debug.LogError($"Failed to download asset bundle from \"{url}\"!\n{response.Code}:{response.Body}");
                    return null;
                }

                var downloadHandler = (DownloadHandlerAssetBundle)webRequest.downloadHandler;
                var assetBundle = downloadHandler.assetBundle;
                downloadHandler.Dispose();
                return assetBundle;
            }
        }

#endif // UNITY_ADDRESSABLES

        [Obsolete("use new overload with debug support")]
        public static async Task<string> DownloadFileAsync(
            string url,
            string fileName = null,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
            return await DownloadFileAsync(url, fileName, parameters, false, cancellationToken);
        }

        /// <summary>
        /// Download a file from the provided <see cref="url"/>.
        /// </summary>
        /// <param name="url">The url to download the file from.</param>
        /// <param name="fileName">Optional, file name to download (including extension).</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="debug">Optional, debug http request.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The path to the downloaded file.</returns>
        public static async Task<string> DownloadFileAsync(
        string url,
        string fileName = null,
        RestParameters parameters = null,
        bool debug = false,
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
            using var fileDownloadHandler = new DownloadHandlerFile(filePath);
            fileDownloadHandler.removeFileOnAbort = true;
            webRequest.downloadHandler = fileDownloadHandler;
            var response = await webRequest.SendAsync(parameters, cancellationToken);
            response.Validate(debug);
            return filePath;
        }

        /// <summary>
        /// Download a file from the provided <see cref="url"/> and return the <see cref="byte[]"/> array of its contents.
        /// </summary>
        /// <param name="url">The url to download the file from.</param>
        /// <param name="fileName">Optional, file name to download (including extension).</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="debug">Optional, debug http request.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="byte[]"/> of the downloaded file.</returns>
        public static async Task<byte[]> DownloadFileBytesAsync(
        string url,
        string fileName = null,
        RestParameters parameters = null,
        bool debug = false,
        CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;
            byte[] bytes = null;

            var filePath = await DownloadFileAsync(url, fileName, parameters, false, cancellationToken);
            var absolutefilePath = filePath.Replace("file://", string.Empty);
            if (File.Exists(absolutefilePath))
            {
                try
                {
                    bytes = File.ReadAllBytes(absolutefilePath);
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                    throw;
                }
            }

            return bytes;
        }

        /// <summary>
        /// Download a <see cref="byte[]"/> from the provided <see cref="url"/>.
        /// </summary>
        /// <param name="url">The url to download from.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="debug">Optional, debug http request.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="byte[]"/> downloaded from the server.</returns>
        /// <remarks>This call cannot be cached, if you want the cached version of a <see cref="byte[]"/>, then use the <see cref="DownloadFileBytesAsync"/> function.</remarks>
        public static async Task<byte[]> DownloadBytesAsync(
        string url,
        RestParameters parameters = null,
        bool debug = false,
        CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;

            using var webRequest = UnityWebRequest.Get(url);
            using var downloadHandlerBuffer = new DownloadHandlerBuffer();
            webRequest.downloadHandler = downloadHandlerBuffer;
            var response = await webRequest.SendAsync(parameters, cancellationToken);
            response.Validate(debug);
            return response.Data;
        }
        #endregion Get Multimedia Content

        /// <summary>
        /// Process a <see cref="UnityWebRequest"/> asynchronously.
        /// </summary>
        /// <param name="webRequest">The <see cref="UnityWebRequest"/>.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
        /// <returns><see cref="Response"/></returns>
        public static async Task<Response> SendAsync(
            this UnityWebRequest webRequest,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
            => await SendAsync(webRequest, parameters, null, cancellationToken);

        /// <summary>
        /// Process a <see cref="UnityWebRequest"/> asynchronously.
        /// </summary>
        /// <param name="webRequest">The <see cref="UnityWebRequest"/>.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="serverSentEventCallback">Optional, <see cref="Action{T}"/> server sent event callback.</param>
        /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
        /// <returns><see cref="Response"/></returns>
        public static async Task<Response> SendAsync(
            this UnityWebRequest webRequest,
            RestParameters parameters = null,
            Action<string> serverSentEventCallback = null,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;

            if (parameters is { Timeout: > 0 })
            {
                webRequest.timeout = parameters.Timeout;
            }

            if (parameters is { Headers: not null })
            {
                foreach (var header in parameters.Headers)
                {
                    webRequest.SetRequestHeader(header.Key, header.Value);
                }
            }

            var hasUpload = webRequest.method is
                UnityWebRequest.kHttpVerbPOST or
                UnityWebRequest.kHttpVerbPUT or
                kHttpVerbPATCH;

            // HACK: Workaround for extra quotes around boundary.
            if (hasUpload)
            {
                const string CONTENT_TYPE = "Content-Type";
                var contentType = webRequest.GetRequestHeader(CONTENT_TYPE);

                if (!string.IsNullOrWhiteSpace(contentType))
                {
                    contentType = contentType.Replace("\"", string.Empty);
                    webRequest.SetRequestHeader(CONTENT_TYPE, contentType);
                }
            }

            webRequest.certificateHandler = parameters?.CertificateHandler;
            webRequest.disposeCertificateHandlerOnDispose = parameters?.DisposeCertificateHandler ?? true;
            webRequest.disposeDownloadHandlerOnDispose = parameters?.DisposeDownloadHandler ?? true;
            webRequest.disposeUploadHandlerOnDispose = parameters?.DisposeUploadHandler ?? true;

            if (parameters is { Progress: not null } ||
                serverSentEventCallback != null)
            {
                async void CallbackThread()
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
                            if (serverSentEventCallback != null)
                            {
                                SendServerEventCallback(false);
                            }

                            if (parameters is { Progress: not null })
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
                                var percentage = hasUpload && webRequest.uploadProgress > 1f
                                    ? webRequest.uploadProgress
                                    : webRequest.downloadProgress;
                                // Get the content length of the download
                                const string CONTENT_LENGTH = "Content-Length";

                                if (!ulong.TryParse(webRequest.GetResponseHeader(CONTENT_LENGTH), out var length))
                                {
                                    length = webRequest.downloadedBytes;
                                }

                                // Report the progress using the progress handler provided by the caller
                                parameters.Progress.Report(new Progress(webRequest.downloadedBytes, length, percentage * 100f, speed, unit));
                            }

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

#pragma warning disable CS4014
                Task.Run(CallbackThread, cancellationToken);
#pragma warning restore CS4014
            }

            try
            {
                await webRequest.SendWebRequest();
            }
            catch (Exception e)
            {
                return new Response(webRequest.url, webRequest.method, false, $"{nameof(Rest)}.{nameof(SendAsync)}::{nameof(UnityWebRequest.SendWebRequest)} Failed!", null, -1, null, e.ToString());
            }

            parameters?.Progress?.Report(new Progress(webRequest.downloadedBytes, webRequest.downloadedBytes, 100f, 0, Progress.DataUnit.b));
            var responseHeaders = webRequest.GetResponseHeaders() ?? new Dictionary<string, string> { { "Invalid Headers", "Invalid Headers" } };

            if (webRequest.result is
                UnityWebRequest.Result.ConnectionError or
                UnityWebRequest.Result.ProtocolError &&
                webRequest.responseCode is 0 or >= 400)
            {
                return webRequest.downloadHandler switch
                {
                    DownloadHandlerFile => new Response(webRequest.url, webRequest.method, false, null, null, webRequest.responseCode, responseHeaders, $"{webRequest.error}\n{webRequest.downloadHandler?.error}"),
                    DownloadHandlerTexture => new Response(webRequest.url, webRequest.method, false, null, null, webRequest.responseCode, responseHeaders, $"{webRequest.error}\n{webRequest.downloadHandler?.error}"),
                    DownloadHandlerAudioClip => new Response(webRequest.url, webRequest.method, false, null, null, webRequest.responseCode, responseHeaders, $"{webRequest.error}\n{webRequest.downloadHandler?.error}"),
                    DownloadHandlerAssetBundle => new Response(webRequest.url, webRequest.method, false, null, null, webRequest.responseCode, responseHeaders, $"{webRequest.error}\n{webRequest.downloadHandler?.error}"),
                    DownloadHandlerBuffer bufferDownloadHandler => new Response(webRequest.url, webRequest.method, false, bufferDownloadHandler.text, bufferDownloadHandler.data, webRequest.responseCode, responseHeaders, $"{webRequest.error}\n{webRequest.downloadHandler?.error}"),
                    DownloadHandlerScript scriptDownloadHandler => new Response(webRequest.url, webRequest.method, false, scriptDownloadHandler.text, scriptDownloadHandler.data, webRequest.responseCode, responseHeaders, $"{webRequest.error}\n{webRequest.downloadHandler?.error}"),
                    _ => new Response(webRequest.url, webRequest.method, false, webRequest.responseCode == 401 ? "Invalid Credentials" : webRequest.downloadHandler?.text, webRequest.downloadHandler?.data, webRequest.responseCode, responseHeaders, $"{webRequest.error}\n{webRequest.downloadHandler?.error}")
                };
            }

            if (serverSentEventCallback != null)
            {
                SendServerEventCallback(true);
            }

            return webRequest.downloadHandler switch
            {
                DownloadHandlerFile => new Response(webRequest.url, webRequest.method, true, null, null, webRequest.responseCode, responseHeaders),
                DownloadHandlerTexture => new Response(webRequest.url, webRequest.method, true, null, null, webRequest.responseCode, responseHeaders),
                DownloadHandlerAudioClip => new Response(webRequest.url, webRequest.method, true, null, null, webRequest.responseCode, responseHeaders),
                DownloadHandlerAssetBundle => new Response(webRequest.url, webRequest.method, true, null, null, webRequest.responseCode, responseHeaders),
                DownloadHandlerBuffer bufferDownloadHandler => new Response(webRequest.url, webRequest.method, true, bufferDownloadHandler.text, bufferDownloadHandler.data, webRequest.responseCode, responseHeaders),
                DownloadHandlerScript scriptDownloadHandler => new Response(webRequest.url, webRequest.method, true, scriptDownloadHandler.text, scriptDownloadHandler.data, webRequest.responseCode, responseHeaders),
                _ => new Response(webRequest.url, webRequest.method, true, webRequest.downloadHandler?.text, webRequest.downloadHandler?.data, webRequest.responseCode, responseHeaders)
            };

            void SendServerEventCallback(bool isEnd)
            {
                parameters ??= new RestParameters();
                var lines = webRequest.downloadHandler?.text
                    .Split(eventDelimiter, StringSplitOptions.RemoveEmptyEntries)
                    .ToArray();

                if (lines is { Length: > 1 })
                {
                    var stride = isEnd ? 0 : 1;
                    for (var i = parameters.ServerSentEventCount; i < lines.Length - stride; i++)
                    {
                        var line = lines[i];

                        if (!line.Contains(stopEventDelimiter))
                        {
                            parameters.ServerSentEventCount++;
                            serverSentEventCallback.Invoke(line);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validates the <see cref="Response"/> and will throw a <see cref="RestException"/> if the response is unsuccessful.
        /// </summary>
        /// <param name="response"><see cref="Response"/>.</param>
        /// <param name="debug">Print debug information of <see cref="Response"/>.</param>
        /// <param name="methodName">Optional, <see cref="CallerMemberNameAttribute"/>.</param>
        /// <exception cref="RestException"></exception>
        public static void Validate(this Response response, bool debug = false, [CallerMemberName] string methodName = null)
        {
            if (!response.Successful)
            {
                throw new RestException(response);
            }

            if (debug)
            {
                Debug.Log(response.ToString(methodName));
            }
        }
    }
}
