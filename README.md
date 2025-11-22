# The API Wrapper for Trust Wallet Core's "WalletConsole"

<img src="Screenshots/2025.11.22_WalletConsole%20API.png" alt="Web API page" style="width:60%; display: block; margin: auto;">

Inspired by [Trust Developers, Integration Guide - Server-Side](https://developer.trustwallet.com/developer/wallet-core/integration-guide/server-side) document.

This project aims to bridge the gap left by the absence of a Web API for the “Trust Wallet Core” library. It does so in the simplest possible way: by wrapping the “WalletConsole” utility with a set of REST API endpoints.

**Features:**
- **REST API** for major "WalletConsole" utility's commands
- **Swagger** documentation for all the endpoints

**Compatible WalletConsole versions:**
- v4.3.13 (commit: [6d1a0ed42e9a54e666c6b20f971bee3763a53f65](https://github.com/trustwallet/wallet-core/commit/6d1a0ed42e9a54e666c6b20f971bee3763a53f65))

> [!IMPORTANT]  
> This repository does not include a ready-built WalletConsole utility from the [Trust Wallet Core](https://github.com/trustwallet/wallet-core) project - you'll have to compile it yourself. This approach aligns with best practices for blockchain-related software (verify and audit yourself, don't trust third-party binaries).  
> You will, however, find helpful scripts in the "[WalletConsole](WalletConsoleApi/WalletConsole)" directory to assist you in building your own copy of the utility.  
> **Without the `walletconsole` executable placed alongside these scripts you'll see a warning on build, the project's tests will fail!**

> [!WARNING]  
> **Disclaimer:** This project is a simple proof of concept and is not optimized for performance. It will not reliably run on slow machines or systems under a heavy CPU load (the implementation relies on timer-based logic to manage some errors and edge cases).  
> Do not use it in production.  
> And by all means, do not expose its API to the public Internet!

### Technical challenges solved
- Compiling `walletconsole` utility :)
- Managing communication with `walletconsole` and handling state transitions.
- Packing everything in a Docker container
- **Exposing `walletconsole` functionality through the REST API**.

### Glossary

You'll not be confused if you distinguish:
- [Trust Wallet](https://trustwallet.com) - a secure, noncustodial crypto wallet that allows users to store, manage, send, and receive over 10 million cryptocurrencies and NFTs across 100+ blockchains, including support for decentralized applications (dApps) through its mobile app and browser extension. 
- [Trust Wallet Core](https://github.com/trustwallet/wallet-core) - a **core part of the Trust Wallet**. An open-source, cross-platform, mobile-focused library implementing low-level cryptographic wallet functionality for a high number of blockchains.
- [WalletConsole](https://github.com/trustwallet/wallet-core/tree/master/walletconsole) - an interactive command-line utility for accessing key and address management functionality of the Trust Wallet Core library. It is included in the Trust Wallet Core repository.
- `walletconsole` (when writen in lower case) - the WalletConsole utility **executable**

### How does it work under the hood?

The "WalletConsole" utility maintains an internal state (e.g., selected coin, mnemonic phrase, etc.).
The presence of this internal "context" conflicts with the "stateless nature" of REST APIs.

To bridge this gap:
- Each incoming web request spawns a new instance of the `walletconsole` executable.
- That new process is configured - its internal state is set - by sending appropriate commands via StdIn
- The process responds via StdOut, collected by the WalletConsoleApi wrapper.
- The wrapper may perform multiple rounds of request/response with that process to satisfy the web request.
- Finally, the wrapper merges or formats those outputs and returns a response to the client.

**More technical details:**

- WalletConsoleApi is an ASP.NET Core Minimal API application 
- It uses Swagger to document the API exposed to clients
- For each incoming HTTP request, it runs a separate instance of the `walletconsole` executable
- It attaches to the process’s standard IO streams (StdIn, StdOut, StdErr) to drive the utility and capture its output

**Even more technical details:**

- The core logic is driven by a `StateMachine` component:
  - It maintains the internal context of the `walletconsole` session (e.g., selected coin, phrase).
  - It defines allowed transitions between `States` as part of predefined `Flows`, essentially mapping out how interactions progress.
- A dedicated `ProcessListener`:
  - Starts a new `walletconsole` process per request.
  - Attaches to StdIn, StdOut, and StdErr for bidirectional communication.
  - Reads StdOut and StdErr asynchronously.
  - ⚠️ Although `walletconsole` generally does not output anything to StdErr, any content received on this stream is treated as an error.
- For each command sent to the `walletconsole` process:
  - The `ProcessListener` posts one or more commands via StdIn.
  - It waits for a trigger line signaling that the command’s output is complete.
  - Meanwhile, it buffers all data coming from StdOut.
- A "watchdog" timer within `ProcessListener` handles edge cases:
  - If the expected trigger line is missing or an unexpected delay occurs, it makes `ProcessListener` flush the buffered output to the `StateMachine`.
  - This typically results in an "UnexpectedError", but avoids hanging or resource locking.
  - The "watchdog" also helps catch empty responses (e.g., issuing a "dumpXpub" command for an unsupported coin like Solana).
- The buffered output is passed to the `StateMachine`, which:
  - Parses and analyzes it.
  - Prepares for the next state or step in the flow.
  - Produces an intermediary result.
- The Minimal API endpoints:
  - Define the ordered sequence of commands (i.e., the "flow") necessary to fulfill a specific request.
  - Interact with the `StateMachine` to await for each "step" completion.
  - Accumulate intermediary results.
  - Merge and return the final response to the client.

Glossary:
- Flow - a sequence of ordered States required to: 1) configure the `walletconsole` context and 2) get the result. In other words, a Flow defines the valid transitions between States.
- State - a representation of the current status of the managed `walletconsole` process (e.g., what context has been set, what step is expected next).
- Command - a text line sent to the `walletconsole` process (via StdIn) to invoke an action or query.
- Intermediary result - one or multiple lines of text output by `walletconsole` (via StdOut) in response to a Command, before the overall Flow is complete.
- Trigger line - a special output line (or containing a special marker) that signals the end of the Intermediary result (i.e., that the response to the Command is complete).

### Development

> [!WARNING]  
> **Platform Note:** This project is designed to run on Linux.  
> Do not attempt to run it directly on Windows - the `walletconsole` binary is a Linux-only executable and will not start.  
> However, Windows users can work around this. See suggestions below.

This project was originally developed on a Windows machine using JetBrains Rider (v2025) connected to an Ubuntu Server (v24.04.3 LTS) virtual machine running on Hyper-V (Windows 11 Pro feature).
This setup was chosen to "shift" development to a Linux environment, as the `walletconsole` utility only runs on Linux.
While it may sound complex, this arrangement offered fast feedback loops and streamlined testing.

You are free to use a much simpler setup, as Visual Studio Code running on/in:
- Native Linux
- A Docker container (development in Docker approach)
- A DevContainer (not pre-configured in this repository)

Requirements:
- Git
- Docker
- .Net 8 SDK

Before building and running the project (and its tests), **you must compile the `walletconsole` utility first**.  
See the [WalletConsole](WalletConsoleApi/WalletConsole) directory for scripts to help with this step.

**Troubleshooting:**  

- 500 "Internal Server Error"s reported in logs or Integration Tests usually mean the `walletconsole` is not found or **is not executable** (check the "executable bit" - run `chmod +x walletconsole`). 

### How to run?

- Compile the `walletconsole` utility (see above).
- Build the project in Docker using one of `DockerBuild.*` scripts.
- Run the project in Docker using one of `DockerRun.*` scripts.  
- By default, the application starts on port 8080, so navigate to `http://localhost:8080` to see the Swagger UI.  

Example Docker compose is something like:
```yaml
services:
  wallet-console-api:
    image: localhost/walletconsoleapi:latest
    mem_limit: 512g
    environment:
      - ASPNETCORE_URLS=http://+:8080; # Note: there are no quotes around the value
    ports:
      - 6055:8080
    restart: unless-stopped
    # if you want to harden it:
    user: app:1654 # the default user for a dockerized .NET application is "app" (UID = 1654)
    cpu_shares: 768
    security_opt:
      - no-new-privileges:true
    cap_drop:
      - ALL
    read_only: true
    tmpfs:
      - /tmp:rw,noexec,nosuid,size=64m
```

### Links
- [Trust Wallet Core](https://github.com/trustwallet/wallet-core) - is an open-source, cross-platform, mobile-focused library implementing low-level cryptographic wallet functionality for a high number of blockchains. It is a core part of the popular Trust Wallet and some other projects.
- [Trust Developers, Building](https://developer.trustwallet.com/developer/wallet-core/developing-the-library/building) - instructions for building the Wallet Core library locally. 
- [WalletConsole utility](https://github.com/trustwallet/wallet-core/tree/master/walletconsole) - The _Wallet Core_ library comes with an interactive command-line utility for accessing key- and address management functionality of the library.
- [Trust Developers, WalletConsole utility](https://developer.trustwallet.com/developer/wallet-core/developing-the-library/walletconsole) - "walletconsole" CLI reference

### Contributing

This project is currently in a **"Portfolio"** state, so pull requests (PRs) are not actively solicited.  
However, if you're interested in contributing or learning, here are some ideas:
- [ ] Change the "newMnemonic" endpoint to take the number of words instead of the number of bits (C#, difficulty - easy)
- [ ] Improve `CompileTrustWalletConsole` scripts (PowerShell/Bash, difficulty - easy)
- [ ] Refactor null checks: Replace all `== null`/`!= null` checks with `is null`/`is not null` (C#, difficulty - easy)
  - Reasoning: "==" and "!=" operators may be overloaded in C# and we all remember what `#define true false` can lead us to ;)
- [ ] Replace Swagger with OpenAPI (C#, difficulty - easy)
  - Reasoning: Swagger support has been removed in .NET 9
- [ ] Upgrade the project to the next version on .NET (C#, difficulty - easy)
  - **Warning:** Be cautious of dependencies, as some libraries have changed their licenses to non-free...
  - Please take into account that I prefer to stick to LTS versions of .NET, so PR's with .NET 9 may not be accepted.
- [ ] Implement "addrXpub" endpoint to derive a new address from the given XPUB and address index (C#, difficulty - medium)
  - There is a catch: far from all coins support XPUBs
- [ ] Create a proper `.devcontainer` configuration for everyone to be able to develop the project inside a containerized environment (configuration, difficulty - medium)  
- [ ] Refactor `MinimalApiExtensions` (C#, difficulty - middle)
  - Current implementation is very sensitive to the order of `PostCommand` calls, which must correspond to `StateMachieFlows` and `StateMachineStates` transitions defined in `StateMachine`'s `ProcessOutput` method.
  - You may not be afraid of loops as they likely produce an "UnexpectedError" result, indicating something went wrong.
  - Remember to run Integration Tests frequently to check your progress.
- [ ] Contribute to the Wallet Core repository: For example, add something like message/transaction signing support to WalletConsole, then bring the update to this WalletConsoleApi solution accordingly (C/C++ with C#, difficulty - hard)
  - Avoid attempting to add "GetBalance"-like functionality, as it is not the intended purpose of Wallet Core (refer to the documentation).
- [ ] Make a competitor project that replaces CLI calls to `walletconsole` with direct PInvoke calls to the Trust Wallet Core library (C#, difficulty - hard)

### Software Bill of Materials

Core components:
- Trust Wallet Core (Apache-2.0 license)
- ASP.NET Core 8.0
- Swagger
- Microsoft Test SDK
- FluentAssertions
- XUnit

Technologies & Tools:
- Ubuntu Server
- Git and GitHub
- Docker
- JetBrains Rider
- Hyper-V

### Acknowledgments

Thanks for everyone who made this project possible!

Address for acknowledgments if any:  
[![Bitcoin Thanks](https://img.shields.io/badge/Bitcoin-1AczZbU5Wg3KkZSVNqCkSjMbMFJ4fMMAbx-grey?logo=bitcoin&logoColor=white&labelColor=FF9900)](bitcoin:1MB2rSidXHKfKTaGiFcYEVzCf7iANHzpMH)
