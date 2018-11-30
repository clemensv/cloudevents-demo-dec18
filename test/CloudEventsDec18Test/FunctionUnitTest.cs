// Copyright (c) Cloud Native Foundation. 
// Licensed under the Apache 2.0 license.
// See LICENSE file in the project root for full license information.

using Xunit;

[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]


namespace CloudEventsDec18Test
{
    using System;
    using System.Collections.Concurrent;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web.Http;
    using System.Web.Http.Hosting;
    using CloudNative.CloudEvents;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.CloudEventsDec18;
    using Microsoft.Extensions.Logging.Abstractions;
    using Newtonsoft.Json;
    using Xunit;

    
    public class FunctionUnitTest
    {
        static int port = 52670; 
        const string testContextHeader = "testcontext";
        HttpListener listener;
        string listenerAddress = $"http://localhost:{port++}/";


        ConcurrentDictionary<string, Func<HttpListenerContext, Task>> pendingRequests =
            new ConcurrentDictionary<string, Func<HttpListenerContext, Task>>();

        public FunctionUnitTest()
        {
            listener = new HttpListener()
            {
                AuthenticationSchemes = AuthenticationSchemes.Anonymous,
                Prefixes = { listenerAddress }
            };
            listener.Start();
            listener.GetContextAsync().ContinueWith(t =>
            {
                if (t.IsCompleted)
                {
                    HandleContext(t.Result);
                }
            });
        }



        public void Dispose()
        {
            listener.Stop();
        }

        async Task HandleContext(HttpListenerContext requestContext)
        {
            var ctxHeaderValue = requestContext.Request.QueryString["ctx"];
            if (pendingRequests.TryRemove(ctxHeaderValue, out var pending))
            {
                await pending(requestContext);
            }
#pragma warning disable 4014
            listener.GetContextAsync().ContinueWith(t =>
            {
                if (t.IsCompleted)
                {
                    HandleContext(t.Result);
                }
            });
#pragma warning restore 4014
        }


        static string cejson = "{" +
                               "\"cloudEventsVersion\":\"0.1\"," +
                               "\"eventType\":\"word.found.noun\"," +
                               "\"source\":\"http://srcdog.com/madlibs\"," +
                               "\"eventID\":\"96fb5f0b-001e-0108-6dfe-da6e2806f124\"," +
                               "\"eventTime\":\"2018-10-23T12:28:22.4579346Z\"" +
                               "}";

        [Fact]
        public void StructuredWithCallbackx5()
        {
            // the function picks randomly between content modes. Repeat 5 times to catch both.
            for (int i = 0; i < 5; i++)
            {
                StructuredWithCallback();
            }
        }

        [Fact]
        public void BinaryWithCallbackx5()
        {
            // the function picks randomly between content modes. Repeat 5 times to catch both.
            for (int i = 0; i < 5; i++)
            {
                BinaryWithCallback();
            }
        }


        public void BinaryWithCallback()
        {
            TaskCompletionSource<CloudEvent> tcs = new TaskCompletionSource<CloudEvent>();
            string ctx = Guid.NewGuid().ToString();
            pendingRequests.TryAdd(ctx, async context =>
            {
                var ce1 = context.Request.ToCloudEvent();
                if (ce1.Data is Stream)
                {
                    var ms = new MemoryStream();
                    ((Stream)ce1.Data).CopyTo(ms);
                    ms.Position = 0;
                    ce1.Data = ms;
                }

                tcs.SetResult(ce1);

                context.Response.StatusCode = 204;
                context.Response.Close();
            });

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://localhost"),
                Headers =
                {
                    { "Ce-Id", "3a1f086e-f474-4990-bea3-f926fe4e70ec"},
                    { "Ce-Source", "https://srcdog.com/madlibs"},
                    {"Ce-Specversion", "0.1"},
                    {"Ce-Time", "2018-11-29T17:55:24Z"},
                    {"Ce-Type" , "word.found.noun"}
                }
            };
            request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, new HttpConfiguration());
            request.Headers.Add("X-Callback-URL", listenerAddress + "?ctx=" + ctx.ToString());
            var res = Functions.Madlibs(request, NullLogger.Instance).GetAwaiter().GetResult();
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
            Assert.True(tcs.Task.Wait(2000));
            var ce = tcs.Task.Result;
            Assert.Equal("word.picked.noun", ce.Type);
            if (ce.Data is Stream)
            {
                dynamic data = JsonSerializer.Create()
                    .Deserialize(new JsonTextReader(new StreamReader((Stream)ce.Data)));
                Assert.NotEmpty((string)data.word);
            }
            else
            {
                dynamic data = ce.Data;
                Assert.NotEmpty((string)data.word);
            }
        }

        public void StructuredWithCallback()
        {
            TaskCompletionSource<CloudEvent> tcs = new TaskCompletionSource<CloudEvent>();
            string ctx = Guid.NewGuid().ToString();
            pendingRequests.TryAdd(ctx, async context =>
            {
                var ce1 = context.Request.ToCloudEvent();
                if (ce1.Data is Stream)
                {
                    var ms = new MemoryStream();
                    ((Stream)ce1.Data).CopyTo(ms);
                    ms.Position = 0;
                    ce1.Data = ms;
                }
                tcs.SetResult(ce1);

                context.Response.StatusCode = 204;
                context.Response.Close();
            });

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://localhost"),
                Content = new StringContent(cejson, Encoding.UTF8, "application/cloudevents+json")
            };
            request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, new HttpConfiguration());
            request.Headers.Add("X-Callback-URL", listenerAddress + "?ctx=" + ctx.ToString());
            var res = Functions.Madlibs(request, NullLogger.Instance).GetAwaiter().GetResult();
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
            Assert.True(tcs.Task.Wait(2000));
            var ce = tcs.Task.Result;
            Assert.Equal("word.picked.noun", ce.Type);
            if (ce.Data is Stream)
            {
                dynamic data = JsonSerializer.Create().Deserialize(new JsonTextReader(new StreamReader((Stream)ce.Data)));
                Assert.NotEmpty((string)data.word);
            }
            else
            {
                dynamic data = ce.Data;
                Assert.NotEmpty((string)data.word);
            }
        }
    }
}