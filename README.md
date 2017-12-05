# WOM platform

Coming soon.

## Installation

Persistent disk installation:

```
gcloud compute disks create --size 200GB mysql-disk
```

MySQL database secret:

```
kubectl create secret generic mysql --from-literal=password=YOUR_PASSWORD
```

Deploy Kubernetes manifests:

```
kubectl create -f mysql.yaml
kubectl create -f web-api.yaml
```
