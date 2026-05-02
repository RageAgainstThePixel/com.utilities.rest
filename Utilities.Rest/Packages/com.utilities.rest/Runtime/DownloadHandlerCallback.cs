// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace Utilities.WebRequestRest
{
    /// <summary>
    /// Streaming download handler that delivers response data to <see cref="OnDataReceived"/> as
    /// owned <see cref="NativeArray{T}"/> chunks. The full stream is also accumulated and can be
    /// transferred to the final <see cref="Response"/> via <see cref="DetachStreamAsArray"/>.
    /// </summary>
    /// <remarks>
    /// Threading: this handler is invoked by Unity on the main thread. <see cref="ReceiveData"/>,
    /// <see cref="EmitChunk"/>, and <see cref="DetachStreamAsArray"/> all rely on that invariant
    /// (e.g. <c>stream.AsArray()</c> aliases the list buffer and is not safe to use across threads).
    /// </remarks>
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
        /// Size in bytes of each chunk passed to <see cref="OnDataReceived"/>. Defaults to <see cref="DefaultChunkBytes"/>.
        /// </param>
        /// <param name="receiveBufferSize">
        /// Size in bytes of the preallocated receive buffer Unity uses to hand data to <see cref="ReceiveData"/>.
        /// Larger values reduce the number of <see cref="ReceiveData"/> callbacks for high-throughput streams.
        /// Defaults to <see cref="DefaultReceiveBufferBytes"/>.
        /// </param>
        public DownloadHandlerCallback(UnityWebRequest webRequest, int chunkSize = DefaultChunkBytes, int receiveBufferSize = DefaultReceiveBufferBytes)
            : base(new byte[receiveBufferSize])
        {
            this.webRequest = webRequest;
            eventChunkSize = chunkSize;
            stream = new NativeList<byte>(Allocator.Persistent);
        }

        /// <summary>
        /// Default size of each chunk delivered to <see cref="OnDataReceived"/> (not the buffer passed to base).
        /// </summary>
        public const int DefaultChunkBytes = 4096;

        /// <summary>
        /// Default size of the preallocated buffer passed to Unity in the base ctor; avoids per-call managed alloc in <see cref="ReceiveData"/>.
        /// Kept large for efficient network reads.
        /// </summary>
        public const int DefaultReceiveBufferBytes = 65536;

        /// <summary>Legacy alias for <see cref="DefaultChunkBytes"/>. Use <see cref="DefaultChunkBytes"/> instead.</summary>
        [Obsolete("Use DefaultChunkBytes instead.")]
        // ReSharper disable once InconsistentNaming
        internal const int kEventChunkSize = DefaultChunkBytes;

        private readonly int eventChunkSize;
        private readonly UnityWebRequest webRequest;
        private readonly Dictionary<string, string> cachedResponseHeaders = new();

        private bool responseHeadersCached;

        private NativeList<byte> stream;

        private long streamPosition;

        private long StreamOffset => stream.IsCreated ? stream.Length - streamPosition : 0;

        /// <summary>
        /// Callback invoked for each streaming chunk.
        /// The <see cref="Response"/> contains the chunk data;
        /// dispose the <see cref="Response"/> when done (e.g. in a <c>finally</c> block).
        /// </summary>
        public Action<Response> OnDataReceived { get; set; }

        /// <inheritdoc />
        /// <remarks>
        /// Returns <see langword="null"/> because the streaming pipeline is responsible for body
        /// ownership: callers detach the accumulated stream via <see cref="DetachStreamAsArray"/>.
        /// This avoids the duplicate full-body copy that <see cref="DownloadHandlerScript"/>
        /// otherwise materializes for the final <see cref="Response"/>.
        /// </remarks>
        protected override byte[] GetData() => null;

        /// <inheritdoc />
        protected override string GetText() => null;

        /// <summary>
        /// Transfers ownership of the accumulated stream to a new <see cref="NativeArray{T}"/>.
        /// The internal stream buffer is disposed after the copy. Returns a default (uncreated)
        /// array if the stream is empty.
        /// </summary>
        /// <param name="allocator">Allocator for the returned array; caller owns and must dispose.</param>
        /// <returns>A native array containing the full stream contents, or <c>default</c> if empty.</returns>
        internal NativeArray<byte> DetachStreamAsArray(Allocator allocator)
        {
            if (!stream.IsCreated || stream.Length == 0)
            {
                if (stream.IsCreated)
                {
                    stream.Dispose();
                    stream = default;
                }
                return default;
            }

            var arr = new NativeArray<byte>(stream.Length, allocator);
            NativeArray<byte>.Copy(stream.AsArray(), arr, stream.Length);
            stream.Dispose();
            stream = default;
            return arr;
        }

        /// <inheritdoc />
        /// <remarks>
        /// <para>
        /// In preallocated mode, Unity reuses the buffer passed to the base ctor; <paramref name="dataLength"/>
        /// marks how many bytes are new (see Unity docs). Do not pass Unity's callback array to
        /// <see cref="DownloadHandler.ReceiveData(byte[],int)"/> on the base class—doing so can crash on
        /// Android/IL2CPP when native and managed lifetimes disagree. Copy into a separate buffer and call
        /// <c>base.ReceiveData(rented, …)</c> with that buffer only.
        /// </para>
        /// <para>
        /// Buffers come from <see cref="ArrayPool{T}.Shared"/> to avoid a new <see cref="byte"/>[] allocation
        /// every callback. Pooling assumes <c>base.ReceiveData</c> does not retain the array past this
        /// synchronous call (normal for this API). The second argument to <c>base.ReceiveData</c> is
        /// <c>byteCount</c>, not the rented array's length, when the pool returns an oversized backing array.
        /// </para>
        /// </remarks>
        protected override bool ReceiveData(byte[] unprocessedData, int dataLength)
        {
            if (unprocessedData == null || dataLength <= 0)
            {
                return base.ReceiveData(unprocessedData, dataLength);
            }

            var srcOffset = unprocessedData.Length >= dataLength
                ? unprocessedData.Length - dataLength
                : 0;
            var byteCount = Math.Min(dataLength, unprocessedData.Length - srcOffset);
            if (byteCount <= 0)
            {
                return base.ReceiveData(unprocessedData, dataLength);
            }

            var rented = ArrayPool<byte>.Shared.Rent(dataLength);
            try
            {
                Buffer.BlockCopy(unprocessedData, srcOffset, rented, 0, byteCount);

                try
                {
                    AppendToStream(rented, byteCount);

                    if (StreamOffset >= eventChunkSize)
                    {
                        var multiplier = StreamOffset / eventChunkSize;
                        var maxMultiplier = (long)int.MaxValue / eventChunkSize;
                        if (multiplier > maxMultiplier)
                        {
                            multiplier = maxMultiplier;
                        }

                        EmitChunk((int)(eventChunkSize * multiplier));
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }

                return base.ReceiveData(rented, byteCount);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented, clearArray: false);
            }
        }

        /// <summary>
        /// Appends <paramref name="length"/> bytes from <paramref name="source"/> into the
        /// accumulating <see cref="NativeList{T}"/> stream using a stack-only pin (<c>fixed</c>)
        /// and a single <see cref="NativeList{T}.AddRange(void*, int)"/> memcpy. Avoids the
        /// per-call <c>GCHandle.Alloc</c> that <c>NativeArray&lt;byte&gt;.Copy(byte[], …)</c>
        /// performs internally.
        /// </summary>
        private unsafe void AppendToStream(byte[] source, int length)
        {
            if (length <= 0)
            {
                return;
            }

            fixed (byte* ptr = source)
            {
                stream.AddRange(ptr, length);
            }
        }

        /// <inheritdoc />
        protected override void CompleteContent()
        {
            Complete();
            base.CompleteContent();
        }

        /// <summary>
        /// Flushes any remaining buffered bytes to <see cref="OnDataReceived"/> as a final chunk.
        /// Idempotent: safe to call again from the streaming overloads' <c>finally</c> in case
        /// the request was aborted before <see cref="CompleteContent"/> ran.
        /// </summary>
        internal void Complete()
        {
            try
            {
                while (StreamOffset > 0)
                {
                    var remaining = StreamOffset;
                    var bytesToRead = remaining > int.MaxValue ? int.MaxValue : (int)remaining;
                    EmitChunk(bytesToRead);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private void EmitChunk(int bytesToRead)
        {
            if (!responseHeadersCached)
            {
                cachedResponseHeaders.Clear();
                var headers = webRequest.GetResponseHeaders();
                if (headers != null)
                {
                    foreach (var kv in headers)
                    {
                        cachedResponseHeaders[kv.Key] = kv.Value;
                    }
                }

                responseHeadersCached = true;
            }

            if (streamPosition > int.MaxValue)
            {
                Debug.LogError($"[{nameof(DownloadHandlerCallback)}] streamPosition exceeds int.MaxValue; aborting chunk emission to avoid truncation.");
                return;
            }

            var chunk = new NativeArray<byte>(bytesToRead, Allocator.Persistent);
            NativeArray<byte>.Copy(stream.AsArray(), (int)streamPosition, chunk, 0, bytesToRead);
            streamPosition += bytesToRead;

            var response = new Response(
                request: webRequest.url,
                method: webRequest.method,
                requestBody: null,
                successful: true,
                body: null,
                nativeData: chunk,
                responseCode: webRequest.responseCode,
                headers: cachedResponseHeaders,
                parameters: null);

            try
            {
                OnDataReceived?.Invoke(response);
            }
            catch
            {
                response.Dispose();
                throw;
            }
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            if (stream.IsCreated)
            {
                stream.Dispose();
                stream = default;
            }

            base.Dispose();
        }
    }
}
