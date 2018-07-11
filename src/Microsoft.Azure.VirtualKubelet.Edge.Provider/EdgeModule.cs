// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.VirtualKubelet.Edge.Provider
{
    using Newtonsoft.Json;

    public class EdgeModule
    {
        [JsonProperty("type")]
        public string Type => "docker";

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("version", NullValueHandling = NullValueHandling.Ignore)]
        public string Version { get; set; }

        [JsonProperty("restartPolicy")]
        public string RestartPolicy { get; set; }

        [JsonProperty("settings")]
        public EdgeModuleSettings Settings { get; set; }
        
        [JsonProperty("env", NullValueHandling = NullValueHandling.Ignore)]
        public IDictionary<string, EdgeModuleEnv> Env;
    }
}
