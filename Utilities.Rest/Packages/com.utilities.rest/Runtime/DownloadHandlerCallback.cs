// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Collections;

namespace Utilities.WebRequestRest
{
    internal class DownloadHandlerCallback : DownloadHandlerScript
    {
        public DownloadHandlerCallback(UnityWebRequest webRequest, int bufferSize = kEventChunkSize)
        {
            this.webRequest = webRequest;
            eventChunkSize = bufferSize;
            stream = new NativeList<byte>(Allocator.Persistent);
        }

        // ReSharper disable once InconsistentNaming
        internal const int kEventChunkSize = 512;

        private readonly int eventChunkSize;
        private NativeList<byte> stream;
        private readonly UnityWebRequest webRequest;

        private long streamPosition;

        private long StreamOffset => stream.IsCreated ? stream.Length - streamPosition : 0;

        public Action<Response> OnDataReceived { get; set; }

        protected override byte[] GetData() => stream.IsCreated ? stream.ToArray() : null;

        protected override string GetText() => null;

        protected override bool ReceiveData(byte[] unprocessedData, int dataLength)
        {
            if (unprocessedData == null || dataLength <= 0)
            {
                return base.ReceiveData(unprocessedData, dataLength);
            }

            byte[] copy = null;
            try
            {
                var offset = Math.Max(0, unprocessedData.Length - dataLength);
                copy = new byte[dataLength];
                Array.Copy(unprocessedData, offset, copy, 0, dataLength);

                for (var i = 0; i < dataLength; i++)
                {
                    stream.Add(copy[i]);
                }

                if (StreamOffset >= eventChunkSize)
                {
                    var multiplier = StreamOffset / eventChunkSize;
                    var bytesToRead = (int)(eventChunkSize * multiplier);
                    var buffer = new byte[bytesToRead];
                    var arr = stream.AsArray();
                    for (var i = 0; i < bytesToRead; i++)
                    {
                        buffer[i] = arr[(int)streamPosition + i];
                    }

                    streamPosition += bytesToRead;
                    OnDataReceived?.Invoke(new Response(webRequest.url, webRequest.method, null, true, null, buffer, webRequest.responseCode, webRequest.GetResponseHeaders(), null!, null));
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            return base.ReceiveData(copy ?? Array.Empty<byte>(), copy != null ? dataLength : 0);
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
                    var bytesToRead = (int)StreamOffset;
                    var buffer = new byte[bytesToRead];
                    var arr = stream.AsArray();
                    for (var i = 0; i < bytesToRead; i++)
                    {
                        buffer[i] = arr[(int)streamPosition + i];
                    }

                    streamPosition += bytesToRead;
                    OnDataReceived?.Invoke(new Response(webRequest.url, webRequest.method, null, true, null, buffer, webRequest.responseCode, webRequest.GetResponseHeaders(), null, null));
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
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
