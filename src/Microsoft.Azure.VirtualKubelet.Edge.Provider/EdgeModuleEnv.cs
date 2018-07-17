// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.VirtualKubelet.Edge.Provider
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    public class EdgeModuleEnv
    {
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
