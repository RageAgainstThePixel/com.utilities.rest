// Licensed under the MIT License. See LICENSE in the project root for license information.

using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TestTools;

namespace Utilities.WebRequestRest.Tests
{
    internal class TestFixture_02_CRUD
    {
        private const string SseServer = "https://echo.websocket.org/.sse";

        [Test]
        [Timeout(5100)]
        public async Task Test_01_ServerSentEvents()
        {
            try
            {
                using var cts = new CancellationTokenSource();
                cts.CancelAfter(TimeSpan.FromSeconds(5));
                await Rest.GetAsync(SseServer, ServerSentEventHandler, cancellationToken: cts.Token);

                Task ServerSentEventHandler(Response res, ServerSentEvent sse)
                {
                    Debug.Log(sse.ToJsonString());
                    Assert.IsTrue(res.Successful);
                    res.Validate(true);

                    return Task.CompletedTask;
                }
            }
            catch (Exception e)
            {
                switch (e)
                {
                    case TaskCanceledException:
                    case OperationCanceledException:
                        // expected due to cancellation token
                        break;
                    default:
                        Debug.LogException(e);
                        throw;

                }
            }
        }

        [Test]
        public async Task Test_02_01_GET()
        {
            try
            {
                var response = await Rest.GetAsync("https://jsonplaceholder.typicode.com/posts/1");
                response.Validate(true);
                Assert.IsTrue(response.Successful);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        [Test]
        public async Task Test_02_02_POST()
        {
            try
            {
                var payload = new { title = "foo", body = "bar", userId = 1 };
                var response = await Rest.PostAsync("https://jsonplaceholder.typicode.com/posts", JsonConvert.SerializeObject(payload));
                response.Validate(true);
                Assert.IsTrue(response.Successful);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        [Test]
        public async Task Test_02_03_PUT()
        {
            try
            {
                var payload = new { id = 1, title = "foo", body = "bar", userId = 1 };
                var response = await Rest.PutAsync("https://jsonplaceholder.typicode.com/posts/1", JsonConvert.SerializeObject(payload));
                response.Validate(true);
                Assert.IsTrue(response.Successful);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        [Test]
        public async Task Test_02_04_PATCH()
        {
            try
            {
                var payload = new { title = "foo" };
                var response = await Rest.PatchAsync("https://jsonplaceholder.typicode.com/posts/1", JsonConvert.SerializeObject(payload));
                response.Validate(true);
                Assert.IsTrue(response.Successful);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }

        [Test]
        public async Task Test_02_04_DELETE()
        {
            try
            {
                var response = await Rest.DeleteAsync("https://jsonplaceholder.typicode.com/posts/1");
                response.Validate(true);
                Assert.IsTrue(response.Successful);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }
    }
}
