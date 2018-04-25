// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.VirtualKubelet.Server.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using k8s.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.VirtualKubelet.Types;
    using Microsoft.Extensions.Logging;

    [Route("/")]
    public class VirtualKubeletProviderController : Controller
    {
        readonly IProvider provider;
        readonly ILogger logger;

        public VirtualKubeletProviderController(IProvider provider, ILogger<VirtualKubeletProviderController> logger)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            this.provider = provider;
            this.logger = logger;
        }

        [HttpGet]
        [Route("")]
        public string Index() => $"Welcome to {this.provider.Name}";

        [HttpPost("{pod}")]
        [Route("/createPod")]
        public Task<ActionResult> CreatePod([FromBody]Corev1Pod pod) =>
            this.DoAction(Event.CreatePod, () => this.provider.CreatePodAsync(pod));

        [HttpPut("{pod}")]
        [Route("/updatePod")]
        public Task<ActionResult> UpdatePod([FromBody]Corev1Pod pod) =>
            this.DoAction(Event.UpdatePod, () => this.provider.UpdatePodAsync(pod));

        [HttpDelete("{pod}")]
        [Route("/deletePod")]
        public Task<ActionResult> DeletePod([FromBody]Corev1Pod pod) =>
            this.DoAction(Event.DeletePod, () => this.provider.DeletePodAsync(pod));

        [HttpGet]
        [Route("/getPod")]
        public async Task<ActionResult> GetPod([FromQuery(Name = "namespace")]string ns, string name)
        {
            Corev1Pod pod = await this.provider.GetPodAsync(ns, name);
            if (pod == null)
            {
                return new StatusCodeResult(404);
            }

            return new JsonResult(pod);
        }

        [HttpGet]
        [Route("/getContainerLogs")]
        public async Task<string> GetContainerLogs([FromQuery(Name = "namespace")]string ns, string podName, string containerName, int tail) =>
            await this.provider.GetContainerLogsAsync(ns, podName, containerName, tail);

        [HttpGet]
        [Route("/getPodStatus")]
        public async Task<ActionResult> GetPodStatus([FromQuery(Name = "namespace")]string ns, string name)
        {
            Corev1PodStatus status = await this.provider.GetPodStatusAsync(ns, name);
            if (status == null)
            {
                return new StatusCodeResult(404);
            }

            return new JsonResult(status);
        }

        [HttpGet]
        [Route("/getPods")]
        public async Task<IEnumerable<Corev1Pod>> GetPods() => await this.provider.GetPodsAsync();

        [HttpGet]
        [Route("/capacity")]
        public async Task<IDictionary<string, string>> Capacity() => await this.provider.CapacityAsync();

        [HttpGet]
        [Route("/nodeConditions")]
        public async Task<IEnumerable<Corev1NodeCondition>> NodeConditions() => await this.provider.NodeConditionsAsync();

        [HttpGet]
        [Route("/nodeAddresses")]
        public async Task<IEnumerable<Corev1NodeAddress>> NodeAddresses() => await this.provider.NodeAddressesAsync();

        [HttpGet]
        [Route("/nodeDaemonEndpoints")]
        public async Task<Corev1NodeDaemonEndpoints> NodeDaemonEndpoints() => await this.provider.NodeDaemonEndpointsAsync();

        [HttpGet]
        [Route("/operatingSystem")]
        public async Task<string> OperatingSystem() => await this.provider.OperatingSystemAsync();

        async Task<ActionResult> DoAction(Event evt, Func<Task> action)
        {
            try
            {
                await action();
                return new OkResult();
            }
            catch (Exception ex)
            {
                this.logger.LogError((int)evt, ex, "Error");
                return new StatusCodeResult(500);
            }
        }
    }

    enum Event
    {
        CreatePod = 100,
        UpdatePod = 200,
        DeletePod = 300
    }
}
