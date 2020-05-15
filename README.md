
# Azure IoT Edge Connector for Kubernetes
Azure IoT Edge Connector leverages the [Virtual Kubelet](https://github.com/virtual-kubelet/virtual-kubelet/blob/master/README.md) project to provide a
virtual Kubernetes node backed by an Azure IoT hub. It translates a Kubernetes
pod specification to an [IoT Edge Deployment](https://docs.microsoft.com/en-us/azure/iot-edge/module-deployment-monitoring) and submits it to the backing IoT hub. The edge deployment contains a device selector query that controls which subset of edge devices the deployment will be applied to.

>*This project does not provide Kubernetes-backed high availability or disaster recovery to IoT Edge deployments. It is about workload deployment to IoT Edge devices using Kubernetes concepts and primitives. The workload itself runs on the edge device(s), and not on the cluster where the IoT Edge connector is installed.

# Architecture


![iot edge connector](/media/iot-edge-connector.png)


The components provided by this project are depicted in the blue boxes in the diagram above. An IoT Edge provider container is spawned alongside the virtual kubelet container in the same pod. This pod instantiates the IoT Edge Connector virtual node.

The IoT Edge provider handles *kubelet* API calls forwarded by the virtual kubelet. It talks to the Azure IoT hub using the Azure IoT SDKs to submit an equivalent container specification in form of an [IoT Edge deployment manifest](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-monitor).

Kubernetes pod annotations and configmaps are used to encode IoT Edge specific information like module routes and device selector query.

# Quick Start

## Prerequisites

* [Azure IoT Hub](https://azure.microsoft.com/en-us/services/iot-hub/)

* A Kubernetes cluster (like [AKS](https://docs.microsoft.com/en-us/azure/aks/kubernetes-walkthrough))

* [kubectl](https://kubernetes.io/docs/tasks/tools/install-kubectl/)

* [Helm 3](https://helm.sh/)

* Clone this repo 

## Install

> *Quickstart instructions assume an AKS cluster setup, but can be easily translated to any Kubernetes cluster.*

1. Create a Kubernetes namespace to house the IoT Edge connector: `kubectl create ns hub0`

1. Create a Kubernetes secret in the namespace to hold the IoT Hub connection string.
   To find the connection string, navigate to your IoT Hub resource in the Azure portal and click on "Shared access policies" and the "iothubowner" will contain your connection string. 
    ```
    kubectl create secret generic my-secrets -n hub0 \
     --from-literal=hub0-cs='<iot-hub-owner-connection-string>'
     ```
    
    If you using kubectl from cmd.exe or PowerShell, use double-quotes around the connection string:
    
    ```
    kubectl create secret generic my-secrets -n hub0 --from-literal=hub0-cs="<iot-hub-owner-connection-string>"
    ```
    
    
1. Use [Helm](https://helm.sh), a Kubernetes package manager, to install the *iot-edge-connector*. Ensure you're using Helm 3.

    ```
    helm install iot-edge-connector-hub0 src/charts/iot-edge-connector --namespace hub0
    ```

    After a few seconds ```kubectl get nodes``` should show ```iot-edge-connector0``` listed.

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

* Creating multiple virtual kubelets (by changing values.yaml in the Helm chart) mapped to different IoT hubs, and scaling the Kubernetes deployment to push the same deployment manifest to edge devices connecting to different hubs. Here is a [demo](https://www.youtube.com/watch?v=p-R2mV7Bxuk) of this use case.

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
