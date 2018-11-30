// Copyright (c) Microsoft Corporation
// Licensed under the Apache 2.0 license.
// See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.CloudEventsDec18
{
    using System.IO;
    using System.Text;
    using Newtonsoft.Json;

    public class Words
    {
        static Words()
        {
            JsonSerializer js = new JsonSerializer();
            All = js.Deserialize<Words>(new JsonTextReader(new StreamReader(new MemoryStream(Resource.words), Encoding.UTF8, true)));
        }

        public static Words All { get; }

        [JsonProperty(PropertyName = "adjective")]
        public string[] Adjectives { get; set; }

        [JsonProperty(PropertyName = "adverb")]
        public string[] Adverbs { get; set; }

        [JsonProperty(PropertyName = "exclamation")]
        public string[] Exclamations { get; set; }

        [JsonProperty(PropertyName = "noun")]
        public string[] Nouns { get; set; }

        [JsonProperty(PropertyName = "pluralnoun")]
        public string[] Pluralnouns { get; set; }

        [JsonProperty(PropertyName = "verb")]
        public string[] Verbs { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string[] Names { get; set; }

        [JsonProperty(PropertyName = "animal")]
        public string[] Animals { get; set; }

        [JsonProperty(PropertyName = "verbing")]
        public string[] Verbings { get; set; }

        [JsonProperty(PropertyName = "color")]
        public string[] Colors { get; set; }
    }
}