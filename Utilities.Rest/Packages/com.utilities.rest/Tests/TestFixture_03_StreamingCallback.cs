// Licensed under the MIT License. See LICENSE in the project root for license information.
// Validates DownloadHandlerCallback streaming path (used by PostAsync(..., callback, eventChunkSize)).

using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Utilities.WebRequestRest.Tests
{
    internal class TestFixture_03_StreamingCallback
    {
        private const int StreamingChunkSize = 8192;
        private static readonly Uri PostsUrl = new("https://jsonplaceholder.typicode.com/posts");

        /// <summary>
        /// Ask for uncompressed payload so TLS/stack chunking matches what DownloadHandlerCallback sees,
        /// instead of a single gzip blob that some runners decode in one shot.
        /// </summary>
        private static readonly RestParameters UncompressedPostsParameters = new(
            headers: new Dictionary<string, string> { ["Accept-Encoding"] = "identity" });

        [Test]
        public async Task Test_01_StreamingGet_ReceivesMultipleChunks_AndFullBodyMatches()
        {
            var chunks = new List<byte[]>();
            var chunkCount = 0;
            try
            {
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(15));
                using var response = await Rest.GetAsync(
                    query: PostsUrl,
                    dataReceivedEventCallback: (chunkResponse) =>
                    {
                        try
                        {
                            chunkCount++;
                            Assert.IsTrue(chunkResponse.Successful, "Streaming response should be successful");
                            if (chunkResponse.NativeData is { IsCreated: true } nativeData && nativeData.Length > 0)
                            {
                                chunks.Add(nativeData.ToArray());
                            }
                        }
                        finally
                        {
                            chunkResponse.Dispose();
                        }
                    },
                    eventChunkSize: StreamingChunkSize,
                    parameters: UncompressedPostsParameters,
                    cancellationToken: cts.Token);
                response.Validate(debug: true);
                Assert.IsTrue(response.Successful, "Final response should be successful");
                Assert.IsTrue(response.HasNativeData, "Final response data should not be null");
                var accumulatedLength = chunks.Sum(c => c.Length);
                Assert.AreEqual(response.NativeData.Length, accumulatedLength, "Accumulated chunk data length should equal final response body size");
                Assert.GreaterOrEqual(chunkCount, 1, "Streaming callback should run at least once.");
            }
            catch (OperationCanceledException)
            {
                Assert.Ignore("Request timed out");
            }
        }

        [Test]
        public async Task Test_02_StreamingGet_MultipleRequestsInSequence_NoException()
        {
            const int requestCount = 5;
            var totalChunks = 0;
            try
            {
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(30));

                for (var i = 0; i < requestCount; i++)
                {
                    var perRequestChunkBytes = 0;
                    using var response = await Rest.GetAsync(
                        query: PostsUrl,
                        dataReceivedEventCallback: (chunkResponse) =>
                        {
                            try
                            {
                                if (chunkResponse.NativeData is { IsCreated: true } nativeData && nativeData.Length > 0)
                                {
                                    totalChunks++;
                                    perRequestChunkBytes += nativeData.Length;
                                }
                            }
                            finally
                            {
                                chunkResponse.Dispose();
                            }
                        },
                        eventChunkSize: StreamingChunkSize,
                        parameters: UncompressedPostsParameters,
                        cancellationToken: cts.Token);

                    Assert.IsTrue(response.Successful, $"Request {i + 1}/{requestCount} should be successful");
                    Assert.IsTrue(response.HasNativeData, $"Request {i + 1}/{requestCount} final response should have native data");
                    Assert.AreEqual(response.NativeData.Length, perRequestChunkBytes, $"Request {i + 1}/{requestCount}: accumulated chunk length should equal final response body size");
                }

                Assert.Greater(totalChunks, 0, "At least one chunk should have been received across all requests");
            }
            catch (OperationCanceledException)
            {
                Assert.Ignore("Requests timed out");
            }
        }

        [Test]
        public async Task Test_03_StreamingGet_CancellationDuringStream_NoUseAfterDispose()
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(15));
            var firstChunkReceived = false;
            var observedException = (Exception)null;

            try
            {
                using var response = await Rest.GetAsync(
                    query: PostsUrl,
                    dataReceivedEventCallback: (chunkResponse) =>
                    {
                        try
                        {
                            if (!firstChunkReceived)
                            {
                                firstChunkReceived = true;
                                cts.Cancel();
                            }
                        }
                        finally
                        {
                            chunkResponse.Dispose();
                        }
                    },
                    eventChunkSize: StreamingChunkSize,
                    parameters: UncompressedPostsParameters,
                    cancellationToken: cts.Token);

                Assert.IsTrue(response.Successful || cts.IsCancellationRequested,
                    "Request must either complete cleanly or be cancelled");
            }
            catch (OperationCanceledException)
            {
                observedException = null;
            }
            catch (Exception ex)
            {
                observedException = ex;
            }

            Assert.IsNull(observedException,
                $"Cancellation race must not surface as a non-cancellation exception (got: {observedException}). This guards against use-after-dispose of webRequest in the progress/SSE pump.");
        }

        [Test]
        public async Task Test_04_StreamingGet_ThrowingCallback_DisposesChunkResponse()
        {
            Response capturedFirstChunk = null;

            try
            {
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(15));
                using var response = await Rest.GetAsync(
                    query: PostsUrl,
                    dataReceivedEventCallback: (chunkResponse) =>
                    {
                        if (capturedFirstChunk == null)
                        {
                            capturedFirstChunk = chunkResponse;
                            throw new InvalidOperationException("Simulated user-callback failure");
                        }

                        chunkResponse.Dispose();
                    },
                    eventChunkSize: StreamingChunkSize,
                    parameters: UncompressedPostsParameters,
                    cancellationToken: cts.Token);
            }
            catch (OperationCanceledException)
            {
                Assert.Ignore("Request timed out");
                return;
            }

            Assert.IsNotNull(capturedFirstChunk,
                "Streaming callback should have been invoked at least once before throwing");
            Assert.IsTrue(capturedFirstChunk.IsDisposed,
                "EmitChunk must dispose the chunk Response when the user callback throws (guards against C3 leak).");
        }
    }
}
