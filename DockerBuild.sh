#!/bin/bash
# Script to build Docker image of WalletConsoleApi application

DOCKER_REPOSITORY="localhost"
TARGET_IMAGE="walletconsoleapi"
TARGET_TAG="latest"

set -euo pipefail

if [ ! -f "./WalletConsoleApi/WalletConsole/walletconsole" ]; then
  echo "WalletConsole utility is missing - you have to compile it by yourself first"
  exit 1
fi

docker build -t "${DOCKER_REPOSITORY}/${TARGET_IMAGE}:${TARGET_TAG}" .
