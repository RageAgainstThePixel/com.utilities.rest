// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Utilities.Async;

namespace Utilities.WebRequestRest
{
    internal class DownloadHandlerCallback : DownloadHandlerScript
    {
        internal const int kEventChunkSize = 512;

        private readonly MemoryStream stream = new MemoryStream();

        private long streamPosition;

        private long StreamOffset => stream.Length - streamPosition;

        private int eventChunkSize = kEventChunkSize;

        public int EventChunkSize
        {
            get => eventChunkSize;
            set
            {
                if (value < 1)
                {
                    throw new InvalidOperationException($"{nameof(EventChunkSize)} must be greater than 1!");
                }

                eventChunkSize = value;
            }
        }

        public UnityWebRequest UnityWebRequest { get; set; }

        public Action<Response> OnDataReceived { get; set; }

        protected override bool ReceiveData(byte[] unprocessedData, int dataLength)
        {
            var offset = unprocessedData.Length - dataLength;

            try
            {
                stream.Position = stream.Length;
                stream.Write(unprocessedData, offset, dataLength);

                if (StreamOffset >= EventChunkSize)
                {
                    var multiplier = StreamOffset / EventChunkSize;
                    var bytesToRead = EventChunkSize * multiplier;
                    stream.Position = streamPosition;
                    var buffer = new byte[bytesToRead];
                    var bytesRead = stream.Read(buffer, 0, (int)bytesToRead);
                    streamPosition += bytesRead;
                    OnDataReceived?.Invoke(new Response(UnityWebRequest.url, true, null, buffer, UnityWebRequest.responseCode, UnityWebRequest.GetResponseHeaders()));
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
            try
            {
                if (StreamOffset > 0)
                {
                    stream.Position = streamPosition;
                    var buffer = new byte[StreamOffset];
                    var bytesRead = stream.Read(buffer);
                    streamPosition += bytesRead;
                    OnDataReceived?.Invoke(new Response(UnityWebRequest.url, true, null, buffer, UnityWebRequest.responseCode, UnityWebRequest.GetResponseHeaders()));
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            base.CompleteContent();
        }

        public override void Dispose()
        {
            stream.Dispose();
            base.Dispose();
        }
    }
}
