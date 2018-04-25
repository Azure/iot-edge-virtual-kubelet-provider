// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.VirtualKubelet.Edge.Provider
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class EdgeHub
    {
        [JsonProperty("schemaVersion")]
        public string SchemaVersion => "1.0";

        [JsonProperty("routes")]
        public IDictionary<string, string> Routes { get; set; }

        [JsonProperty("storeAndForwardConfiguration")]
        public StoreForwardConfiguration StoreForwardConfiguration { get; set; }
    }
}
