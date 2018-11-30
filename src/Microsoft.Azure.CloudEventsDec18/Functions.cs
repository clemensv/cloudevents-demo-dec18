// Copyright (c) Microsoft Corporation
// Licensed under the Apache 2.0 license.
// See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.CloudEventsDec18
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Mime;
    using System.Threading.Tasks;
    using CloudNative.CloudEvents;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;

    public static class Functions
    {
        private const string SourceIdentifier = "urn:azure-microsoft-com:messaging:madlibs";
        static readonly ContentType Json = new ContentType("application/json");
        static Random rnd = new Random();

        [FunctionName("Madlibs")]
        public static async Task<HttpResponseMessage> Madlibs(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
            HttpRequestMessage req,
            ILogger log)
        {
            try
            {
                // opt into push if needed
                if (req.IsWebHookValidationRequest())
                {
                    return await req.HandleAsWebHookValidationRequest(null, null);
                }

                if (!req.Headers.Contains("X-Callback-URL"))
                {
                    // thanks, but we can't respond without an address
                    log.LogInformation("Didn't find 'X-Callback-URL' header.");
                    return new HttpResponseMessage(HttpStatusCode.BadRequest);
                }
                string callbackUrl = req.Headers.GetValues("X-Callback-URL").FirstOrDefault();

                CloudEvent receivedCloudEvent = req.ToCloudEvent();
                CloudEvent raisedCloudEvent = null;

                log.LogInformation($"Processing {receivedCloudEvent.SpecVersion} with {receivedCloudEvent.Type}" );
                log.LogInformation($"Callback to {callbackUrl}");

                switch (receivedCloudEvent.Type)
                {
                    case "word.found.noun":
                        raisedCloudEvent = new CloudEvent("word.picked.noun",
                            new Uri(SourceIdentifier))
                        {
                            ContentType = Json,
                            Data = new { word = Words.All.Nouns[rnd.Next(Words.All.Nouns.Length)] }
                        };
                        break;
                    case "word.found.verb":
                        raisedCloudEvent = new CloudEvent("word.picked.verb",
                            new Uri(SourceIdentifier))
                        {
                            ContentType = Json,
                            Data = new { word = Words.All.Verbs[rnd.Next(Words.All.Verbs.Length)] }
                        };
                        break;
                    case "word.found.exclamation":
                        raisedCloudEvent = new CloudEvent("word.picked.exlamation",
                            new Uri(SourceIdentifier))
                        {
                            ContentType = Json,
                            Data = new { word = Words.All.Exclamations[rnd.Next(Words.All.Exclamations.Length)] }
                        };
                        break;
                    case "word.found.adverb":
                        raisedCloudEvent = new CloudEvent("word.picked.adverb",
                            new Uri(SourceIdentifier))
                        {
                            ContentType = Json,
                            Data = new { word = Words.All.Adverbs[rnd.Next(Words.All.Adverbs.Length)] }
                        };
                        break;
                    case "word.found.pluralnoun":
                        raisedCloudEvent = new CloudEvent("word.picked.pluralnoun",
                            new Uri(SourceIdentifier))
                        {
                            ContentType = Json,
                            Data = new { word = Words.All.Pluralnouns[rnd.Next(Words.All.Pluralnouns.Length)] }
                        };
                        break;
                    case "word.found.adjective":
                        raisedCloudEvent = new CloudEvent("word.picked.adjective",
                            new Uri(SourceIdentifier))
                        {
                            ContentType = Json,
                            Data = new { word = Words.All.Adjectives[rnd.Next(Words.All.Adjectives.Length)] }
                        };
                        break;
                    case "word.found.color":
                        raisedCloudEvent = new CloudEvent("word.picked.color",
                            new Uri(SourceIdentifier))
                        {
                            ContentType = Json,
                            Data = new { word = Words.All.Colors[rnd.Next(Words.All.Colors.Length)] }
                        };
                        break;
                    case "word.found.name":
                        raisedCloudEvent = new CloudEvent("word.picked.name",
                            new Uri(SourceIdentifier))
                        {
                            ContentType = Json,
                            Data = new { word = Words.All.Names[rnd.Next(Words.All.Names.Length)] }
                        };
                        break;
                    case "word.found.animal":
                        raisedCloudEvent = new CloudEvent("word.picked.animal",
                            new Uri(SourceIdentifier))
                        {
                            ContentType = Json,
                            Data = new { word = Words.All.Animals[rnd.Next(Words.All.Animals.Length)] }
                        };
                        break;
                    case "word.found.verbing":
                        raisedCloudEvent = new CloudEvent("word.picked.verbing",
                            new Uri(SourceIdentifier))
                        {
                            ContentType = Json,
                            Data = new { word = Words.All.Verbings[rnd.Next(Words.All.Verbings.Length)] }
                        };
                        break;
                    default:
                        return new HttpResponseMessage(HttpStatusCode.NoContent);
                }

                raisedCloudEvent.GetAttributes().Add("relatedid", receivedCloudEvent.Id);

                HttpClient client = new HttpClient();
                var result = await client.PostAsync(callbackUrl,
                    new CloudEventContent(raisedCloudEvent, rnd.Next(2) == 0 ? ContentMode.Binary : ContentMode.Structured,
                    new JsonEventFormatter()));
                log.LogInformation($"Callback result {result.StatusCode}");
                return new HttpResponseMessage(result.StatusCode);
            }
            catch (Exception e)
            {
                log.LogInformation($"Exception while processing {e.ToString()}" );
                throw;
            }
        }
    }
}