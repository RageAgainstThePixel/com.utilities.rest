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
                    parameters: null,
                    cancellationToken: cts.Token);
                response.Validate(debug: true);
                Assert.IsTrue(response.Successful, "Final response should be successful");
                Assert.IsTrue(response.NativeData.HasValue && response.NativeData.Value.IsCreated, "Final response data should not be null");
                Assert.GreaterOrEqual(chunkCount, 2, "Streaming callback should be invoked at least twice (validates DownloadHandlerCallback multi-chunk path)");
                var accumulatedLength = chunks.Sum(c => c.Length);
                Assert.AreEqual(response.NativeData!.Value.Length, accumulatedLength, "Accumulated chunk data length should equal final response body size");
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
                    using var response = await Rest.GetAsync(
                        query: PostsUrl,
                        dataReceivedEventCallback: (chunkResponse) =>
                        {
                            try
                            {
                                if (chunkResponse.NativeData is { IsCreated: true } nativeData && nativeData.Length > 0)
                                {
                                    totalChunks++;
                                }
                            }
                            finally
                            {
                                chunkResponse.Dispose();
                            }
                        },
                        eventChunkSize: StreamingChunkSize,
                        parameters: null,
                        cancellationToken: cts.Token);

                    Assert.IsTrue(response.Successful, $"Request {i + 1}/{requestCount} should be successful");
                }

                Assert.Greater(totalChunks, 0, "At least one chunk should have been received across all requests");
            }
            catch (OperationCanceledException)
            {
                Assert.Ignore("Requests timed out");
            }
        }
    }
}
