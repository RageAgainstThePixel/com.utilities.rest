// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Utilities.WebRequestRest.Tests
{
    /// <summary>
    /// Minimal loopback HTTP server that serves one SSE response for integration tests.
    /// Avoids flaky third-party endpoints (e.g. echo.websocket.org) in CI.
    /// </summary>
    internal sealed class LocalSseTestServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly Task _serveTask;

        public LocalSseTestServer()
        {
            _listener = new TcpListener(IPAddress.Loopback, 0);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _serveTask = ServeOnceAsync();
        }

        public int Port { get; }

        public Uri SseUri => new($"http://127.0.0.1:{Port}/stream");

        private async Task ServeOnceAsync()
        {
            try
            {
                using var client = await _listener.AcceptTcpClientAsync().ConfigureAwait(false);
                using var stream = client.GetStream();
                await ReadHttpHeadersAsync(stream).ConfigureAwait(false);

                const string payload = "data: unit-test\n\n";
                var payloadBytes = Encoding.UTF8.GetBytes(payload);
                var header = new StringBuilder()
                    .Append("HTTP/1.1 200 OK\r\n")
                    .Append("Content-Type: text/event-stream\r\n")
                    .Append("Cache-Control: no-cache\r\n")
                    .Append("Connection: close\r\n")
                    .Append("Content-Length: ")
                    .Append(payloadBytes.Length)
                    .Append("\r\n\r\n")
                    .ToString();
                var headerBytes = Encoding.UTF8.GetBytes(header);
                await stream.WriteAsync(headerBytes, 0, headerBytes.Length).ConfigureAwait(false);
                await stream.WriteAsync(payloadBytes, 0, payloadBytes.Length).ConfigureAwait(false);
            }
            catch (ObjectDisposedException)
            {
                // Listener stopped during dispose.
            }
            catch (SocketException)
            {
                // Accept cancelled when the listener is stopped.
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[{nameof(LocalSseTestServer)}] {e.Message}");
            }
            finally
            {
                try { _listener.Stop(); } catch { /* ignored */ }
            }
        }

        private static async Task ReadHttpHeadersAsync(NetworkStream stream)
        {
            var buffer = new List<byte>(256);
            var scratch = new byte[1];
            while (true)
            {
                var n = await stream.ReadAsync(scratch, 0, 1).ConfigureAwait(false);
                if (n == 0)
                {
                    throw new InvalidOperationException("Client closed before sending HTTP headers.");
                }

                buffer.Add(scratch[0]);
                var c = buffer.Count;
                if (c >= 4 &&
                    buffer[c - 4] == '\r' &&
                    buffer[c - 3] == '\n' &&
                    buffer[c - 2] == '\r' &&
                    buffer[c - 1] == '\n')
                {
                    return;
                }

                if (c > 65536)
                {
                    throw new InvalidOperationException("HTTP header block too large.");
                }
            }
        }

        public void Dispose()
        {
            try { _listener.Stop(); } catch { /* ignored */ }

            try
            {
                if (!_serveTask.Wait(TimeSpan.FromSeconds(15)))
                {
                    Debug.LogWarning($"[{nameof(LocalSseTestServer)}] Server task did not complete in time.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[{nameof(LocalSseTestServer)}] {e.Message}");
            }
        }
    }
}
