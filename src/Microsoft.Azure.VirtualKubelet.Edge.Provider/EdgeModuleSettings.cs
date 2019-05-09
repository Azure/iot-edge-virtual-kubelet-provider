// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.VirtualKubelet.Edge.Provider
{
    using Newtonsoft.Json;
    using Microsoft.Azure.Devices.Edge.Util;

    [JsonConverter(typeof(EdgeModuleSettingsJsonConverter))]
    public class EdgeModuleSettings
    {
        public EdgeModuleSettings() : this(string.Empty, "{}")
        {
        }

        public EdgeModuleSettings(string image) : this(image, "{}")
        {
        }

        public EdgeModuleSettings(string image, string options)
        {
            this.Image = image;
            this.CreateOptions = options;
        }

        [JsonProperty("image")] public string Image { get; set; }

        [JsonProperty("createOptions")] public string CreateOptions { get; set; }
    }

    class EdgeModuleSettingsJsonConverter : JsonConverter
    {
        private const int TwinValueMaxChunks = 100;
        private const int TwinValueMaxSize = 512;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteStartObject();
            writer.Formatting = Formatting.None;

            var dockerconfig = (EdgeModuleSettings) value;

            writer.WritePropertyName("image");
            serializer.Serialize(writer, dockerconfig.Image);

            var options = JsonConvert.SerializeObject((JsonConvert.DeserializeObject(dockerconfig.CreateOptions)))
                .Chunks(TwinValueMaxSize)
                .Take(TwinValueMaxChunks)
                .Enumerate();
            foreach (var (i, chunk) in options)
            {
                var field = i != 0
                    ? string.Format("createOptions{0}", i.ToString("D2"))
                    : "createOptions";
                writer.WritePropertyName(field);
                writer.WriteValue(chunk);
            }

            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            JObject obj = JObject.Load(reader);

            // Pull out JToken values from json
            obj.TryGetValue("image", StringComparison.OrdinalIgnoreCase, out JToken jTokenImage);

            var options = obj.ChunkedValue("createOptions", true)
                .Take(TwinValueMaxChunks)
                .Select(token => token?.ToString() ?? string.Empty)
                .Join();

            return new EdgeModuleSettings(jTokenImage?.ToString(), options) { };
        }

        public override bool CanConvert(Type objectType) => objectType == typeof(EdgeModuleSettings);
    }
}
