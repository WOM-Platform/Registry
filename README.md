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

### Data access via phpMyAdmin

Start and stop the phpMyAdmin service:

```
kubectl create -f phpmyadmin.yaml
kubectl delete -f phpmyadmin.yaml
```

Access available on IP:8081 as shown with command:

```
kubectl get service phpmyadmin
```
