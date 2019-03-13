// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.VirtualKubelet.Edge.Provider
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using k8s;
    using k8s.Models;
    using Microsoft.Azure.Devices;
    using Microsoft.Azure.Devices.Shared;
    using Microsoft.Azure.VirtualKubelet.Types;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    public class EdgeProvider : IProvider
    {
        readonly string connectionString;
        readonly RegistryManager registryManager;
        readonly ConcurrentDictionary<string, Corev1Pod> podsMap = new ConcurrentDictionary<string, Corev1Pod>();
        readonly IKubernetes kubeClient;

        public EdgeProvider(IKubernetes kubeClient)
        {
            if (kubeClient == null)
            {
                throw new ArgumentNullException(nameof(kubeClient));
            }
            this.kubeClient = kubeClient;

            this.connectionString = Environment.GetEnvironmentVariable("HUB_CONNECTION_STRING");
            if (string.IsNullOrEmpty(this.connectionString.Trim()))
            {
                throw new ArgumentNullException("HUB_CONNECTION_STRING");
            }

            this.registryManager = RegistryManager.CreateFromConnectionString(this.connectionString);
        }

        public string Name => "Azure IoT Edge Virtual Kubelet Provider";

        public Task<IDictionary<string, string>> CapacityAsync()
        {
            // Pod anti-affnity isn't working, likely because pod status 
            // is not correctly reported back from the provider. This 
            // causes multiple workload pods to be scheduled on the node 
            // when scaling replicas. 
            //
            // Workaround this by setting pods capacity to 3. Two infra
            // pods are always scheduled on a node, leaving room for only
            // one workload pod effectively preventing the unwanted pod
            // scheduling behavior.
            //
            // TODO: revisit after fixing pod status reporting.
            // 
            return Task.FromResult(new Dictionary<string, string>
            {
                { "cpu", "20" },
                { "memory", "100Gi" },
                { "pods", "3" }
            } as IDictionary<string, string>);
        }

        void PopulateModuleConfig(Corev1ConfigMapList configMapList, string moduleName, EdgeModule module)
        {
            // see if there is an entry for this module in the config maps
            Corev1ConfigMap configMap = configMapList.Items.Where(cm => cm.Metadata.Name == moduleName).FirstOrDefault();
            if (configMap != null && configMap.Data != null)
            {
                    if (configMap.Data.TryGetValue("restartPolicy", out string configMapRestartPolicy))
                    {
                        module.RestartPolicy = configMapRestartPolicy ?? "on-unhealthy";
                    }
                    else
                    {
                        module.RestartPolicy = "on-unhealthy";
                    }
                    if (configMap.Data.TryGetValue("version", out string configMapVersion))
                    {
                        module.Version = configMapVersion ?? "1.0";
                    }
                    else
                    {
                        module.Version = "1.0";
                    }
                    if (configMap.Data.TryGetValue("status", out string configMapStatus))
                    {
                        module.Status = configMapStatus ?? "running";
                    }
                    else
                    {
                        module.Status = "running";
                    }
                    if (configMap.Data.TryGetValue("createOptions", out string configMapCreateOptions))
                    {
                        module.Settings.CreateOptions = configMapCreateOptions ?? "{}";
                    }
                    else
                    {
                        module.Settings.CreateOptions = "{}";
                    }
            }
            else
            {
                    module.RestartPolicy = "on-unhealthy";
                    module.Version = "1.0";
                    module.Status = "running";
                    module.Settings.CreateOptions = "{}";
            }
        }

        void PopulateEdgeHubConfig(Corev1ConfigMapList configMapList, EdgeHub edgeHub)
        {
            Corev1ConfigMap configMap = configMapList.Items.Where(cm => cm.Metadata.Name == "edgehub").FirstOrDefault();
            if (configMap != null && configMap.Data.TryGetValue("desiredProperties", out string desiredPropsJson))
            {
                EdgeHub edgeHub2 = JsonConvert.DeserializeObject<EdgeHub>(desiredPropsJson);
                if (edgeHub2.Routes != null)
                {
                    edgeHub.Routes = edgeHub2.Routes;
                }
                if (edgeHub2.StoreForwardConfiguration != null)
                {
                    edgeHub.StoreForwardConfiguration = edgeHub2.StoreForwardConfiguration;
                }
            }
        }
        
        void PopulateEdgeAgentConfig(Corev1ConfigMapList configMapList, EdgeAgent edgeAgent)
        {
            Corev1ConfigMap configMap = configMapList.Items.Where(cm => cm.Metadata.Name == "edgeagent").FirstOrDefault();
            if (configMap != null && configMap.Data.TryGetValue("desiredProperties", out string desiredPropsJson))
            {
                EdgeAgent edgeAgent2 = JsonConvert.DeserializeObject<EdgeAgent>(desiredPropsJson);
                if (edgeAgent2.Runtime != null)
                {
                    if (edgeAgent2.Runtime.Settings.RegistryCredentials != null)
                    {
                        edgeAgent.Runtime.Settings.RegistryCredentials = edgeAgent2.Runtime.Settings.RegistryCredentials;
                    }
                }
                if (edgeAgent2.SystemModules != null)
                {
                    if (edgeAgent2.SystemModules.ContainsKey("edgeHub"))
                    {
                        edgeAgent.SystemModules["edgeHub"].Env = edgeAgent2.SystemModules["edgeHub"].Env;
                    }
                }
            }
        }

        private IList<(string Name, JObject Twin)> PopulateModuleTwins(Corev1ConfigMapList configMapList, ICollection<string> moduleNames)
        {
            var moduleTwins = new List<(string Name, JObject Twin)>();
            foreach (string moduleName in moduleNames)
            {
                Corev1ConfigMap configMap = configMapList.Items.Where(cm => cm.Metadata.Name == moduleName).FirstOrDefault();
                if (configMap != null && configMap.Data != null)
                {
                    if(configMap.Data.TryGetValue("desiredProperties", out string desiredPropsJson))
                    {
                        moduleTwins.Add((moduleName, JObject.Parse(desiredPropsJson)));
                    }
                }
            }

            return moduleTwins;
        }

        public async Task CreatePodAsync(Corev1Pod pod)
        {
            if (this.IsEdgeDeployment(pod))
            {
                // get list of config maps in this namespace
                Corev1ConfigMapList configMapList = await this.kubeClient.ListNamespacedConfigMapAsync(pod.Metadata.NamespaceProperty);

                // build configuration
                var configuration = new Configuration(pod.Metadata.Name)
                {
                    TargetCondition = pod.Metadata.Annotations["targetCondition"],
                    Priority = ParseInt32(pod.Metadata.Annotations["priority"], 10),
                    Labels = new Dictionary<string, string>(),
                    Content = new ConfigurationContent
                    {
                        ModuleContent = new Dictionary<string, TwinContent>()
                    }
                };

                // copy over the labels set on the pod as deployment labels
                foreach (KeyValuePair<string, string> label in pod.Metadata.Labels)
                {
                    configuration.Labels.Add(label.Key, label.Value);
                }

                EdgeAgent edgeAgent = this.GetDefaultEdgeAgentConfig();
                this.PopulateEdgeAgentConfig(configMapList, edgeAgent);                
                foreach (Corev1Container container in pod.Spec.Containers)
                {
                    var module = new EdgeModule
                    {
                        Settings = new EdgeModuleSettings
                        {
                            Image = container.Image
                        }
                    };
                    this.PopulateModuleConfig(configMapList, container.Name, module);
                    edgeAgent.Modules.Add(container.Name, module);
                }

                EdgeHub edgeHub = this.GetDefaultEdgeHubConfig();
                this.PopulateEdgeHubConfig(configMapList, edgeHub);

                // create module twins
                IList<(string Name, JObject Twin)> moduleTwins = this.PopulateModuleTwins(configMapList, edgeAgent.Modules.Keys);

                // now we have everything we need to build the configuration content
                AddModuleTwin(configuration, "$edgeAgent", edgeAgent);
                AddModuleTwin(configuration, "$edgeHub", edgeHub);
                foreach ((string Name, JObject Twin) in moduleTwins)
                {
                    AddModuleTwin(configuration, Name, Twin);
                }

                await this.registryManager.AddConfigurationAsync(configuration);
            }

            pod.Status = pod.Status ?? new Corev1PodStatus();
            pod.Status.Phase = "Running";
            pod.Status.Conditions = new List<Corev1PodCondition>
            {
                new Corev1PodCondition("True", "Initialized", null, DateTime.UtcNow),
                new Corev1PodCondition("True", "Ready", null, DateTime.UtcNow),
                new Corev1PodCondition("True", "PodScheduled", null, DateTime.UtcNow)
            };
            this.podsMap.AddOrUpdate(pod.Metadata?.Name ?? string.Empty, pod, (k, v) => pod);
        }

        private static void AddModuleTwin<T>(Configuration configuration, string key, T twinObject)
        {
            configuration.Content.ModuleContent[key] = new TwinContent()
            {
                TargetContent = new TwinCollection(JsonConvert.SerializeObject(twinObject))
            };
        }

        EdgeAgent GetDefaultEdgeAgentConfig() => new EdgeAgent
        {
            Runtime = new EdgeRuntime
            {
                Settings = new EdgeRuntimeSettings
                {
                    MinDockerVersion = "v1.25",
                    LoggingOptions = "",
                    RegistryCredentials = new Dictionary<string, RegistryCredentialsSettings>
                    {

                    }
                }
            },
            SystemModules = new Dictionary<string, EdgeModule>
                {
                    {
                        "edgeAgent",
                        new EdgeModule
                        {
                            Status = "running",
                            RestartPolicy = "always",
                            Settings = new EdgeModuleSettings
                            {
                                Image = "mcr.microsoft.com/azureiotedge-agent:1.0",
                                CreateOptions = "{}"
                            }
                        }
                    },
                    {
                        "edgeHub",
                        new EdgeModule
                        {
                            Status = "running",
                            RestartPolicy = "always",
                            Settings = new EdgeModuleSettings
                            {
                                Image = "mcr.microsoft.com/azureiotedge-hub:1.0",
                                CreateOptions = "{}"
                            },
                            Env = new Dictionary<string, EdgeModuleEnv>
                            {

                            }
                        }
                    }
                },
            Modules = new Dictionary<string, EdgeModule>()
        };

        EdgeHub GetDefaultEdgeHubConfig() => new EdgeHub
        {
            Routes = new Dictionary<string, string>
                {
                    { "route", "FROM /* INTO $upstream" }
                },
            StoreForwardConfiguration = new StoreForwardConfiguration
            {
                TimeToLiveSecs = 7200
            }
        };

        bool IsEdgeDeployment(Corev1Pod pod) => pod.Metadata.Annotations.TryGetValue("isEdgeDeployment", out string isEdgeDeployment) && isEdgeDeployment == "true";

        int ParseInt32(string val, int defaultValue)
        {
            if (Int32.TryParse(val, out int ival))
            {
                return ival;
            }

            return defaultValue;
        }

        public Task DeletePodAsync(Corev1Pod pod)
        {
            this.podsMap.Remove(pod.Metadata?.Name ?? string.Empty, out Corev1Pod _);
            return Task.CompletedTask;
        }

        public Task<string> GetContainerLogsAsync(string ns, string podName, string containerName, int tail)
        {
            return Task.FromResult($"GetContainerLogs() - ns = {ns}, podName = {podName}, containerName = {containerName}, tail = {tail}");
        }

        public Task<Corev1Pod> GetPodAsync(string ns, string name)
        {
            if (this.podsMap.TryGetValue(name, out Corev1Pod pod) && pod.Metadata?.NamespaceProperty == ns)
            {
                return Task.FromResult(pod);
            }

            return Task.FromResult(null as Corev1Pod);
        }

        public Task<IEnumerable<Corev1Pod>> GetPodsAsync()
        {
            return Task.FromResult(this.podsMap.Values as IEnumerable<Corev1Pod>);
        }

        public Task<Corev1PodStatus> GetPodStatusAsync(string ns, string name)
        {
            if (this.podsMap.TryGetValue(name, out Corev1Pod pod) && pod.Metadata?.NamespaceProperty == ns)
            {
                return Task.FromResult(pod.Status);
            }

            return Task.FromResult(null as Corev1PodStatus);
        }

        public Task<IEnumerable<Corev1NodeAddress>> NodeAddressesAsync()
        {
            return Task.FromResult(new Corev1NodeAddress[] { } as IEnumerable<Corev1NodeAddress>);
        }

        public Task<IEnumerable<Corev1NodeCondition>> NodeConditionsAsync()
        {
            return Task.FromResult(new Corev1NodeCondition[]
            {
                new Corev1NodeCondition("True", "Ready", DateTime.UtcNow, DateTime.UtcNow, ".NET Runs", "KubeletReady")
            } as IEnumerable<Corev1NodeCondition>);
        }

        public Task<Corev1NodeDaemonEndpoints> NodeDaemonEndpointsAsync()
        {
            return Task.FromResult(null as Corev1NodeDaemonEndpoints);
        }

        public Task<string> OperatingSystemAsync()
        {
            return Task.FromResult("Linux");
        }

        public Task UpdatePodAsync(Corev1Pod pod) => this.CreatePodAsync(pod);
    }
}
