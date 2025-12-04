// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Scripting;
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
        private const char Space = ' ';
        private const char Bom = '\uFEFF';
        private const char NewLine = '\n';
        private const char Return = '\r';

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
        public static Task<Response> GetAsync(
            string query,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
            => GetAsync(new Uri(query), parameters, cancellationToken);

        /// <summary>
        /// Rest GET.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> GetAsync(
            Uri query,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;
            using var webRequest = UnityWebRequest.Get(query);
            return await webRequest.SendAsync(parameters, cancellationToken);
        }

        /// <summary>
        /// Rest GET.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="serverSentEventHandler"><see cref="Action{Response, ServerSentEvent}"/> server sent event callback handler.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static Task<Response> GetAsync(
            string query,
            Func<Response, ServerSentEvent, Task> serverSentEventHandler,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
            => GetAsync(new Uri(query), serverSentEventHandler, parameters, cancellationToken);

        /// <summary>
        /// Rest GET.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="serverSentEventHandler"><see cref="Action{Response, ServerSentEvent}"/> server sent event callback handler.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> GetAsync(
            Uri query,
            Func<Response, ServerSentEvent, Task> serverSentEventHandler,
            RestParameters? parameters = null,
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
        /// <param name="dataReceivedEventCallback"><see cref="Action{T}"/> data received event callback.</param>
        /// <param name="eventChunkSize"></param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static Task<Response> GetAsync(
            string query,
            Action<Response> dataReceivedEventCallback,
            int? eventChunkSize = null,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
            => GetAsync(new Uri(query), dataReceivedEventCallback, eventChunkSize, parameters, cancellationToken);

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
            Uri query,
            Action<Response> dataReceivedEventCallback,
            int? eventChunkSize = null,
            RestParameters? parameters = null,
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
        public static Task<Response> PostAsync(
            string query,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
            => PostAsync(new Uri(query), parameters, cancellationToken);

        /// <summary>
        /// Rest POST.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PostAsync(
            Uri query,
            RestParameters? parameters = null,
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
        public static Task<Response> PostAsync(
            string query,
            WWWForm formData,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
            => PostAsync(new Uri(query), formData, parameters, cancellationToken);

        /// <summary>
        /// Rest POST.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="formData">Form Data.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PostAsync(
            Uri query,
            WWWForm formData,
            RestParameters? parameters = null,
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
        public static Task<Response> PostAsync(
            string query,
            string jsonData,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
            => PostAsync(new Uri(query), jsonData, parameters, cancellationToken);

        /// <summary>
        /// Rest POST.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="jsonData">JSON data for the request.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PostAsync(
            Uri query,
            string jsonData,
            RestParameters? parameters = null,
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

        /// <summary>
        /// Rest POST.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="jsonData">JSON data for the request.</param>
        /// <param name="serverSentEventHandler"><see cref="Func{Response, ServerSentEvent, Task}"/> server sent event callback handler.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static Task<Response> PostAsync(
            string query,
            string jsonData,
            Func<Response, ServerSentEvent, Task> serverSentEventHandler,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
            => PostAsync(new Uri(query), jsonData, serverSentEventHandler, parameters, cancellationToken);

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
            Uri query,
            string jsonData,
            Func<Response, ServerSentEvent, Task> serverSentEventHandler,
            RestParameters? parameters = null,
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
        /// <param name="dataReceivedEventCallback"><see cref="Action{T}"/> data received event callback.</param>
        /// <param name="eventChunkSize">Optional, <see cref="dataReceivedEventCallback"/> event chunk size in bytes (Defaults to 512 bytes).</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static Task<Response> PostAsync(
            string query,
            string jsonData,
            Action<Response> dataReceivedEventCallback,
            int? eventChunkSize = null,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
            => PostAsync(new Uri(query), jsonData, dataReceivedEventCallback, eventChunkSize, parameters, cancellationToken);

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
            Uri query,
            string jsonData,
            Action<Response> dataReceivedEventCallback,
            int? eventChunkSize = null,
            RestParameters? parameters = null,
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
        public static Task<Response> PostAsync(
            string query,
            byte[] bodyData,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
            => PostAsync(new Uri(query), bodyData, parameters, cancellationToken);

        /// <summary>
        /// Rest POST.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="bodyData">The raw data to post.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PostAsync(
            Uri query,
            byte[] bodyData,
            RestParameters? parameters = null,
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
        public static Task<Response> PostAsync(
            string query,
            List<IMultipartFormSection> form,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
            => PostAsync(new Uri(query), form, parameters, cancellationToken);

        /// <summary>
        /// Rest POST.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="form">The <see cref="IMultipartFormSection"/> to post.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PostAsync(
            Uri query,
            List<IMultipartFormSection> form,
            RestParameters? parameters = null,
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
        public static Task<Response> PutAsync(
            string query,
            string jsonData,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
            => PutAsync(new Uri(query), jsonData, parameters, cancellationToken);

        /// <summary>
        /// Rest PUT.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="jsonData">Data to be submitted.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PutAsync(
            Uri query,
            string jsonData,
            RestParameters? parameters = null,
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
        public static Task<Response> PutAsync(
            string query,
            byte[] bodyData,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
            => PutAsync(new Uri(query), bodyData, parameters, cancellationToken);

        /// <summary>
        /// Rest PUT.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="bodyData">Data to be submitted.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PutAsync(
            Uri query,
            byte[] bodyData,
            RestParameters? parameters = null,
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
        public static Task<Response> PatchAsync(
            string query,
            string jsonData,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
            => PatchAsync(new Uri(query), jsonData, parameters, cancellationToken);

        /// <summary>
        /// Rest PATCH.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="jsonData">Data to be submitted.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PatchAsync(
            Uri query,
            string jsonData,
            RestParameters? parameters = null,
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
        public static Task<Response> PatchAsync(
            string query,
            byte[] bodyData,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
            => PatchAsync(new Uri(query), bodyData, parameters, cancellationToken);

        /// <summary>
        /// Rest PATCH.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="bodyData">Data to be submitted.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> PatchAsync(
            Uri query,
            byte[] bodyData,
            RestParameters? parameters = null,
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
        public static Task<Response> DeleteAsync(
            string query,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
            => DeleteAsync(new Uri(query), parameters, cancellationToken);

        /// <summary>
        /// Rest DELETE.
        /// </summary>
        /// <param name="query">Finalized Endpoint Query with parameters.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The response data.</returns>
        public static async Task<Response> DeleteAsync(
            Uri query,
            RestParameters? parameters = null,
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

        // ReSharper disable once InconsistentNaming
        private const string download_cache = nameof(download_cache);

        private static IDownloadCache cache;

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
#endif
        private static void Init_Rest()
        {
            downloadLocation = Application.temporaryCachePath;
            cache = Application.platform == RuntimePlatform.WebGLPlayer
                    ? new NoOpDownloadCache()
                    : new DiskDownloadCache();
        }

        /// <summary>
        /// Enable/Disable the download cache for all requests.
        /// </summary>
        /// <remarks>
        /// WebGL cache is always disabled.
        /// </remarks>
        public static bool CacheEnabled
        {
            get => cache.GetType() == typeof(NoOpDownloadCache);
            set
            {
                if (!value || Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    cache = new NoOpDownloadCache();
                }
                else
                {
                    cache = new DiskDownloadCache();
                }
            }
        }

        private static readonly HashSet<string> allowedDownloadLocations = new()
        {
            Application.temporaryCachePath,
            Application.persistentDataPath,
            Application.dataPath,
            Application.streamingAssetsPath
        };

        private static string downloadLocation;

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
            => cache.ValidateCacheDirectory();

        /// <summary>
        /// Creates the <see cref="DownloadCacheDirectory"/> if it doesn't exist.
        /// </summary>
        public static Task ValidateCacheDirectoryAsync()
            => cache.ValidateCacheDirectoryAsync();

        /// <summary>
        /// Try to get a file out of the download cache by uri reference.
        /// </summary>
        /// <param name="uri">The uri key of the item.</param>
        /// <param name="filePath">The file path to the cached item.</param>
        /// <returns>True, if the item was in cache, otherwise false.</returns>
        public static bool TryGetDownloadCacheItem(string uri, out string filePath)
        {
            var result = TryGetDownloadCacheItem(new Uri(uri), out var local);
            filePath = local.LocalPath;
            return result;
        }

        /// <summary>
        /// Try to get a file out of the download cache by uri reference.
        /// </summary>
        /// <param name="uri">The uri key of the item.</param>
        /// <param name="filePath">The file path to the cached item.</param>
        /// <returns>True, if the item was in cache, otherwise false.</returns>
        public static bool TryGetDownloadCacheItem(Uri uri, out Uri filePath)
            => cache.TryGetDownloadCacheItem(uri, out filePath);

        /// <summary>
        /// Try to delete the cached item at the uri.
        /// </summary>
        /// <param name="uri">The uri key of the item.</param>
        /// <returns>True, if the cached item was successfully deleted.</returns>
        public static bool TryDeleteCacheItem(string uri)
            => TryDeleteCacheItem(new Uri(uri));

        /// <summary>
        /// Try to delete the cached item at the uri.
        /// </summary>
        /// <param name="uri">The uri key of the item.</param>
        /// <returns>True, if the cached item was successfully deleted.</returns>
        public static bool TryDeleteCacheItem(Uri uri)
            => cache.TryDeleteCacheItem(uri);

        /// <summary>
        /// Deletes all the files in the download cache.
        /// </summary>
        public static void DeleteDownloadCache()
            => cache.DeleteDownloadCache();

        /// <summary>
        /// We will try go guess the name based on the url.
        /// </summary>
        /// <param name="url">The url to parse to try to guess file name.</param>
        /// <param name="fileName">The filename if found.</param>
        /// <returns>True, if a valid filename is found from the url.</returns>
        /// <remarks>
        /// Url must start with "http" and the last segment must have a file extension, or it will return false.
        /// </remarks>
        [Obsolete("use TryGetFileNameFromUri")]
        public static bool TryGetFileNameFromUrl(string url, out string fileName)
        {
            fileName = null;
            const string http = nameof(http);
            if (!url.StartsWith(http)) { return false; }
            var baseUrl = UnityWebRequest.UnEscapeURL(url);
            var rootUrl = baseUrl.Split('?')[0];
            var index = rootUrl.LastIndexOf('/') + 1;
            fileName = rootUrl.Substring(index, rootUrl.Length - index);
            return Path.HasExtension(fileName);
        }

        /// <summary>
        /// We will try go guess the name based on the uri.
        /// </summary>
        /// <param name="uri">The url to parse to try to guess file name.</param>
        /// <param name="fileName">The filename if found.</param>
        /// <returns>True, if a valid filename is found from the uri.</returns>
        /// <remarks>
        /// Uri must be a remote resource and the last segment must have a file extension, or it will return false.
        /// </remarks>
        public static bool TryGetFileNameFromUri(Uri uri, out string fileName)
        {
            fileName = null;
            if (uri.IsFile) { return false; }
            var baseUrl = UnityWebRequest.UnEscapeURL(uri.ToString());
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
        public static Task<Texture2D> DownloadTextureAsync(
            string url,
            string fileName,
            RestParameters? parameters,
            CancellationToken cancellationToken = default)
            => DownloadTextureAsync(new Uri(url), fileName, true, parameters, cancellationToken);

        /// <summary>
        /// Download a <see cref="Texture2D"/> from the provided <see cref="url"/>.
        /// </summary>
        /// <param name="url">The url to download the <see cref="Texture2D"/> from.</param>
        /// <param name="fileName">Optional, file name to download (including extension).</param>
        /// <param name="readable">Optional, Enables the texture's raw data will be accessible to script.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>A new <see cref="Texture2D"/> instance.</returns>
        public static async Task<Texture2D> DownloadTextureAsync(
            string url,
            string fileName = null,
            bool readable = true,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
            => await DownloadTextureAsync(
                new Uri(url),
                fileName,
                readable,
                parameters,
                cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Download a <see cref="Texture2D"/> from the provided <see cref="uri"/>.
        /// </summary>
        /// <param name="uri">The uri to download the <see cref="Texture2D"/> from.</param>
        /// <param name="fileName">Optional, file name to download (including extension).</param>
        /// <param name="readable">Optional, Enables the texture's raw data will be accessible to script.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>A new <see cref="Texture2D"/> instance.</returns>
        public static async Task<Texture2D> DownloadTextureAsync(
            Uri uri,
            string fileName = null,
            bool readable = true,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;

            bool isCached;
            string cachePath;
            var restParams = parameters.Clone(disposeDownloadHandler: true);

            if (uri.IsFile)
            {
                isCached = true;
                cachePath = uri.AbsolutePath;
            }
            else
            {
                if (restParams.CacheDownloads &&
                    string.IsNullOrWhiteSpace(fileName))
                {
                    TryGetFileNameFromUri(uri, out fileName);
                }

                isCached = TryGetDownloadCacheItem(fileName, out cachePath) && restParams.CacheDownloads;
            }

            if (isCached)
            {
                uri = new Uri(cachePath);
            }

            Texture2D texture;
            using var webRequest = UnityWebRequestTexture.GetTexture(uri, nonReadable: !readable); // inverted logic

            try
            {
                var response = await webRequest.SendAsync(restParams, cancellationToken);
                response.Validate(restParams.Debug);

                if (!isCached && restParams.CacheDownloads)
                {
                    await cache.WriteCacheItemAsync(webRequest.downloadHandler.data, cachePath, cancellationToken).ConfigureAwait(true);
                }

                texture = ((DownloadHandlerTexture)webRequest.downloadHandler).texture;

                if (texture == null)
                {
                    throw new RestException(response, $"Failed to load texture from \"{uri}\"!");
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
        public static Task<AudioClip> DownloadAudioClipAsync(
            string url,
            AudioType audioType,
            string httpMethod = UnityWebRequest.kHttpVerbGET,
            string fileName = null,
            string jsonData = null,
            byte[] payload = null,
            bool compressed = false,
            bool streamingAudio = false,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
            => DownloadAudioClipAsync(
                new Uri(url),
                audioType,
                httpMethod,
                fileName,
                jsonData,
                payload,
                compressed,
                streamingAudio,
                parameters,
                cancellationToken);

        /// <summary>
        /// Download a <see cref="AudioClip"/> from the provided <see cref="uri"/>.
        /// </summary>
        /// <param name="uri">The uri to download the <see cref="AudioClip"/> from.</param>
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
            Uri uri,
            AudioType audioType,
            string httpMethod = UnityWebRequest.kHttpVerbGET,
            string fileName = null,
            string jsonData = null,
            byte[] payload = null,
            bool compressed = false,
            bool streamingAudio = false,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;

            bool isCached;
            Uri cachePath;
            var restParams = parameters.Clone();

            if (uri.IsFile)
            {
                isCached = true;
                cachePath = uri;
            }
            else
            {
                if (restParams.CacheDownloads &&
                    string.IsNullOrWhiteSpace(fileName))
                {
                    TryGetFileNameFromUri(uri, out fileName);
                }

                isCached = TryGetDownloadCacheItem(fileName, out cachePath) && restParams.CacheDownloads;
            }

            if (isCached)
            {
                uri = cachePath;
            }

            UploadHandler uploadHandler = null;
            using var downloadHandler = new DownloadHandlerAudioClip(uri, audioType);
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

                    foreach (var header in restParams.Headers)
                    {
                        jsonHeaders.Add(header.Key, header.Value);
                    }

                    restParams = restParams.Clone(headers: jsonHeaders);
                }

                uploadHandler = new UploadHandlerRaw(payload);
            }

            AudioClip clip;
            restParams = restParams.Clone(disposeUploadHandler: false, disposeDownloadHandler: false);
            using var webRequest = new UnityWebRequest(uri, httpMethod, downloadHandler, uploadHandler);

            try
            {
                var response = await webRequest.SendAsync(restParams, cancellationToken);
                response.Validate(restParams.Debug);

                if (!isCached && restParams.CacheDownloads)
                {
                    await cache.WriteCacheItemAsync(downloadHandler.data, cachePath.LocalPath, cancellationToken);
                }

                await Awaiters.UnityMainThread;
                clip = downloadHandler.audioClip;

                if (clip == null)
                {
                    throw new RestException(response, $"Failed to download audio clip from \"{uri}\"!");
                }
            }
            finally
            {
                uploadHandler?.Dispose();
            }

            clip.name = Path.GetFileNameWithoutExtension(cachePath.LocalPath);
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
        /// <param name="playbackAmountThreshold">Optional, the amount of data to download before signaling that streaming is ready.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>A new <see cref="AudioClip"/> instance.</returns>
        public static Task<AudioClip> StreamAudioAsync(
            string url,
            AudioType audioType,
            Action<AudioClip> onStreamPlaybackReady,
            string httpMethod = UnityWebRequest.kHttpVerbPOST,
            string fileName = null,
            string jsonData = null,
            byte[] payload = null,
            ulong playbackAmountThreshold = 10000,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
            => StreamAudioAsync(
                new Uri(url),
                audioType,
                onStreamPlaybackReady,
                httpMethod,
                fileName,
                jsonData,
                payload,
                playbackAmountThreshold,
                parameters,
                cancellationToken);

        /// <summary>
        /// Stream a <see cref="AudioClip"/> from the provided <see cref="uri"/>.
        /// </summary>
        /// <param name="uri">The url to download the <see cref="AudioClip"/> from.</param>
        /// <param name="audioType"><see cref="AudioType"/> to download.</param>
        /// <param name="onStreamPlaybackReady"><see cref="Action{T}"/> callback raised when stream is ready to be played.</param>
        /// <param name="httpMethod">Optional, must be either GET or POST.</param>
        /// <param name="jsonData">Optional, json payload. Only <see cref="jsonData"/> OR <see cref="payload"/> can be supplied.</param>
        /// <param name="payload">Optional, raw byte payload. Only <see cref="payload"/> OR <see cref="jsonData"/> can be supplied.</param>
        /// <param name="fileName">Optional, file name to download (including extension).</param>
        /// <param name="playbackAmountThreshold">Optional, the amount of data to download before signaling that streaming is ready.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>A new <see cref="AudioClip"/> instance.</returns>
        public static async Task<AudioClip> StreamAudioAsync(
            Uri uri,
            AudioType audioType,
            Action<AudioClip> onStreamPlaybackReady,
            string httpMethod = UnityWebRequest.kHttpVerbPOST,
            string fileName = null,
            string jsonData = null,
            byte[] payload = null,
            ulong playbackAmountThreshold = 10000,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;

            if (string.IsNullOrWhiteSpace(fileName) &&
                !TryGetFileNameFromUri(uri, out fileName))
            {
                fileName = uri.IsFile
                    ? Path.GetFileName(uri.LocalPath)
                    : uri.GenerateGuidString();
            }

            if (uri.IsFile)
            {
                // override the httpMethod
                httpMethod = UnityWebRequest.kHttpVerbGET;
            }

            AudioClip clip = null;
            var streamStarted = false;
            UploadHandler uploadHandler = null;
            var restParams = parameters.Clone();

            if (httpMethod == UnityWebRequest.kHttpVerbPOST)
            {
                if (!string.IsNullOrWhiteSpace(jsonData))
                {
                    if (payload != null)
                    {
                        throw new ArgumentException($"{nameof(payload)} and {nameof(jsonData)} cannot be supplied in the same request. Choose either one or the other.", nameof(jsonData));
                    }

                    payload = new UTF8Encoding().GetBytes(jsonData!);

                    var jsonHeaders = new Dictionary<string, string> { { content_type, application_json } };

                    if (restParams.Headers != null)
                    {
                        foreach (var (key, value) in restParams.Headers)
                        {
                            jsonHeaders.Add(key, value);
                        }
                    }

                    restParams = restParams.Clone(headers: jsonHeaders);
                }

                uploadHandler = new UploadHandlerRaw(payload);
            }

            restParams = restParams.Clone(disposeDownloadHandler: false, disposeUploadHandler: false);
            using var downloadHandler = new DownloadHandlerAudioClip(uri, audioType);
            downloadHandler.streamAudio = true; // BUG: Due to a Unity bug this does not work with mp3s of indeterminate length. https://forum.unity.com/threads/downloadhandleraudioclip-streamaudio-is-ignored.699908/
            using var webRequest = new UnityWebRequest(uri, httpMethod, downloadHandler, uploadHandler);
            IProgress<Progress> progress = null;

            if (restParams.Progress != null)
            {
                progress = restParams.Progress;
            }

            var progressCallback = new Progress<Progress>(report =>
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
            restParams = restParams.Clone(progress: progressCallback);
            var response = await webRequest.SendAsync(restParams, cancellationToken);
            uploadHandler?.Dispose();
            response.Validate(restParams.Debug);

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
        public static Task<AssetBundle> DownloadAssetBundleAsync(
            string url,
            UnityEngine.ResourceManagement.ResourceProviders.AssetBundleRequestOptions options,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
            => DownloadAssetBundleAsync(
                new Uri(url),
                options,
                parameters,
                cancellationToken);

        /// <summary>
        /// Download a <see cref="AssetBundle"/> from the provided <see cref="uri"/>.
        /// </summary>
        /// <param name="uri">The uri to download the <see cref="AssetBundle"/> from.</param>
        /// <param name="options">Asset bundle request options.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>A new <see cref="AssetBundle"/> instance.</returns>
        public static async Task<AssetBundle> DownloadAssetBundleAsync(
            Uri uri,
            UnityEngine.ResourceManagement.ResourceProviders.AssetBundleRequestOptions options,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;

            UnityWebRequest webRequest;

            if (options == null)
            {
                webRequest = UnityWebRequestAssetBundle.GetAssetBundle(uri);
            }
            else
            {
                if (!string.IsNullOrEmpty(options.Hash))
                {
                    CachedAssetBundle cachedBundle = new CachedAssetBundle(options.BundleName, Hash128.Parse(options.Hash));
#if ENABLE_CACHING
                    if (options.UseCrcForCachedBundle || !Caching.IsVersionCached(cachedBundle))
                    {
                        webRequest = UnityWebRequestAssetBundle.GetAssetBundle(uri, cachedBundle, options.Crc);
                    }
                    else
                    {
                        webRequest = UnityWebRequestAssetBundle.GetAssetBundle(uri, cachedBundle);
                    }
#else
                    webRequest = UnityWebRequestAssetBundle.GetAssetBundle(uri, cachedBundle, options.Crc);
#endif
                }
                else
                {
                    webRequest = UnityWebRequestAssetBundle.GetAssetBundle(uri, options.Crc);
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
                var restParams = parameters.Clone(disposeDownloadHandler: false, timeout: options?.Timeout);
                var response = await webRequest.SendAsync(restParams, cancellationToken);
                response.Validate(restParams.Debug);

                using var downloadHandler = (DownloadHandlerAssetBundle)webRequest.downloadHandler;
                var assetBundle = downloadHandler.assetBundle;

                if (assetBundle == null)
                {
                    throw new RestException(response, $"Failed to download asset bundle from \"{uri}\"!");
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
        public static Task<string> DownloadFileAsync(
            string url,
            string fileName = null,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
            => DownloadFileAsync(
                new Uri(url),
                fileName,
                parameters,
                cancellationToken);

        /// <summary>
        /// Download a file from the provided <see cref="uri"/>.
        /// </summary>
        /// <param name="uri">The uri to download the file from.</param>
        /// <param name="fileName">Optional, file name to download (including extension).</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The path to the downloaded file.</returns>
        public static async Task<string> DownloadFileAsync(
            Uri uri,
            string fileName = null,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;
            var restParams = parameters.Clone();

            if (string.IsNullOrWhiteSpace(fileName) && restParams.CacheDownloads)
            {
                TryGetFileNameFromUri(uri, out fileName);
            }

            if (TryGetDownloadCacheItem(fileName, out var filePath) && restParams.CacheDownloads)
            {
                return filePath;
            }

            using var webRequest = UnityWebRequest.Get(uri);
            using var fileDownloadHandler = new DownloadHandlerFile(filePath);
            fileDownloadHandler.removeFileOnAbort = true;
            webRequest.downloadHandler = fileDownloadHandler;
            var response = await webRequest.SendAsync(restParams, cancellationToken);
            response.Validate(restParams.Debug);
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
        public static Task<byte[]> DownloadFileBytesAsync(
            string url,
            string fileName = null,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
            => DownloadFileBytesAsync(
                new Uri(url),
                fileName,
                parameters,
                cancellationToken);

        /// <summary>
        /// Download a file from the provided <see cref="uri"/> and return the contents as bytes.
        /// </summary>
        /// <param name="uri">The uri to download the file from.</param>
        /// <param name="fileName">Optional, file name to download (including extension).</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The bytes of the downloaded file.</returns>
        public static async Task<byte[]> DownloadFileBytesAsync(
            Uri uri,
            string fileName = null,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;
            byte[] bytes = null;
            var filePath = await DownloadFileAsync(uri, fileName, parameters, cancellationToken);
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
        public static Task<byte[]> DownloadBytesAsync(
            string url,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
            => DownloadBytesAsync(
                new Uri(url),
                parameters,
                cancellationToken);

        /// <summary>
        /// Download raw file contents from the provided <see cref="uri"/>.
        /// </summary>
        /// <param name="uri">The uri to download from.</param>
        /// <param name="parameters">Optional, <see cref="RestParameters"/>.</param>
        /// <param name="cancellationToken">Optional, <see cref="CancellationToken"/>.</param>
        /// <returns>The bytes downloaded from the server.</returns>
        /// <remarks>This request does not cache results.</remarks>
        public static async Task<byte[]> DownloadBytesAsync(
            Uri uri,
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;
            using var webRequest = UnityWebRequest.Get(uri);
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
            RestParameters? parameters = null,
            CancellationToken cancellationToken = default)
            => await SendAsync(webRequest, parameters, serverSentEventHandler: null, cancellationToken);

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
            RestParameters? parameters = null,
            Func<Response, ServerSentEvent, Task> serverSentEventHandler = null,
            CancellationToken cancellationToken = default)
        {
            await Awaiters.UnityMainThread;
            var restParams = parameters.Clone();

            if (restParams.Timeout > 0)
            {
                webRequest.timeout = restParams.Timeout;
            }

            if (restParams.Headers != null)
            {
                foreach (var header in restParams.Headers)
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

            webRequest.certificateHandler = restParams.CertificateHandler;
            webRequest.disposeCertificateHandlerOnDispose = restParams.DisposeCertificateHandler;
            webRequest.disposeDownloadHandlerOnDispose = restParams.DisposeDownloadHandler;
            webRequest.disposeUploadHandlerOnDispose = restParams.DisposeUploadHandler;

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
                                formParts.Add(Tuple.Create(key, fileName));
                            }
                            else
                            {
                                var value = formFields[1];
                                formParts.Add(Tuple.Create(key, value));
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

            var serverSentEventCharacterIndex = 0;
            var serverSentEventQueue = new Queue<ServerSentEventPayload>();
            CancellationTokenSource serverSentEventCts = null;

            if (restParams.Progress != null || serverSentEventHandler != null)
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

                            if (restParams.Progress != null)
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
                                restParams.Progress.Report(new Progress(webRequest.downloadedBytes, length, percentage * 100f, speed, unit));
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

                            if (serverSentEventQueue.TryDequeue(out var payload))
                            {
                                await serverSentEventHandler.Invoke(payload.Response, payload.Event);
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                    } while (!serverSentEventCts.Token.IsCancellationRequested);
                }
#pragma warning disable CS4014 // We purposefully don't await this task, so it will run on a background thread.
                // ReSharper disable PossiblyMistakenUseOfCancellationToken
                Task.Run(CallbackThread, cancellationToken);

                if (serverSentEventHandler != null)
                {
                    Task.Run(ServerSentEventQueue, cancellationToken);
                }
                // ReSharper restore PossiblyMistakenUseOfCancellationToken
#pragma warning restore CS4014
            }

            try
            {
                await webRequest.SendWebRequest();
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case TaskCanceledException:
                    case OperationCanceledException:
                        throw;
                    default:
                        return new Response(webRequest.url, webRequest.method, requestBody, false, $"{nameof(Rest)}.{nameof(SendAsync)}::{nameof(UnityWebRequest.SendWebRequest)} Failed!", null, -1, null, restParams, e.ToString());
                }
            }
            finally
            {
                restParams.Progress?.Report(new Progress(webRequest.downloadedBytes, webRequest.downloadedBytes, 100f, 0, Progress.DataUnit.b));

                if (serverSentEventHandler != null)
                {
                    EnqueueServerSentEventCallbacks();
                }

                if (serverSentEventCts != null)
                {
                    try
                    {
                        while (serverSentEventQueue.Count > 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            await Task.Yield();
                        }
                    }
                    finally
                    {
                        serverSentEventCts?.Cancel();
                        serverSentEventCts?.Dispose();
                        serverSentEventCts = null;
                    }
                }
            }

            if (webRequest.result is
                UnityWebRequest.Result.ConnectionError or
                UnityWebRequest.Result.ProtocolError &&
                webRequest.responseCode is 0 or >= 400)
            {
                return new Response(webRequest, requestBody, false, restParams);
            }

            return new Response(webRequest, requestBody, true, restParams);

            void EnqueueServerSentEventCallbacks()
            {
                var allEventMessages = webRequest.downloadHandler?.text;
                if (string.IsNullOrWhiteSpace(allEventMessages)) { return; }

                var textLength = allEventMessages.Length;
                var processedIndex = serverSentEventCharacterIndex;

                if (processedIndex < 0)
                {
                    processedIndex = 0;
                    serverSentEventCharacterIndex = 0;
                }
                else if (processedIndex > textLength)
                {
                    Debug.LogWarning($"[{nameof(Rest)}] SSE index {processedIndex} exceeded buffer length {textLength}. Resetting tracked position to avoid corrupt parsing.");
                    processedIndex = 0;
                    serverSentEventCharacterIndex = 0;
                }

                var currentIndex = processedIndex;

                while (currentIndex < textLength)
                {
                    var eventStart = currentIndex;
                    var eventKind = ServerSentEventKind.Comment;
                    StringBuilder dataBuilder = null;
                    var typeAssigned = false;
                    var value = string.Empty;
                    // ReSharper disable once JoinDeclarationAndInitializer
                    string data;

                    // Read lines until a blank line (event boundary) or end of input
                    while (true)
                    {
                        if (!TryReadLine(allEventMessages, textLength, ref currentIndex, out var line))
                        {
                            serverSentEventCharacterIndex = eventStart;
                            return;
                        }

                        if (line.Length == 0)
                        {
                            // Blank line: event boundary
                            break;
                        }

                        var colonIndex = line.IndexOf(':');

                        if (colonIndex < 0) { continue; }

                        var fieldNameSpan = Trim(line[..colonIndex]);
                        var fieldName = fieldNameSpan.Length == 0 ? string.Empty : fieldNameSpan.ToString();
                        var isCommentLine = colonIndex == 0 && fieldNameSpan.Length == 0;
                        var fieldValueSpan = TrimSseValue(line[(colonIndex + 1)..]);
                        var fieldValue = fieldValueSpan.Length == 0 ? string.Empty : new string(fieldValueSpan);

                        if (!typeAssigned)
                        {
                            eventKind = isCommentLine
                                ? ServerSentEventKind.Comment
                                : ServerSentEvent.EventMap.GetValueOrDefault(fieldName, ServerSentEventKind.Comment);

                            value = fieldValue;
                            typeAssigned = true;

                            if (string.Equals(fieldName, nameof(data), StringComparison.OrdinalIgnoreCase))
                            {
                                AppendData(ref dataBuilder, fieldValue);
                            }

                            continue;
                        }

                        if (isCommentLine)
                        {
                            continue;
                        }

                        if (string.Equals(fieldName, nameof(data), StringComparison.OrdinalIgnoreCase))
                        {
                            AppendData(ref dataBuilder, fieldValue);
                        }
                    }

                    serverSentEventCharacterIndex = currentIndex;

                    if (!typeAssigned)
                    {
                        continue;
                    }

                    data = dataBuilder?.ToString();

                    const string doneTag = "[DONE]";
                    const string doneEvent = "done";

                    if (string.Equals(value, doneTag, StringComparison.Ordinal) ||
                        string.Equals(value, doneEvent, StringComparison.Ordinal) ||
                        string.Equals(data, doneTag, StringComparison.Ordinal))
                    {
                        return;
                    }

                    var @event = new ServerSentEvent(eventKind, value, data);
                    var sseResponse = new Response(webRequest, requestBody, true, restParams, (@event.Data ?? @event.Value).ToString(Formatting.None));
                    serverSentEventQueue.Enqueue(new ServerSentEventPayload(sseResponse, @event));
                    restParams.ServerSentEvents.Add(@event);
                }

                static bool TryReadLine(string source, int length, ref int position, out ReadOnlySpan<char> line)
                {
                    if (position >= length)
                    {
                        line = ReadOnlySpan<char>.Empty;
                        return false;
                    }

                    var slice = source.AsSpan(position, length - position);
                    var newlineIndex = slice.IndexOf(NewLine);

                    if (newlineIndex < 0)
                    {
                        line = ReadOnlySpan<char>.Empty;
                        return false;
                    }

                    line = slice[..newlineIndex];
                    position += newlineIndex + 1;

                    if (line.Length > 0 && line[^1] == Return)
                    {
                        line = line[..^1];
                    }

                    return true;
                }

                static ReadOnlySpan<char> Trim(ReadOnlySpan<char> span)
                {
                    var start = 0;
                    var end = span.Length - 1;

                    while (start <= end && char.IsWhiteSpace(span[start]))
                    {
                        start++;
                    }

                    while (end >= start && char.IsWhiteSpace(span[end]))
                    {
                        end--;
                    }

                    return start > end ? ReadOnlySpan<char>.Empty : span[start..(end + 1)];
                }

                static ReadOnlySpan<char> TrimSseValue(ReadOnlySpan<char> span)
                {
                    if (!span.IsEmpty && span[0] == Space)
                    {
                        span = span[1..];
                    }

                    if (!span.IsEmpty && span[0] == Bom)
                    {
                        span = span[1..];
                    }

                    return span;
                }

                static void AppendData(ref StringBuilder builder, string chunk)
                {
                    const int defaultStringBuilderPadding = 16; // extra padding to reduce allocations
                    builder ??= new StringBuilder((chunk?.Length ?? 0) + defaultStringBuilderPadding);

                    if (builder.Length > 0)
                    {
                        builder.Append(NewLine);
                    }

                    if (!string.IsNullOrEmpty(chunk))
                    {
                        builder.Append(chunk);
                    }
                }
            }
        }

        private readonly struct ServerSentEventPayload
        {
            public ServerSentEventPayload(Response response, ServerSentEvent @event)
            {
                Response = response;
                Event = @event;
            }

            public Response Response { get; }
            public ServerSentEvent Event { get; }
        }

        /// <summary>
        /// Validates the <see cref="Response"/> and will throw a <see cref="RestException"/> if the response is unsuccessful.
        /// </summary>
        /// <param name="response"><see cref="Response"/>.</param>
        /// <param name="debug">Print debug information of <see cref="Response"/>.</param>
        /// <param name="methodName">Optional, <see cref="CallerMemberNameAttribute"/>.</param>
        /// <exception cref="RestException"></exception>
        [Preserve]
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

        [Preserve]
        public static RestParameters Clone(this RestParameters? other,
            IReadOnlyDictionary<string, string> headers = null,
            IProgress<Progress> progress = null,
            int? timeout = null,
            bool? disposeDownloadHandler = null,
            bool? disposeUploadHandler = null,
            CertificateHandler certificateHandler = null,
            bool? disposeCertificateHandler = null,
            bool? cacheDownloads = null,
            bool? debug = null)
            => RestParameters.Clone(
                other,
                headers,
                progress,
                timeout,
                disposeDownloadHandler,
                disposeUploadHandler,
                certificateHandler,
                disposeCertificateHandler,
                cacheDownloads,
                debug);

        [Preserve]
        public static RestParameters Clone(this RestParameters other,
            IReadOnlyDictionary<string, string> headers = null,
            IProgress<Progress> progress = null,
            int? timeout = null,
            bool? disposeDownloadHandler = null,
            bool? disposeUploadHandler = null,
            CertificateHandler certificateHandler = null,
            bool? disposeCertificateHandler = null,
            bool? cacheDownloads = null,
            bool? debug = null)
            => RestParameters.Clone(
                other,
                headers,
                progress,
                timeout,
                disposeDownloadHandler,
                disposeUploadHandler,
                certificateHandler,
                disposeCertificateHandler,
                cacheDownloads,
                debug);
    }
}
