This is a special directory where the WalletConsoleApi solution expects to find the WalletConsole utility.

**Important:** This repository does not include a prebuilt copy of the utility.

To build and run WalletConsoleApi successfully, you must:
- **Compile the WalletConsole utility yourself** (see instructions below).
- Place the resulting executable into this directory.
- Name it `walletconsole` (this is usually the default).
- On Linux, make it executable (`chmod +x walletconsole`).

## How to build your own copy of the "WalletConsole" utility

**To avoid confusion:** you **can compile** the WalletConsole utility on Windows, but you **cannot run** it on Windows (because it is a Linux binary).

Prerequisites:
- Git and Docker installed
- Docker running
- At least 7GB of free disk space
- Allow up to ~1 hour for full build (depending on your machine's performance)

Steps:
1) Run the appropriate script for your OS:
  - On Windows: `CompileTrustWalletConsole.ps1`
  - On Linux: `CompileTrustWalletConsole.sh`
  - macOS: not tested, but you may try your luck with `CompileTrustWalletConsole.sh`

2) The script will:

- Clone the **latest** revision of Trust Wallet Core into "wallet-core" subdirectory (unless you override by checking out a different version),
- Build a Docker image using the official [Dockerfile](https://github.com/trustwallet/wallet-core/blob/master/Dockerfile):

<img src="../../Screenshots/2025.10.04_Docker%20build%20time.png" alt="Docker build time" style="width:70%; display: block; margin: auto;">

- Run the built Docker image as a temporary container
- Compile the WalletConsole utility inside the temporary container, then copy the resulting executable "outside":

<img src="../../Screenshots/2025.10.04_Compiled%20walletconsole.png" alt="walletconsole executable" style="width:40%; display: block; margin: auto;">

3) After the script completes successfully:
   - Locate the `walletconsole` executable (it should be next to the build scripts by default)
   - If you ran the script from a different place, move the resulting binary **into this directory** and ensure it is named exactly `walletconsole`. 
   - On Linux, make it executable (run `chmod +x walletconsole`).

4) Proceed to build and run the WalletConsoleApi Solution - it should now be able to find the WalletConsole utility.

5) To rebuild from scratch later (for example: if you expect a new version of WalletConsole):
   - Delete the local "wallet-core" subdirectory
   - Remove the local "wallet-core:latest" image from your Docker cache.

> [!WARNING]  
> The "wallet-core:latest" image you build here is **completely separate from and unrelated to any deprecated or outdated images on Docker Hub**.  
> Please do not push your built images to Docker Hub or any public registry.

**PS:** You need not worry about the "wallet-core" directory being created when checking out the Trust Wallet Core repository - `.gitignore` is already configured to ignore it, so Git neither treat it as a submodule, nor include it in commits.

### Notes on the Compile Scripts

While Trust Wallet Core provides a [Dockerfile](https://github.com/trustwallet/wallet-core/blob/master/Dockerfile) to support Docker-based builds, in practice some additional fixes were necessary.  
The included scripts apply the following adjustments:
- `CompileTrustWalletConsole.ps1` handles "CRLF" line endings issues that sometimes occur when cloning on Windows.
- `CompileTrustWalletConsole.sh` ensures proper "executable bit"s is set for "tools" in the build chain (in case they are lost during close on Linux).

These script-level fixes smooth over cross-platform quirks and help ensure reproducible builds.

### Useful links

- [Trust Developers, Wallet Core - Building](https://developer.trustwallet.com/developer/wallet-core/developing-the-library/building)
