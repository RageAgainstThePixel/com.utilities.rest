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
                var response = await Rest.GetAsync(
                    query: PostsUrl,
                    dataReceivedEventCallback: dataCallback =>
                    {
                        chunkCount++;
                        Assert.IsTrue(dataCallback.Successful, "Streaming response should be successful");
                        Assert.IsNotNull(dataCallback.Data, "Chunk data should not be null");

                        if (dataCallback.Data is { Length: > 0 })
                        {
                            chunks.Add((byte[])dataCallback.Data.Clone());
                        }
                    },
                    eventChunkSize: StreamingChunkSize,
                    parameters: null,
                    cancellationToken: cts.Token);
                response.Validate(debug: true);
                Assert.IsTrue(response.Successful, "Final response should be successful");
                Assert.IsNotNull(response.Data, "Final response data should not be null");
                Assert.GreaterOrEqual(chunkCount, 2, "Streaming callback should be invoked at least twice (validates DownloadHandlerCallback multi-chunk path)");
                var accumulatedLength = chunks.Sum(c => c.Length);
                Assert.AreEqual(response.Data.Length, accumulatedLength, "Accumulated chunk data length should equal final response body size");
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
                    var response = await Rest.GetAsync(
                        query: PostsUrl,
                        dataReceivedEventCallback: dataCallback =>
                        {
                            if (dataCallback.Data != null)
                            {
                                totalChunks++;
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
