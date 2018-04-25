// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.VirtualKubelet.Edge.Provider
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class EdgeAgent
    {
        [JsonProperty("schemaVersion")]
        public string SchemaVersion => "1.0";

        [JsonProperty("runtime")]
        public EdgeRuntime Runtime { get; set; }

        [JsonProperty("systemModules")]
        public IDictionary<string, EdgeModule> SystemModules { get; set; }

        [JsonProperty("modules")]
        public IDictionary<string, EdgeModule> Modules { get; set; }
    }
}
