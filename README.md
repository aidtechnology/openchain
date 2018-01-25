# Openchain AID:Tech Implementation

Openchain is an open source distributed ledger technology. It is suited for organizations wishing to issue and manage digital assets in a robust, secure and scalable way. Visit [openchain.org](https://www.openchain.org/) for more information.

The full documentation for Openchain is available at [docs.openchain.org](https://docs.openchain.org/).

## Installation

The AID:Tech implementation of Openchain features a standalone "Dockerfile" that can be used to build a Docker image and this can be deployed directly onto a Virtual Machine or through an orchestration service (e.g. Kubernetes).

The current Dockerfile does not include the editing of the required config.js file in src/Openchain/data/config.json. If deploying in a VM directly, you will wish to edit this file to set up configuration as detailed in the official Openchain [installation guide](https://docs.openchain.org/en/latest/general/docker-deployment.html).

If using (e.g.) Kubernetes, the config.json file should be mounted as a [secret](https://kubernetes.io/docs/concepts/configuration/secret/) and you will want to set up a Kubernetes service to expose the container on either the 80 (HTTP) or 443 (HTTPS) port.

## Client

A reference client implementation is available at [wallet.openchain.org](https://wallet.openchain.org/). The source code is available on [GitHub](https://github.com/openchain/wallet).

## License

Copyright 2018 Aid Technology Ltd.; 2015 Coinprism, Inc.

Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and limitations under the License.
