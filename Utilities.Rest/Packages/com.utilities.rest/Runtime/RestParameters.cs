// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.Scripting;

namespace Utilities.WebRequestRest
{
    /// <summary>
    /// A common class for restful parameters
    /// </summary>
    [Preserve]
    public readonly struct RestParameters
    {
        [Preserve]
        internal static RestParameters Clone(RestParameters? other,
            IReadOnlyDictionary<string, string> headers = null,
            IProgress<Progress> progress = null,
            int? timeout = null,
            bool? disposeDownloadHandler = null,
            bool? disposeUploadHandler = null,
            CertificateHandler certificateHandler = null,
            bool? disposeCertificateHandler = null,
            bool? cacheDownloads = null,
            bool? debug = null)
        {
            return new RestParameters(
                other?.ServerSentEvents,
                headers ?? other?.Headers,
                progress ?? other?.Progress,
                timeout ?? other?.Timeout ?? -1,
                disposeDownloadHandler ?? other?.DisposeDownloadHandler ?? true,
                disposeUploadHandler ?? other?.DisposeUploadHandler ?? true,
                certificateHandler ?? other?.CertificateHandler,
                disposeCertificateHandler ?? other?.DisposeCertificateHandler ?? true,
                cacheDownloads ?? other?.CacheDownloads ?? true,
                debug ?? other?.Debug ?? false);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="headers">Optional, header information for the request.</param>
        /// <param name="progress">Optional, <see cref="IProgress{T}"/> handler for the request.</param>
        /// <param name="timeout">Optional, time in seconds before the request expires.</param>
        /// <param name="disposeDownloadHandler">Optional, dispose the <see cref="DownloadHandler"/>?<br/>Default is true.</param>
        /// <param name="disposeUploadHandler">Optional, dispose the <see cref="UploadHandler"/>?<br/>Default is true.</param>
        /// <param name="certificateHandler">Optional, certificate handler for the request.</param>
        /// <param name="disposeCertificateHandler">Optional, dispose the <see cref="CertificateHandler"/>?<br/>Default is true.</param>
        /// <param name="cacheDownloads">Optional, cache downloaded content.<br/>Default is true.</param>
        /// <param name="debug">Optional, enable debug output of the request.<br/>Default is false.</param>
        public RestParameters(
            IReadOnlyDictionary<string, string> headers = null,
            IProgress<Progress> progress = null,
            int timeout = -1,
            bool disposeDownloadHandler = true,
            bool disposeUploadHandler = true,
            CertificateHandler certificateHandler = null,
            bool disposeCertificateHandler = true,
            bool cacheDownloads = true,
            bool debug = false)
            : this(
                serverSentEvents: null,
                headers,
                progress,
                timeout,
                disposeDownloadHandler,
                disposeUploadHandler,
                certificateHandler,
                disposeCertificateHandler,
                cacheDownloads,
                debug)
        {
        }

        [Preserve]
        internal RestParameters(
            List<ServerSentEvent> serverSentEvents,
            IReadOnlyDictionary<string, string> headers = null,
            IProgress<Progress> progress = null,
            int timeout = -1,
            bool disposeDownloadHandler = true,
            bool disposeUploadHandler = true,
            CertificateHandler certificateHandler = null,
            bool disposeCertificateHandler = true,
            bool cacheDownloads = true,
            bool debug = false)
        {
            ServerSentEvents = serverSentEvents ?? new List<ServerSentEvent>();
            Headers = headers;
            Progress = progress;
            Timeout = timeout;
            DisposeDownloadHandler = disposeDownloadHandler;
            DisposeUploadHandler = disposeUploadHandler;
            CertificateHandler = certificateHandler;
            DisposeCertificateHandler = disposeCertificateHandler;
            CacheDownloads = cacheDownloads;
            Debug = debug;
        }

        /// <summary>
        /// Header information for the request.
        /// </summary>
        public IReadOnlyDictionary<string, string> Headers { get; }

        /// <summary>
        /// <see cref="IProgress{T}"/> callback handler.
        /// </summary>
        public IProgress<Progress> Progress { get; }

        /// <summary>
        /// Time in seconds before request expires.
        /// </summary>
        public int Timeout { get; }

        /// <summary>
        /// <see cref="CertificateHandler"/>.
        /// </summary>
        public CertificateHandler CertificateHandler { get; }

        /// <summary>
        /// Dispose the <see cref="CertificateHandler"/>?<br/>
        /// Default is true.
        /// </summary>
        public bool DisposeCertificateHandler { get; }

        /// <summary>
        /// Dispose the <see cref="DownloadHandler"/>?<br/>
        /// Default is true.
        /// </summary>
        public bool DisposeDownloadHandler { get; }

        /// <summary>
        /// Dispose the <see cref="UploadHandler"/>?<br/>
        /// Default is true.
        /// </summary>
        public bool DisposeUploadHandler { get; }

        internal readonly List<ServerSentEvent> ServerSentEvents;

        /// <summary>
        /// Cache downloaded content.<br/>
        /// Default is true.
        /// </summary>
        public bool CacheDownloads { get; }

        /// <summary>
        /// Enable debug output of the request.
        /// </summary>
        public bool Debug { get; }
    }
}
