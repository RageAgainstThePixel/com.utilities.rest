// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Utilities.WebRequestRest
{
    internal class DownloadHandlerCallback : DownloadHandlerScript
    {
        public DownloadHandlerCallback(UnityWebRequest webRequest, int bufferSize = kEventChunkSize)
        {
            this.webRequest = webRequest;
            eventChunkSize = bufferSize;
            stream = new MemoryStream();
        }

        internal const int kEventChunkSize = 512;

        private readonly int eventChunkSize;
        private readonly MemoryStream stream;
        private readonly UnityWebRequest webRequest;

        private long streamPosition;

        private long StreamOffset => stream.Length - streamPosition;

        public Action<Response> OnDataReceived { get; set; }

        protected override byte[] GetData() => stream?.ToArray();

        protected override string GetText() => null;

        protected override bool ReceiveData(byte[] unprocessedData, int dataLength)
        {
            try
            {
                var offset = unprocessedData.Length - dataLength;
                stream.Position = stream.Length;
                stream.Write(unprocessedData, offset, dataLength);

                if (StreamOffset >= eventChunkSize)
                {
                    var multiplier = StreamOffset / eventChunkSize;
                    var bytesToRead = eventChunkSize * multiplier;
                    stream.Position = streamPosition;
                    var buffer = new byte[bytesToRead];
                    var bytesRead = stream.Read(buffer, 0, (int)bytesToRead);
                    streamPosition += bytesRead;
                    OnDataReceived?.Invoke(new Response(webRequest.url, webRequest.method, null, true, null, buffer, webRequest.responseCode, webRequest.GetResponseHeaders(), null, null));
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
                    stream.Position = streamPosition;
                    var buffer = new byte[StreamOffset];
                    var bytesRead = stream.Read(buffer);
                    streamPosition += bytesRead;
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
            stream.Dispose();
            base.Dispose();
        }
    }
}
