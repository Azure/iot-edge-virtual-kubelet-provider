// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.VirtualKubelet.Edge.Provider
{
    using Newtonsoft.Json;

    public class StoreForwardConfiguration
    {
        [JsonProperty("timeToLiveSecs")]
        public int TimeToLiveSecs { get; set; }
    }
}
