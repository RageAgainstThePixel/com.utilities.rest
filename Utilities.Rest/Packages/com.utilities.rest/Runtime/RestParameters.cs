// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace Utilities.WebRequestRest
{
    /// <summary>
    /// A common class for restful parameters
    /// </summary>
    public sealed class RestParameters
    {
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
        {
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
        public IReadOnlyDictionary<string, string> Headers { get; internal set; }

        /// <summary>
        /// <see cref="IProgress{T}"/> callback handler.
        /// </summary>
        public IProgress<Progress> Progress { get; internal set; }

        /// <summary>
        /// Time in seconds before request expires.
        /// </summary>
        public int Timeout { get; internal set; }

        /// <summary>
        /// <see cref="CertificateHandler"/>.
        /// </summary>
        public CertificateHandler CertificateHandler { get; internal set; }

        /// <summary>
        /// Dispose the <see cref="CertificateHandler"/>?<br/>
        /// Default is true.
        /// </summary>
        public bool DisposeCertificateHandler { get; internal set; }

        /// <summary>
        /// Dispose the <see cref="DownloadHandler"/>?<br/>
        /// Default is true.
        /// </summary>
        public bool DisposeDownloadHandler { get; internal set; }

        /// <summary>
        /// Dispose the <see cref="UploadHandler"/>?<br/>
        /// Default is true.
        /// </summary>
        public bool DisposeUploadHandler { get; internal set; }

        internal int ServerSentEventCount { get; set; }

        // ReSharper disable once InconsistentNaming
        internal readonly List<ServerSentEvent> ServerSentEvents = new();

        /// <summary>
        /// Cache downloaded content.<br/>
        /// Default is true.
        /// </summary>
        public bool CacheDownloads { get; set; }

        /// <summary>
        /// Enable debug output of the request.
        /// </summary>
        public bool Debug { get; set; }
    }
}
