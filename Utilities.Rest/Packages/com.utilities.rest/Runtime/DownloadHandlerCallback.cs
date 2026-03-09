// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Utilities.WebRequestRest
{
    internal class DownloadHandlerCallback : DownloadHandlerScript
    {
        /// <summary>
        /// Initializes a download handler that streams response data in chunks via <see cref="OnDataReceived"/>.
        /// Uses a preallocated receive buffer (no per-call managed alloc) and delivers data in chunks of <paramref name="chunkSize"/> bytes.
        /// </summary>
        /// <param name="webRequest">
        /// The web request this handler is attached to (used for URL, method, response code, and headers).
        /// </param>
        /// <param name="chunkSize">
        /// Size in bytes of each chunk passed to <see cref="OnDataReceived"/>. Defaults to <see cref="kEventChunkSize"/> (512).
        /// </param>
        public DownloadHandlerCallback(UnityWebRequest webRequest, int chunkSize = kEventChunkSize)
            : base(new byte[kReceiveBufferSize])
        {
            this.webRequest = webRequest;
            eventChunkSize = chunkSize;
            stream = new NativeList<byte>(Allocator.Persistent);
        }

        /// <summary>
        /// Default size of each chunk delivered to <see cref="OnDataReceived"/> (not the buffer passed to base).
        /// </summary>
        // ReSharper disable once InconsistentNaming
        internal const int kEventChunkSize = 512;

        /// <summary>
        /// Size of the preallocated buffer passed to Unity in the base ctor; avoids per-call managed alloc in ReceiveData.
        /// Kept large for efficient network reads.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        private const int kReceiveBufferSize = 65536;

        /// <summary>Chunk size in bytes for each <see cref="OnDataReceived"/> callback (from constructor).</summary>
        private readonly int eventChunkSize;
        private readonly UnityWebRequest webRequest;
        private readonly Dictionary<string, string> emptyResponseHeaders = new();

        private NativeList<byte> stream;

        private long streamPosition;

        private long StreamOffset => stream.IsCreated ? stream.Length - streamPosition : 0;

        /// <summary>
        /// Callback invoked for each streaming chunk.
        /// The <see cref="Response"/> contains the chunk data;
        /// dispose the <see cref="Response"/> when done (e.g. in a <c>finally</c> block).
        /// </summary>
        public Action<Response> OnDataReceived { get; set; }

        protected override byte[] GetData()
        {
            if (!stream.IsCreated || stream.Length == 0)
            {
                return null;
            }

            var nativeArray = stream.ToArray(Allocator.Temp);

            try
            {
                return nativeArray.ToArray();
            }
            finally
            {
                nativeArray.Dispose();
            }
        }

        protected override string GetText() => null;

        protected override bool ReceiveData(byte[] unprocessedData, int dataLength)
        {
            if (unprocessedData == null || dataLength <= 0)
            {
                return base.ReceiveData(unprocessedData, dataLength);
            }

            try
            {
                var nativeArray = new NativeArray<byte>(dataLength, Allocator.Temp);
                try
                {
                    NativeArray<byte>.Copy(unprocessedData, 0, nativeArray, 0, dataLength);
                    stream.AddRange(nativeArray);
                }
                finally
                {
                    nativeArray.Dispose();
                }

                if (StreamOffset >= eventChunkSize)
                {
                    var multiplier = StreamOffset / eventChunkSize;
                    EmitChunk((int)(eventChunkSize * multiplier));
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return base.ReceiveData(unprocessedData, dataLength);
        }

        protected override void CompleteContent()
        {
            Complete();
            base.CompleteContent();
        }

        internal void Complete()
        {
            try
            {
                if (StreamOffset > 0)
                {
                    EmitChunk((int)StreamOffset);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private void EmitChunk(int bytesToRead)
        {
            var headers = webRequest.GetResponseHeaders() ?? emptyResponseHeaders;
            var chunk = new NativeArray<byte>(bytesToRead, Allocator.Persistent);
            NativeArray<byte>.Copy(stream.AsArray(), (int)streamPosition, chunk, 0, bytesToRead);
            streamPosition += bytesToRead;
            OnDataReceived?.Invoke(
                new Response(
                    request: webRequest.url,
                    method: webRequest.method,
                    requestBody: null,
                    successful: true,
                    body: null,
                    nativeData: chunk,
                    responseCode: webRequest.responseCode,
                    headers: headers,
                    parameters: null));
        }

        public override void Dispose()
        {
            if (stream.IsCreated)
            {
                stream.Dispose();
            }

            base.Dispose();
        }
    }
}
