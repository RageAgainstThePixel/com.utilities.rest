// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
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
        private const string content_type = "Content-Type";
        private const string content_length = "Content-Length";
        private const string application_json = "application/json";
        private const string multipart_form_data = "multipart/form-data";
        private const string application_octet_stream = "application/octet-stream";
        private const string ssePattern = @"(?:(?:(?<type>[^:\n]*):)(?<value>(?:(?!\n\n|\ndata:).)*)(?:\ndata:(?<data>(?:(?!\n\n).)*))?\n\n)";

        private static readonly Regex sseRegex = new(ssePattern);

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
            await Awaiters.UnityMainThread;
            using var webRequest = UnityWebRequest.Get(query);
            return await webRequest.SendAsync(parameters, cancellationToken);
        }

        [Obsolete("use new overload with serverSentEventHandler: Func<Response, ServerSentEvent, Task>")]
        public static async Task<Response> GetAsync(
            string query,
            Action<Response, ServerSentEvent> serverSentEventHandler,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;
            using var webRequest = UnityWebRequest.Get(query);
            return await webRequest.SendAsync(parameters, serverSentEventHandler, cancellationToken);
        }

        /// <summary>
        /// Rest GET.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="serverSentEventHandler"><see cref="Action{Response, ServerSentEvent}"/> server sent event callback handler.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> GetAsync(
            string query,
            Func<Response, ServerSentEvent, Task> serverSentEventHandler,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;
            using var webRequest = UnityWebRequest.Get(query);
            return await webRequest.SendAsync(parameters, serverSentEventHandler, cancellationToken);
        }

        [Obsolete("use new overload with serverSentEventHandler")]
        public static async Task<Response> GetAsync(
            string query,
            Action<string> serverSentEventCallback,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;
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
            await Awaiters.UnityMainThread;
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
                downloadHandler.Complete();
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
            await Awaiters.UnityMainThread;
            using var webRequest = new UnityWebRequest(query, UnityWebRequest.kHttpVerbPOST);
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
            await Awaiters.UnityMainThread;
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
            await Awaiters.UnityMainThread;
            using var webRequest = new UnityWebRequest(query, UnityWebRequest.kHttpVerbPOST);
            var data = new UTF8Encoding().GetBytes(jsonData);
            using var uploadHandler = new UploadHandlerRaw(data);
            webRequest.uploadHandler = uploadHandler;
            using var downloadHandler = new DownloadHandlerBuffer();
            webRequest.downloadHandler = downloadHandler;
            webRequest.SetRequestHeader(content_type, application_json);
            return await webRequest.SendAsync(parameters, cancellationToken);
        }

        [Obsolete("Use new overload with serverSentEventHandler: Func<Response, ServerSentEvent, Task>")]
        public static async Task<Response> PostAsync(
            string query,
            string jsonData,
            Action<Response, ServerSentEvent> serverSentEventHandler,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;
            using var webRequest = new UnityWebRequest(query, UnityWebRequest.kHttpVerbPOST);
            var data = new UTF8Encoding().GetBytes(jsonData);
            using var uploadHandler = new UploadHandlerRaw(data);
            webRequest.uploadHandler = uploadHandler;
            using var downloadHandler = new DownloadHandlerBuffer();
            webRequest.downloadHandler = downloadHandler;
            webRequest.SetRequestHeader(content_type, application_json);
            return await webRequest.SendAsync(parameters, serverSentEventHandler, cancellationToken);
        }

        /// <summary>
        /// Rest POST.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="jsonData">JSON data for the request.</param>
        /// <param name="serverSentEventHandler"><see cref="Func{Response, ServerSentEvent, Task}"/> server sent event callback handler.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PostAsync(
            string query,
            string jsonData,
            Func<Response, ServerSentEvent, Task> serverSentEventHandler,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;
            using var webRequest = new UnityWebRequest(query, UnityWebRequest.kHttpVerbPOST);
            var data = new UTF8Encoding().GetBytes(jsonData);
            using var uploadHandler = new UploadHandlerRaw(data);
            webRequest.uploadHandler = uploadHandler;
            using var downloadHandler = new DownloadHandlerBuffer();
            webRequest.downloadHandler = downloadHandler;
            webRequest.SetRequestHeader(content_type, application_json);
            return await webRequest.SendAsync(parameters, serverSentEventHandler, cancellationToken);
        }

        [Obsolete("use new overload with serverSentEventHandler")]
        public static async Task<Response> PostAsync(
            string query,
            string jsonData,
            Action<string> serverSentEventCallback,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;
            using var webRequest = new UnityWebRequest(query, UnityWebRequest.kHttpVerbPOST);
            var data = new UTF8Encoding().GetBytes(jsonData);
            using var uploadHandler = new UploadHandlerRaw(data);
            webRequest.uploadHandler = uploadHandler;
            using var downloadHandler = new DownloadHandlerBuffer();
            webRequest.downloadHandler = downloadHandler;
            webRequest.SetRequestHeader(content_type, application_json);
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
            await Awaiters.UnityMainThread;
            using var webRequest = new UnityWebRequest(query, UnityWebRequest.kHttpVerbPOST);
            var data = new UTF8Encoding().GetBytes(jsonData);
            using var uploadHandler = new UploadHandlerRaw(data);
            webRequest.uploadHandler = uploadHandler;
            using var downloadHandler = eventChunkSize.HasValue
                ? new DownloadHandlerCallback(webRequest, eventChunkSize.Value)
                : new DownloadHandlerCallback(webRequest);
            downloadHandler.OnDataReceived += dataReceivedEventCallback;
            webRequest.downloadHandler = downloadHandler;
            webRequest.SetRequestHeader(content_type, application_json);

            try
            {
                return await webRequest.SendAsync(parameters, serverSentEventHandler: null, cancellationToken);
            }
            finally
            {
                downloadHandler.Complete();
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
            await Awaiters.UnityMainThread;
            using var webRequest = new UnityWebRequest(query, UnityWebRequest.kHttpVerbPOST);
            using var uploadHandler = new UploadHandlerRaw(bodyData);
            webRequest.uploadHandler = uploadHandler;
            using var downloadHandler = new DownloadHandlerBuffer();
            webRequest.downloadHandler = downloadHandler;
            webRequest.SetRequestHeader(content_type, application_octet_stream);
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
            await Awaiters.UnityMainThread;
            using var webRequest = new UnityWebRequest(query, UnityWebRequest.kHttpVerbPOST);
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
            await Awaiters.UnityMainThread;
            using var webRequest = UnityWebRequest.Put(query, jsonData);
            webRequest.SetRequestHeader(content_type, application_json);
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
            await Awaiters.UnityMainThread;
            using var webRequest = UnityWebRequest.Put(query, bodyData);
            webRequest.SetRequestHeader(content_type, application_octet_stream);
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
            await Awaiters.UnityMainThread;
            using var webRequest = UnityWebRequest.Put(query, jsonData);
            webRequest.method = kHttpVerbPATCH;
            webRequest.SetRequestHeader(content_type, application_json);
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
            await Awaiters.UnityMainThread;
            using var webRequest = UnityWebRequest.Put(query, bodyData);
            webRequest.method = kHttpVerbPATCH;
            webRequest.SetRequestHeader(content_type, application_octet_stream);
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
            await Awaiters.UnityMainThread;
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

        private static readonly List<string> allowedDownloadLocations = new()
        {
            Application.temporaryCachePath,
            Application.persistentDataPath,
            Application.dataPath,
            Application.streamingAssetsPath
        };

        private static string downloadLocation = Application.temporaryCachePath;

        /// <summary>
        /// The top level directory to create the <see cref="download_cache"/> directory.
        /// </summary>
        public static string DownloadLocation
        {
            get => downloadLocation;
            set
            {
                if (allowedDownloadLocations.Contains(value))
                {
                    if (downloadLocation == value) { return; }
                    var downloadCacheDirectory = Path.Combine(downloadLocation, download_cache);
                    if (Directory.Exists(downloadCacheDirectory)) { Directory.Delete(downloadCacheDirectory, true); }
                    downloadLocation = value;
                }
                else
                {
                    Debug.LogError($"Invalid Download location specified. Must be one of: {string.Join(", ", allowedDownloadLocations)}");
                }
            }
        }

        /// <summary>
        /// The download cache directory.<br/>
        /// </summary>
        /// <remarks>
        /// This directory is a subdirectory of the <see cref="DownloadLocation"/> named <see cref="download_cache"/>.<br/>
        /// </remarks>
        public static string DownloadCacheDirectory
        {
            get
            {
                var downloadCacheDirectory = Path.Combine(DownloadLocation, download_cache);
                if (!Directory.Exists(downloadCacheDirectory)) { Directory.CreateDirectory(downloadCacheDirectory); }
                return downloadCacheDirectory;
            }
        }

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

        /// <summary>
        /// Download a <see cref="Texture2D"/> from the provided <see cref="url"/>.
        /// </summary>
        /// <param name="url">The url to download the <see cref="Texture2D"/> from.</param>
        /// <param name="fileName">Optional, file name to download (including extension).</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>A new <see cref="Texture2D"/> instance.</returns>
        public static async Task<Texture2D> DownloadTextureAsync(
            string url,
            string fileName = null,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;

            if (string.IsNullOrWhiteSpace(fileName))
            {
                TryGetFileNameFromUrl(url, out fileName);
            }

            bool isCached;
            string cachePath;
            parameters ??= new RestParameters();

            if (url.Contains(FileUriPrefix))
            {
                isCached = true;
                cachePath = url;
            }
            else
            {
                isCached = TryGetDownloadCacheItem(fileName, out cachePath) && parameters.CacheDownloads;
            }

            if (isCached)
            {
                url = cachePath;
            }

            Texture2D texture;
            parameters.DisposeDownloadHandler = true;
            using var webRequest = UnityWebRequestTexture.GetTexture(url);

            try
            {
                var response = await webRequest.SendAsync(parameters, cancellationToken);
                response.Validate(parameters.Debug);

                if (!isCached && parameters.CacheDownloads)
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

        /// <summary>
        /// Download a <see cref="AudioClip"/> from the provided <see cref="url"/>.
        /// </summary>
        /// <param name="url">The url to download the <see cref="AudioClip"/> from.</param>
        /// <param name="audioType"><see cref="AudioType"/> to download.</param>
        /// <param name="fileName">Optional, file name to download (including extension).</param>
        /// <param name="httpMethod">Optional, must be either GET or POST.</param>
        /// <param name="jsonData">Optional, json payload. Only <see cref="jsonData"/> OR <see cref="payload"/> can be supplied.</param>
        /// <param name="payload">Optional, raw byte payload. Only <see cref="payload"/> OR <see cref="jsonData"/> can be supplied.</param>
        /// <param name="compressed">Optional, Create AudioClip that is compressed in memory.<br/>
        /// Note: When <see cref="streamingAudio"/> is true, it supersedes compression, and the download handler creates an AudioClip similar
        /// to an imported clip with the loadType AudioClipLoadType.Streaming.
        /// </param>
        /// <param name="streamingAudio">Optional, Create a streaming audio clip.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>A new <see cref="AudioClip"/> instance.</returns>
        public static async Task<AudioClip> DownloadAudioClipAsync(
            string url,
            AudioType audioType,
            string httpMethod = UnityWebRequest.kHttpVerbGET,
            string fileName = null,
            string jsonData = null,
            byte[] payload = null,
            bool compressed = false,
            bool streamingAudio = false,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;

            if (string.IsNullOrWhiteSpace(fileName))
            {
                TryGetFileNameFromUrl(url, out fileName);
            }

            bool isCached;
            string cachePath;
            parameters ??= new RestParameters();

            if (url.Contains(FileUriPrefix))
            {
                isCached = true;
                cachePath = url;
            }
            else
            {
                isCached = TryGetDownloadCacheItem(fileName, out cachePath) && parameters.CacheDownloads;
            }

            if (isCached)
            {
                url = cachePath;
            }

            UploadHandler uploadHandler = null;
            using var downloadHandler = new DownloadHandlerAudioClip(url, audioType);
            downloadHandler.compressed = compressed;
            downloadHandler.streamAudio = streamingAudio;

            if (httpMethod == UnityWebRequest.kHttpVerbPOST)
            {
                if (!string.IsNullOrWhiteSpace(jsonData))
                {
                    if (payload != null)
                    {
                        throw new ArgumentException($"{nameof(payload)} and {nameof(jsonData)} cannot be supplied in the same request. Choose either one or the other.", nameof(jsonData));
                    }

                    payload = new UTF8Encoding().GetBytes(jsonData!);

                    var jsonHeaders = new Dictionary<string, string>
                    {
                        { content_type, application_json }
                    };

                    foreach (var header in parameters.Headers)
                    {
                        jsonHeaders.Add(header.Key, header.Value);
                    }

                    parameters.Headers = jsonHeaders;
                }

                uploadHandler = new UploadHandlerRaw(payload);
            }

            AudioClip clip;
            parameters.DisposeUploadHandler = false;
            parameters.DisposeDownloadHandler = false;
            using var webRequest = new UnityWebRequest(url, httpMethod, downloadHandler, uploadHandler);

            try
            {
                var response = await webRequest.SendAsync(parameters, cancellationToken);
                response.Validate(parameters.Debug);

                if (!isCached && parameters.CacheDownloads)
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

                    payload = new UTF8Encoding().GetBytes(jsonData!);

                    var jsonHeaders = new Dictionary<string, string>
                    {
                        { content_type, application_json }
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
            response.Validate(parameters.Debug);

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
                parameters ??= new RestParameters();
                parameters.Timeout = options?.Timeout ?? -1;
                parameters.DisposeDownloadHandler = false;
                var response = await webRequest.SendAsync(parameters, cancellationToken);
                response.Validate(parameters.Debug);

                var downloadHandler = (DownloadHandlerAssetBundle)webRequest.downloadHandler;
                var assetBundle = downloadHandler.assetBundle;
                downloadHandler.Dispose();

                if (assetBundle == null)
                {
                    throw new RestException(response, $"Failed to download asset bundle from \"{url}\"!");
                }

                return assetBundle;
            }
        }

#endif // UNITY_ADDRESSABLES

        /// <summary>
        /// Download a file from the provided <see cref="url"/>.
        /// </summary>
        /// <param name="url">The url to download the file from.</param>
        /// <param name="fileName">Optional, file name to download (including extension).</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The path to the downloaded file.</returns>
        public static async Task<string> DownloadFileAsync(
            string url,
            string fileName = null,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;

            if (string.IsNullOrWhiteSpace(fileName))
            {
                TryGetFileNameFromUrl(url, out fileName);
            }

            if (TryGetDownloadCacheItem(fileName, out var filePath) &&
                (parameters?.CacheDownloads ?? true))
            {
                return filePath;
            }

            using var webRequest = UnityWebRequest.Get(url);
            using var fileDownloadHandler = new DownloadHandlerFile(filePath);
            fileDownloadHandler.removeFileOnAbort = true;
            webRequest.downloadHandler = fileDownloadHandler;
            var response = await webRequest.SendAsync(parameters, cancellationToken);
            response.Validate(parameters?.Debug ?? false);
            return filePath;
        }

        /// <summary>
        /// Download a file from the provided <see cref="url"/> and return the contents as bytes.
        /// </summary>
        /// <param name="url">The url to download the file from.</param>
        /// <param name="fileName">Optional, file name to download (including extension).</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The bytes of the downloaded file.</returns>
        public static async Task<byte[]> DownloadFileBytesAsync(
            string url,
            string fileName = null,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;
            byte[] bytes = null;
            var filePath = await DownloadFileAsync(url, fileName, parameters, cancellationToken);
            var localPath = filePath.Replace("file://", string.Empty);

            if (File.Exists(localPath))
            {
                bytes = await File.ReadAllBytesAsync(localPath, cancellationToken).ConfigureAwait(true);
            }

            return bytes;
        }

        /// <summary>
        /// Download raw file contents from the provided <see cref="url"/>.
        /// </summary>
        /// <param name="url">The url to download from.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The bytes downloaded from the server.</returns>
        /// <remarks>This request does not cache results.</remarks>
        public static async Task<byte[]> DownloadBytesAsync(
            string url,
            RestParameters parameters = null,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;
            using var webRequest = UnityWebRequest.Get(url);
            using var downloadHandlerBuffer = new DownloadHandlerBuffer();
            webRequest.downloadHandler = downloadHandlerBuffer;
            var response = await webRequest.SendAsync(parameters, cancellationToken);
            response.Validate(parameters?.Debug ?? false);
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
            => await SendAsync(webRequest, parameters, serverSentEventHandler: null, cancellationToken);

        [Obsolete("Use new overload with serverSentEventHandler")]
        public static async Task<Response> SendAsync(
            this UnityWebRequest webRequest,
            RestParameters parameters = null,
            Action<string> serverSentEventCallback = null,
            CancellationToken cancellationToken = default)
        {
            Action<Response, ServerSentEvent> serverSentEventHandler = null;

            if (serverSentEventCallback != null)
            {
                serverSentEventHandler = (_, @event) =>
                {
                    if (@event.Value != null)
                    {
                        serverSentEventCallback.Invoke(@event.Value.ToString(Formatting.None));
                    }
                };
            }

            return await SendAsync(webRequest, parameters, serverSentEventHandler, cancellationToken);
        }

        [Obsolete("use new .ctr with new serverSentEventHandler: Func<Response, ServerSentEvent, Task>")]
        public static async Task<Response> SendAsync(
            this UnityWebRequest webRequest,
            RestParameters parameters = null,
            Action<Response, ServerSentEvent> serverSentEventHandler = null,
            CancellationToken cancellationToken = default)
        {
            return await SendAsync(webRequest, parameters, (response, @event) =>
             {
                 serverSentEventHandler?.Invoke(response, @event);
                 return Task.CompletedTask;
             }, cancellationToken);
        }

        /// <summary>
        /// Process a <see cref="UnityWebRequest"/> asynchronously.
        /// </summary>
        /// <param name="webRequest">The <see cref="UnityWebRequest"/>.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="serverSentEventHandler">Optional, <see cref="Func{Response, ServerSentEvent, Task}"/> server sent event callback handler.</param>
        /// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
        /// <returns><see cref="Response"/></returns>
        public static async Task<Response> SendAsync(
            this UnityWebRequest webRequest,
            RestParameters parameters = null,
            Func<Response, ServerSentEvent, Task> serverSentEventHandler = null,
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
                var contentType = webRequest.GetRequestHeader(content_type);

                if (!string.IsNullOrWhiteSpace(contentType))
                {
                    contentType = contentType.Replace("\"", string.Empty);
                    webRequest.SetRequestHeader(content_type, contentType);
                }
            }

            webRequest.certificateHandler = parameters?.CertificateHandler;
            webRequest.disposeCertificateHandlerOnDispose = parameters?.DisposeCertificateHandler ?? true;
            webRequest.disposeDownloadHandlerOnDispose = parameters?.DisposeDownloadHandler ?? true;
            webRequest.disposeUploadHandlerOnDispose = parameters?.DisposeUploadHandler ?? true;

            var requestBody = string.Empty;

            if (hasUpload && webRequest.uploadHandler != null)
            {
                var contentType = webRequest.GetRequestHeader(content_type);

                if (webRequest.uploadHandler.data is { Length: > 0 })
                {
                    var encodedData = Encoding.UTF8.GetString(webRequest.uploadHandler.data);

                    if (contentType.Contains(multipart_form_data))
                    {
                        var boundary = contentType.Split(';')[1].Split('=')[1];
                        var formData = encodedData.Split(new[] { $"\r\n--{boundary}\r\n", $"\r\n--{boundary}--\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                        var formParts = new List<Tuple<string, string>>();

                        foreach (var form in formData)
                        {
                            const string eol = "\r\n\r\n";
                            var formFields = form.Split(new[] { eol }, StringSplitOptions.RemoveEmptyEntries);
                            var fieldHeader = formFields[0];
                            const string fieldName = "name=\"";
                            var key = fieldHeader.Split(new[] { fieldName }, StringSplitOptions.RemoveEmptyEntries)[1].Split('"')[0];

                            if (fieldHeader.Contains(application_octet_stream))
                            {
                                const string filename = "filename=\"";
                                var fileName = fieldHeader.Split(new[] { filename }, StringSplitOptions.RemoveEmptyEntries)[1].Split('"')[0];
                                formParts.Add(new Tuple<string, string>(key, fileName));
                            }
                            else
                            {
                                var value = formFields[1];
                                formParts.Add(new Tuple<string, string>(key, value));
                            }
                        }

                        requestBody = JsonConvert.SerializeObject(new { contentType, formParts });
                    }
                    else
                    {
                        requestBody = encodedData;
                    }
                }
            }

            var serverSentEventQueue = new Queue<Tuple<Response, ServerSentEvent>>();
            CancellationTokenSource serverSentEventCts = null;

            if (parameters is { Progress: not null } ||
                serverSentEventHandler != null)
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
                            if (serverSentEventHandler != null)
                            {
                                EnqueueServerSentEventCallbacks();
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
                                if (!ulong.TryParse(webRequest.GetResponseHeader(content_length), out var length))
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

                async void ServerSentEventQueue()
                {
                    serverSentEventCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    do
                    {
                        try
                        {
                            await Awaiters.UnityMainThread;

                            if (serverSentEventQueue.TryDequeue(out var @event))
                            {
                                var (sseResponse, ssEvent) = @event;
                                await serverSentEventHandler.Invoke(sseResponse, ssEvent);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                    } while (!serverSentEventCts.Token.IsCancellationRequested);
                }
#pragma warning disable CS4014 // We purposefully don't await this task, so it will run on a background thread.
                Task.Run(CallbackThread, cancellationToken);

                if (serverSentEventHandler != null)
                {
                    Task.Run(ServerSentEventQueue, cancellationToken);
                }
#pragma warning restore CS4014
            }

            try
            {
                await webRequest.SendWebRequest();
            }
            catch (Exception e)
            {
                return new Response(webRequest.url, webRequest.method, requestBody, false, $"{nameof(Rest)}.{nameof(SendAsync)}::{nameof(UnityWebRequest.SendWebRequest)} Failed!", null, -1, null, parameters, e.ToString());
            }
            finally
            {
                parameters?.Progress?.Report(new Progress(webRequest.downloadedBytes, webRequest.downloadedBytes, 100f, 0, Progress.DataUnit.b));

                if (serverSentEventHandler != null)
                {
                    EnqueueServerSentEventCallbacks();
                }

                if (serverSentEventCts != null)
                {
                    await new WaitUntil(() => serverSentEventQueue.Count == 0);
                    serverSentEventCts?.Cancel();
                }
            }

            if (webRequest.result is
                UnityWebRequest.Result.ConnectionError or
                UnityWebRequest.Result.ProtocolError &&
                webRequest.responseCode is 0 or >= 400)
            {
                return new Response(webRequest, requestBody, false, parameters);
            }

            return new Response(webRequest, requestBody, true, parameters);

            void EnqueueServerSentEventCallbacks()
            {
                var allEventMessages = webRequest.downloadHandler?.text;
                if (string.IsNullOrWhiteSpace(allEventMessages)) { return; }

                var matches = sseRegex.Matches(allEventMessages!);
                parameters ??= new RestParameters();
                var eventCount = parameters.ServerSentEventCount;

                for (var i = eventCount; i < matches.Count; i++)
                {
                    ServerSentEventKind type;
                    string value;
                    string data;

                    var match = matches[i];

                    // If the field type is not provided, treat it as a comment
                    type = ServerSentEvent.EventMap.GetValueOrDefault(match.Groups[nameof(type)].Value.Trim(), ServerSentEventKind.Comment);
                    // The UTF-8 decode algorithm strips one leading UTF-8 Byte Order Mark (BOM), if any.
                    value = match.Groups[nameof(value)].Value.TrimStart(' ');
                    data = match.Groups[nameof(data)].Value;

                    const string doneTag = "[DONE]";
                    const string doneEvent = "done";
                    // if either value or data equals doneTag or doneEvent then stop processing events.
                    if (value.Equals(doneTag) || data.Equals(doneTag) || value.Equals(doneEvent)) { return; }

                    var @event = new ServerSentEvent(type);

                    try
                    {
                        @event.Value = JToken.Parse(value);
                    }
                    catch
                    {
                        @event.Value = new JValue(value);
                    }

                    if (!string.IsNullOrWhiteSpace(data))
                    {
                        try
                        {
                            @event.Data = JToken.Parse(data);
                        }
                        catch
                        {
                            @event.Data = string.IsNullOrWhiteSpace(data) ? null : new JValue(value);
                        }
                    }
                    else
                    {
                        @event.Data = null;
                    }

                    var sseResponse = new Response(webRequest, requestBody, true, parameters, (@event.Data ?? @event.Value).ToString(Formatting.None));
                    serverSentEventQueue.Enqueue(Tuple.Create(sseResponse, @event));
                    parameters.ServerSentEventCount++;
                    parameters.ServerSentEvents.Add(@event);
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
