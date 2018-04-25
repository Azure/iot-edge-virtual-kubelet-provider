// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.VirtualKubelet.Types;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.VirtualKubelet.Server
{
    public class Server
    {
        public void Start(IProvider provider)
        {
            BuildWebHost(provider).Run();
        }

        IWebHost BuildWebHost(IProvider provider) =>
            WebHost.CreateDefaultBuilder()
                .UseUrls("http://*:5000")
                .ConfigureServices(services => services.Add(new ServiceDescriptor(typeof(IProvider), provider)))
                .UseStartup<Startup>()
                .Build();
    }
}
