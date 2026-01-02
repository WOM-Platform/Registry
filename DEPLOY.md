# Deploy guide through Google Cloud

## Host configuration

### Tools

The host must be configured with the source repository:

```
git clone https://github.com/WOM-Platform/Registry.git TARGET
```

The GCloud CLI tools must be installed:

```
sudo apt-get install apt-transport-https ca-certificates gnupg curl
curl https://packages.cloud.google.com/apt/doc/apt-key.gpg | sudo gpg --dearmor -o /usr/share/keyrings/cloud.google.gpg
echo "deb [signed-by=/usr/share/keyrings/cloud.google.gpg] https://packages.cloud.google.com/apt cloud-sdk main" | sudo tee -a /etc/apt/sources.list.d/google-cloud-sdk.list
sudo apt-get update && sudo apt-get install google-cloud-cli
```

Initialize GCloud and select the correct project:

```
gcloud init
```

The host must be able to run Makefile:

```
sudo apt-get install build-essential
```

### Project configuration

Configure the `config.env` file with all configuration parameters and overrides needed to run the project.
Also, set the correct JSON keys in the `/keys` directory in order to let the Registry authenticate with Google Cloud.

The Docker host must be able to download images from the Google Cloud artifact repository:

```
gcloud auth configure-docker eu.gcr.io
```

## Google Cloud Build configuration

The Google Cloud Build script is contained in the `cloudbuild.yaml` file.

Setup a Google Cloud Build trigger using the file as a template. Override the parameters, supplying the server host (`_WOM_PROD_SERVER_HOST`), the Linux username running the build locally on the host (`_WOM_PROD_SERVER_USER`) and the full absolute path to the source code on the host (`_WOM_PROD_SERVER_REGISTRY_PWD`).

You must upload a valid SSH private key as a Google Cloud Secret (`WOM_PROD_SERVER_SSH_KEY`), that is configured to access the host.
