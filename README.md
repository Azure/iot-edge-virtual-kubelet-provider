
# Azure IoT Edge Connector for Kubernetes
Azure IoT Edge Connector leverages the [Virtual Kubelet](https://github.com/virtual-kubelet/virtual-kubelet/blob/master/README.md) project to provide a
virtual Kubernetes node backed by an Azure IoT hub. It translates a Kubernetes
pod specification to an [IoT Edge Deployment](https://docs.microsoft.com/en-us/azure/iot-edge/module-deployment-monitoring) and submits it to the backing IoT hub. The edge deployment contains a device selector query that controls which subset of edge devices the deployment will be applied to.

>*This project does not provide Kubernetes-backed high availability or disaster recovery to IoT Edge deployments. It is about software deployment and management of the edge devices using Kubernetes concepts and primitives. Ingress to the edge device is not controlled by the Kubernetes load balancer.*

# Architecture


![iot edge connector](/media/iot-edge-connector.png)


The components provided by this project are depicted in the blue boxes in the diagram above. An IoT Edge provider container is spawned alongside the virtual kubelet container in the same pod. This pod instantiates the IoT Edge Connector virtual node.

The IoT Edge provider handles the kubelet API calls forwared to it by the virtual kubelet. It talks to the Azure IoT hub using the Azure IoT SDKs to submit an equivalent container specification in the form of a edge deployment manifest.

Kubernetes pod annotations and configmaps are used to encode IoT Edge specific information like module routes and device selector query.

# Quick Start

## Prerequisites

* [Azure IoT Hub](https://azure.microsoft.com/en-us/services/iot-hub/)

* A Kubernetes cluster (like [AKS](https://docs.microsoft.com/en-us/azure/aks/kubernetes-walkthrough))

* [kubectl](https://kubernetes.io/docs/tasks/tools/install-kubectl/)

* [Helm](https://github.com/kubernetes/helm)

* Clone this repo 

## Install

1. Create a Kubernetes secrets store to hold the IoT Hub connection string.
   To find the connection string, navigate to your IoT Hub resource in the Azure portal and click on "Shared access policies" and the "iothubowner" will contain your connection string. 
    ```
    kubectl create secret generic my-secrets \
     --from-literal=hub0-cs='<iot-hub-owner-connection-string>'
    ```
    > Add a new ```--from-literal``` entry if you want to store multiple keys
    
1. Use [Helm](https://github.com/kubernetes/helm), a Kubernetes package manager, to install the *iot-edge-connector*
    ```
    helm install -n hub0 src/charts/iot-edge-connector
    ```
    After a few seconds ```kubectl get nodes``` should show ```iot-edge-connector0``` listed.
    
    Use the following command to install the *iot-edge-connector* on Kubernetes clusters using RBAC
    ```
    helm install -n hub0 --set rbac.install=true src/charts/iot-edge-connector
    ```

1. Submit the sample Kubernetes deployment.
    ```
    kubectl apply -f \
     src/Microsoft.Azure.VirtualKubelet.Edge.Provider/sample-deployment.yaml
    ```
    >*The sample deployment contains the simulated temperature sensor container. You can use it as a example to create your own deployment.*    

    In a few seconds, you should see the deployment show up the IoT Hub portal under IoT Edge Deployments. Example screenshot below:

    ![tempsensor deployment](/media/tempsensor-deployment.png)

Connected edge devices targetted by the deployment will get the new deployment manifest applied within 5 minutes!

# More use cases

There are more interesting use cases for this project like:

* Using a single Kubernetes deployment that controls your cloud-side and device-side software configuration. 

* Creating multiple virtual kubelets (by changing values.yaml in the Helm chart) mapped to different IoT hubs, and scaling the Kubernetes deployment to push the same deployment manifest to edge devices connecting to different hubs.

Please give us feedback on how the tool is working for you by tweeting us at [@MicrosoftIoT](https://twitter.com/MicrosoftIoT), as well as any feature requests at [Azure IoT Edge Feedback](https://feedback.azure.com/forums/907045-azure-iot-edge/) or GitHub issues for this repo.


# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
