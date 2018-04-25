// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.VirtualKubelet.Types
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using k8s.Models;

    public interface IProvider
    {
        string Name { get; }
        Task CreatePodAsync(Corev1Pod pod);
        Task UpdatePodAsync(Corev1Pod pod);
        Task DeletePodAsync(Corev1Pod pod);
        Task<Corev1Pod> GetPodAsync(string ns, string name);
        Task<string> GetContainerLogsAsync(string ns, string podName, string containerName, int tail);
        Task<Corev1PodStatus> GetPodStatusAsync(string ns, string name);
        Task<IEnumerable<Corev1Pod>> GetPodsAsync();
        Task<IDictionary<string, string>> CapacityAsync();
        Task<IEnumerable<Corev1NodeCondition>> NodeConditionsAsync();
        Task<IEnumerable<Corev1NodeAddress>> NodeAddressesAsync();
        Task<Corev1NodeDaemonEndpoints> NodeDaemonEndpointsAsync();
        Task<string> OperatingSystemAsync();
    }
}
