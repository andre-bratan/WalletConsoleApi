#!/bin/bash
# Script to run Docker image of WalletConsoleApi application

DOCKER_REPOSITORY="localhost"
TARGET_IMAGE="walletconsoleapi"
TARGET_TAG="latest"

docker run --rm --name walletconsoleapi -e ASPNETCORE_URLS="http://+:8080;" -p 8080:8080 "${DOCKER_REPOSITORY}/${TARGET_IMAGE}:${TARGET_TAG}"
