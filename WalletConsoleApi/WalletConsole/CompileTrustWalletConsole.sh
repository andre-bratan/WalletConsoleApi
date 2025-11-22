#!/usr/bin/env bash
# Script to compile Trust Wallet Core Console utility on Linux

set -euo pipefail

SOURCE_REPOSITORY="https://github.com/trustwallet/wallet-core.git"
TARGET_CLONE_DIRECTORY="wallet-core"
TARGET_DOCKER_IMAGE="wallet-core:latest"

# Save the starting directory
START_DIR="$(pwd)"

# A small helper to ensure we return to the start directory no matter what
cleanup() {
  cd "$START_DIR" || true
}
trap cleanup EXIT

# If walletconsole is already present in current directory, skip everything
if [ -f "walletconsole" ]; then
  echo "WalletConsole is already compiled; exiting."
  exit 0
fi

# Check if Git is installed
if ! git --version > /dev/null 2>&1; then
  echo "Git is not installed"
  exit 1
fi

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
  echo "Docker is not running"
  exit 1
fi

# Check if the Docker image already exists locally (without pulling)
if docker image inspect "$TARGET_DOCKER_IMAGE" > /dev/null 2>&1; then
  echo "Docker image '$TARGET_DOCKER_IMAGE' already exists; skipping build."
else
  # Clone repository if needed
  if [ ! -d "$TARGET_CLONE_DIRECTORY" ]; then
    echo "Cloning Trust Wallet Core repository...."
    git clone "$SOURCE_REPOSITORY" "$TARGET_CLONE_DIRECTORY"
    echo "Done."
  else
    echo "Directory '$TARGET_CLONE_DIRECTORY' already exists; skipping clone."
  fi

  # Change into clone directory
  cd "$TARGET_CLONE_DIRECTORY"

  # Fix permissions on Linux :)
  cd tools
  chmod +x *
  cd ../codegen/bin
  chmod +x *
  cd ../..

  echo "Docker image '$TARGET_DOCKER_IMAGE' not found, building..."
  if ! docker build -t "$TARGET_DOCKER_IMAGE" .; then
    echo "Docker image '$TARGET_DOCKER_IMAGE' built successfully."
  else
    echo "Failed to build Docker image '$TARGET_DOCKER_IMAGE'."
    exit 1
  fi

  # Go back to root where the script is supposed to run
  cd "$START_DIR"
fi

# Build the utility via Docker, and copy it back out
docker run --rm --pull=never -v "$PWD":/opt "$TARGET_DOCKER_IMAGE" bash -c "\
  cd build/walletconsole && \
  make && \
  cp walletconsole /opt && \
  exit \
"
