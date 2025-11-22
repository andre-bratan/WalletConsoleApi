using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WalletConsoleApi.ProcessOutput;
using WalletConsoleApi.ProcessState;
using WalletConsoleApi.ProcessState.Results;
using WalletConsoleApi.Utils;

namespace WalletConsoleApi;

//
// WARNING: Never implement endpoints exposing "fileW" and "fileR" commands of WalletConsole via API - they give access to the host's filesystem and are potential security holes!
//

public static class MinimalApiExtensions
{
    private const string WALLET_CONSOLE_LOCATION = "WalletConsole/walletconsole";

    public static void ConfigureMinimalApiEndpoints(this WebApplication application)
    {
        // Redirect root URL to Swagger UI
        application.MapGet("/", () => Results.Redirect("/swagger"))
            .ExcludeFromDescription(); // do not show this endpoint in Swagger
            // .NET 10 will support something like this:
            // .WithOpenApi(operation =>
            // {
            //     // Return `null` means “don’t include this in OpenAPI”
            //     return null;
            // });

        application.MapGet("/ping", () => "Pong"); // health check

        // Note: WalletConsole always generates a new seed phrase on startup, so we just "dump" it
        application.MapGet("/getMnemonic",
            async ([FromServices] ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
            {
                var logger = loggerFactory.CreateLogger("GetMnemonic Endpoint");

                var executableCheckResult = CheckExecutablePath(logger);
                if (executableCheckResult != null)
                    return executableCheckResult;

                using var process = new Process { StartInfo = GetProcessStartInfo() };
                var stateMachineLogger = loggerFactory.CreateLogger<StateMachine>();
                IStateMachine stateMachine = new StateMachine(stateMachineLogger);
                stateMachine.ExpectDumpMnemonicFlow(); // IStateMachineConfiguration

                var processListenerLogger = loggerFactory.CreateLogger<ProcessListener>();
                using var processListener = new ProcessListener(process, stateMachine, processListenerLogger);

                try
                {
                    await processListener.StartProcess(
                        expectedTriggerOutputLine: StateMachine.ExpectedApplicationStartupLines.Last(),
                        cancellationToken);
                    var stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    await processListener.PostCommand("dumpMnemonic",
                        expectedTriggerOutputLine: "", // expecting only one line
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    //processListener.NoMoreCommands(); // do not close the StdIn stream because WalletConsole process will start spamming with "> " prompts

                    return Results.Ok(stateMachine.Result);
                }
                finally
                {
                    KillProcess(process, logger);
                }
            });

        // Note: It may seem like this endpoint does the same as "getMnemonic", but it allows setting the seed phrase's "strength" also
        application.MapGet("/newMnemonic",
            async ([FromServices] ILoggerFactory loggerFactory, CancellationToken cancellationToken, [FromQuery] int strength = 128) =>
            {
                var logger = loggerFactory.CreateLogger("NewMnemonic Endpoint");

                if (strength < 128 || strength > 256 || strength % 32 != 0)
                    return Results.BadRequest(new StateMachineErrorResult { Error = "Strength must be between 128 and 256, and multiple of 32" });

                var executableCheckResult = CheckExecutablePath(logger);
                if (executableCheckResult != null)
                    return executableCheckResult;

                using var process = new Process { StartInfo = GetProcessStartInfo() };
                var stateMachineLogger = loggerFactory.CreateLogger<StateMachine>();
                IStateMachine stateMachine = new StateMachine(stateMachineLogger);
                stateMachine.ExpectNewMnemonicFlow(); // IStateMachineConfiguration

                var processListenerLogger = loggerFactory.CreateLogger<ProcessListener>();
                using var processListener = new ProcessListener(process, stateMachine, processListenerLogger);

                try
                {
                    await processListener.StartProcess(
                        expectedTriggerOutputLine: StateMachine.ExpectedApplicationStartupLines.Last(),
                        cancellationToken);
                    var stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    await processListener.PostCommand($"newMnemonic {strength}",
                        expectedTriggerOutputLine: "", // expecting only one line
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    await processListener.PostCommand("dumpMnemonic",
                        expectedTriggerOutputLine: "", // expecting only one line
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    //processListener.NoMoreCommands(); // do not close the StdIn stream because WalletConsole process will start spamming with "> " prompts

                    return Results.Ok(stateMachine.Result);
                }
                finally
                {
                    KillProcess(process, logger);
                }
            });

        application.MapPost("/getSeed",
            async ([FromBody] string mnemonic, [FromServices] ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
            {
                var logger = loggerFactory.CreateLogger("GetSeed Endpoint");

                if (string.IsNullOrWhiteSpace(mnemonic))
                    return Results.BadRequest(new StateMachineErrorResult { Error = "Mnemonic must be provided" });

                var executableCheckResult = CheckExecutablePath(logger);
                if (executableCheckResult != null)
                    return executableCheckResult;

                using var process = new Process { StartInfo = GetProcessStartInfo() };
                var stateMachineLogger = loggerFactory.CreateLogger<StateMachine>();
                IStateMachine stateMachine = new StateMachine(stateMachineLogger);
                stateMachine.ExpectDumpSeedFlow(); // IStateMachineConfiguration

                var processListenerLogger = loggerFactory.CreateLogger<ProcessListener>();
                using var processListener = new ProcessListener(process, stateMachine, processListenerLogger);

                try
                {
                    await processListener.StartProcess(
                        expectedTriggerOutputLine: StateMachine.ExpectedApplicationStartupLines.Last(),
                        cancellationToken);
                    var stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    await processListener.PostCommand($"setMnemonic {mnemonic}",
                        expectedTriggerOutputLine: "", // expecting only one line
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    await processListener.PostCommand("dumpSeed",
                        expectedTriggerOutputLine: "", // expecting only one line
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    //processListener.NoMoreCommands(); // do not close the StdIn stream because WalletConsole process will start spamming with "> " prompts

                    return Results.Ok(stateMachine.Result);
                }
                finally
                {
                    KillProcess(process, logger);
                }
            });

        application.MapGet("/listCoins",
            async ([FromServices] ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
            {
                var logger = loggerFactory.CreateLogger("ListCoins Endpoint");

                var executableCheckResult = CheckExecutablePath(logger);
                if (executableCheckResult != null)
                    return executableCheckResult;

                using var process = new Process { StartInfo = GetProcessStartInfo() };
                var stateMachineLogger = loggerFactory.CreateLogger<StateMachine>();
                var stateMachine = new StateMachine(stateMachineLogger);
                stateMachine.ExpectListCoinsFlow(); // IStateMachineConfiguration

                var processListenerLogger = loggerFactory.CreateLogger<ProcessListener>();
                using var processListener = new ProcessListener(process, stateMachine, processListenerLogger);

                try
                {
                    await processListener.StartProcess(
                        expectedTriggerOutputLine: StateMachine.ExpectedApplicationStartupLines.Last(),
                        cancellationToken);
                    var stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    await processListener.PostCommand("coins",
                        expectedTriggerOutputLine: StateMachine.ExpectedListCoinsLineSuffix,
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    //processListener.NoMoreCommands(); // do not close the StdIn stream because WalletConsole process will start spamming with "> " prompts

                    return Results.Ok(stateMachine.Result);
                }
                finally
                {
                    KillProcess(process, logger);
                }
            });

        application.MapGet("/checkAddress/{coinId}/{address}",
            async ([FromRoute] string coinId, [FromRoute] string address, [FromServices] ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
            {
                var logger = loggerFactory.CreateLogger("CheckAddress Endpoint");

                if (string.IsNullOrWhiteSpace(coinId))
                    return Results.BadRequest(new StateMachineErrorResult { Error = "CoinId must be provided" });

                if (string.IsNullOrWhiteSpace(address))
                    return Results.BadRequest(new StateMachineErrorResult { Error = "Address must be provided" });

                var executableCheckResult = CheckExecutablePath(logger);
                if (executableCheckResult != null)
                    return executableCheckResult;

                using var process = new Process { StartInfo = GetProcessStartInfo() };
                var stateMachineLogger = loggerFactory.CreateLogger<StateMachine>();
                var stateMachine = new StateMachine(stateMachineLogger);
                stateMachine.ExpectCheckAddressFlow(); // IStateMachineConfiguration

                var processListenerLogger = loggerFactory.CreateLogger<ProcessListener>();
                using var processListener = new ProcessListener(process, stateMachine, processListenerLogger);

                try
                {
                    await processListener.StartProcess(
                        expectedTriggerOutputLine: StateMachine.ExpectedApplicationStartupLines.Last(),
                        cancellationToken);
                    var stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    await processListener.PostCommand($"coin {coinId}",
                        expectedTriggerOutputLine: "", // expecting only one line
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    await processListener.PostCommand($"addr {address}",
                        expectedTriggerOutputLine: "", // expecting only one line
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    //processListener.NoMoreCommands(); // do not close the StdIn stream because WalletConsole process will start spamming with "> " prompts

                    return Results.Ok(stateMachine.Result);
                }
                finally
                {
                    KillProcess(process, logger);
                }
            });

        application.MapGet("/derivationPath/{coinId}",
            async ([FromRoute] string coinId, [FromServices] ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
            {
                var logger = loggerFactory.CreateLogger("DerivationPath Endpoint");

                if (string.IsNullOrWhiteSpace(coinId))
                    return Results.BadRequest(new StateMachineErrorResult { Error = "CoinId must be provided" });

                var executableCheckResult = CheckExecutablePath(logger);
                if (executableCheckResult != null)
                    return executableCheckResult;

                using var process = new Process { StartInfo = GetProcessStartInfo() };
                var stateMachineLogger = loggerFactory.CreateLogger<StateMachine>();
                var stateMachine = new StateMachine(stateMachineLogger);
                stateMachine.ExpectDerivationPathFlow(); // IStateMachineConfiguration

                var processListenerLogger = loggerFactory.CreateLogger<ProcessListener>();
                using var processListener = new ProcessListener(process, stateMachine, processListenerLogger);

                try
                {
                    await processListener.StartProcess(
                        expectedTriggerOutputLine: StateMachine.ExpectedApplicationStartupLines.Last(),
                        cancellationToken);
                    var stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    await processListener.PostCommand($"coin {coinId}",
                        expectedTriggerOutputLine: "", // expecting only one line
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    await processListener.PostCommand("dumpDP",
                        expectedTriggerOutputLine: "", // expecting only one line
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    //var derivationPath = ((StateMachineStringResult)stateMachine.Result).Result;
                    //var coin = new StateMachineCoinResult { DerivationPath = derivationPath };

                    //processListener.NoMoreCommands(); // do not close the StdIn stream because WalletConsole process will start spamming with "> " prompts

                    return Results.Ok(stateMachine.Result);
                }
                finally
                {
                    KillProcess(process, logger);
                }
            });

        // Note: This endpoint is slow for coins not having a "xpub" because WalletCore produces empty response for such cases thus making the ProcessListener fall back to output timeout timer
        application.MapPost("/xpub/{coinId}",
            async ([FromRoute] string coinId, [FromBody] string mnemonic, [FromServices] ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
            {
                var logger = loggerFactory.CreateLogger("XPUB Endpoint");

                if (string.IsNullOrWhiteSpace(coinId))
                    return Results.BadRequest(new StateMachineErrorResult { Error = "CoinId must be provided" });

                if (string.IsNullOrWhiteSpace(mnemonic))
                    return Results.BadRequest(new StateMachineErrorResult { Error = "Mnemonic must be provided" });

                var executableCheckResult = CheckExecutablePath(logger);
                if (executableCheckResult != null)
                    return executableCheckResult;

                using var process = new Process { StartInfo = GetProcessStartInfo() };
                var stateMachineLogger = loggerFactory.CreateLogger<StateMachine>();
                var stateMachine = new StateMachine(stateMachineLogger);
                stateMachine.ExpectDumpXPubFlow(); // IStateMachineConfiguration

                var processListenerLogger = loggerFactory.CreateLogger<ProcessListener>();
                using var processListener = new ProcessListener(process, stateMachine, processListenerLogger);

                try
                {
                    await processListener.StartProcess(
                        expectedTriggerOutputLine: StateMachine.ExpectedApplicationStartupLines.Last(),
                        cancellationToken);
                    var stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    await processListener.PostCommand($"coin {coinId}",
                        expectedTriggerOutputLine: "", // expecting only one line
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    await processListener.PostCommand($"setMnemonic {mnemonic}",
                        expectedTriggerOutputLine: "", // expecting only one line
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    await processListener.PostCommand("dumpXpub",
                        expectedTriggerOutputLine: "", // expecting only one line
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    //processListener.NoMoreCommands(); // do not close the StdIn stream because WalletConsole process will start spamming with "> " prompts

                    return Results.Ok(stateMachine.Result);
                }
                finally
                {
                    KillProcess(process, logger);
                }
            });

        application.MapPost("/defaultAddress/{coinId}",
            async ([FromRoute] string coinId, [FromBody] string mnemonic, [FromServices] ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
            {
                var logger = loggerFactory.CreateLogger("GetDefaultAddress Endpoint");

                if (string.IsNullOrWhiteSpace(coinId))
                    return Results.BadRequest(new StateMachineErrorResult { Error = "CoinId must be provided" });

                if (string.IsNullOrWhiteSpace(mnemonic))
                    return Results.BadRequest(new StateMachineErrorResult { Error = "Mnemonic must be provided" });

                var executableCheckResult = CheckExecutablePath(logger);
                if (executableCheckResult != null)
                    return executableCheckResult;

                using var process = new Process { StartInfo = GetProcessStartInfo() };
                var stateMachineLogger = loggerFactory.CreateLogger<StateMachine>();
                var stateMachine = new StateMachine(stateMachineLogger);
                stateMachine.ExpectDefaultAddressFlow(); // IStateMachineConfiguration

                var processListenerLogger = loggerFactory.CreateLogger<ProcessListener>();
                using var processListener = new ProcessListener(process, stateMachine, processListenerLogger);

                try
                {
                    await processListener.StartProcess(
                        expectedTriggerOutputLine: StateMachine.ExpectedApplicationStartupLines.Last(),
                        cancellationToken);
                    var stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    await processListener.PostCommand($"coin {coinId}",
                        expectedTriggerOutputLine: "", // expecting only one line
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    await processListener.PostCommand($"setMnemonic {mnemonic}",
                        expectedTriggerOutputLine: "", // expecting only one line
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    await processListener.PostCommand("addrDefault",
                        expectedTriggerOutputLine: "", // expecting only one line
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    //processListener.NoMoreCommands(); // do not close the StdIn stream because WalletConsole process will start spamming with "> " prompts

                    return Results.Ok(stateMachine.Result);
                }
                finally
                {
                    KillProcess(process, logger);
                }
            });

        // Note: This endpoint is slow on errors because WalletCore produces an unexpected response making the ProcessListener fall back to output timeout timer
        application.MapPost("/derivePrivateKey/{coinId}",
            async ([FromRoute] string coinId, [FromQuery] string? derivationPath, [FromBody] string mnemonic, [FromServices] ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
            {
                var logger = loggerFactory.CreateLogger("DerivePrivateKey Endpoint");

                if (string.IsNullOrWhiteSpace(coinId))
                    return Results.BadRequest(new StateMachineErrorResult { Error = "CoinId must be provided" });

                // if (string.IsNullOrWhiteSpace(derivationPath))
                //     return Results.BadRequest(
                //         new StateMachineErrorResult { Error = "Derivation path must be provided" });

                if (string.IsNullOrWhiteSpace(mnemonic))
                    return Results.BadRequest(new StateMachineErrorResult { Error = "Mnemonic must be provided" });

                var executableCheckResult = CheckExecutablePath(logger);
                if (executableCheckResult != null)
                    return executableCheckResult;

                using var process = new Process { StartInfo = GetProcessStartInfo() };
                var stateMachineLogger = loggerFactory.CreateLogger<StateMachine>();
                var stateMachine = new StateMachine(stateMachineLogger);
                stateMachine.ExpectDerivePrivateKeyFlow(); // IStateMachineConfiguration

                var processListenerLogger = loggerFactory.CreateLogger<ProcessListener>();
                using var processListener = new ProcessListener(process, stateMachine, processListenerLogger);

                try
                {
                    await processListener.StartProcess(
                        expectedTriggerOutputLine: StateMachine.ExpectedApplicationStartupLines.Last(),
                        cancellationToken);
                    var stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    await processListener.PostCommand($"coin {coinId}",
                        expectedTriggerOutputLine: "", // expecting only one line
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    await processListener.PostCommand($"setMnemonic {mnemonic}",
                        expectedTriggerOutputLine: "", // expecting only one line
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    // "priDP" produces 2 lines in case of success and 1 line in case of error
                    var command = string.IsNullOrWhiteSpace(derivationPath) ? "priDP" : $"priDP {derivationPath}";
                    await processListener.PostCommand(command,
                        expectedTriggerOutputLine: StateMachine.ExpectedResultLinePrefix,
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    //processListener.NoMoreCommands(); // do not close the StdIn stream because WalletConsole process will start spamming with "> " prompts

                    return Results.Ok(stateMachine.Result);
                }
                finally
                {
                    KillProcess(process, logger);
                }
            });

        // Note: This endpoint is slow on errors because WalletCore produces an unexpected response making the ProcessListener fall back to output timeout timer
        application.MapGet("/publicKey/{coinId}",
            async ([FromRoute] string coinId, [FromQuery] string? privateKey, [FromServices] ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
            {
                var logger = loggerFactory.CreateLogger("PublicKey Endpoint");

                if (string.IsNullOrWhiteSpace(coinId))
                    return Results.BadRequest(new StateMachineErrorResult { Error = "CoinId must be provided" });

                if (string.IsNullOrWhiteSpace(privateKey))
                    return Results.BadRequest(new StateMachineErrorResult { Error = "Private Key must be provided" });

                var executableCheckResult = CheckExecutablePath(logger);
                if (executableCheckResult != null)
                    return executableCheckResult;

                using var process = new Process { StartInfo = GetProcessStartInfo() };
                var stateMachineLogger = loggerFactory.CreateLogger<StateMachine>();
                var stateMachine = new StateMachine(stateMachineLogger);
                stateMachine.ExpectGetPublicKeyFlow(); // IStateMachineConfiguration

                var processListenerLogger = loggerFactory.CreateLogger<ProcessListener>();
                using var processListener = new ProcessListener(process, stateMachine, processListenerLogger);

                try
                {
                    await processListener.StartProcess(
                        expectedTriggerOutputLine: StateMachine.ExpectedApplicationStartupLines.Last(),
                        cancellationToken);
                    var stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    await processListener.PostCommand($"coin {coinId}",
                        expectedTriggerOutputLine: "", // expecting only one line
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    // "pubPri" produces 2 lines in case of success and 1 line in case of error
                    await processListener.PostCommand($"pubPri {privateKey}",
                        expectedTriggerOutputLine: StateMachine.ExpectedResultLinePrefix,
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    //processListener.NoMoreCommands(); // do not close the StdIn stream because WalletConsole process will start spamming with "> " prompts

                    return Results.Ok(stateMachine.Result);
                }
                finally
                {
                    KillProcess(process, logger);
                }
            });

        application.MapGet("/address/{coinId}",
            async ([FromRoute] string coinId, [FromQuery] string? privateKey, [FromServices] ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
            {
                var logger = loggerFactory.CreateLogger("Address Endpoint");

                if (string.IsNullOrWhiteSpace(coinId))
                    return Results.BadRequest(new StateMachineErrorResult { Error = "CoinId must be provided" });

                if (string.IsNullOrWhiteSpace(privateKey))
                    return Results.BadRequest(new StateMachineErrorResult { Error = "Private Key must be provided" });

                var executableCheckResult = CheckExecutablePath(logger);
                if (executableCheckResult != null)
                    return executableCheckResult;

                using var process = new Process { StartInfo = GetProcessStartInfo() };
                var stateMachineLogger = loggerFactory.CreateLogger<StateMachine>();
                var stateMachine = new StateMachine(stateMachineLogger);
                stateMachine.ExpectGetAddressFlow(); // IStateMachineConfiguration

                var processListenerLogger = loggerFactory.CreateLogger<ProcessListener>();
                using var processListener = new ProcessListener(process, stateMachine, processListenerLogger);

                try
                {
                    await processListener.StartProcess(
                        expectedTriggerOutputLine: StateMachine.ExpectedApplicationStartupLines.Last(),
                        cancellationToken);
                    var stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    await processListener.PostCommand($"coin {coinId}",
                        expectedTriggerOutputLine: "", // expecting only one line
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    // "pubPri" produces 2 lines in case of success and 1 line in case of error
                    await processListener.PostCommand($"addrPri {privateKey}",
                        expectedTriggerOutputLine: "", // expecting only one line
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    //processListener.NoMoreCommands(); // do not close the StdIn stream because WalletConsole process will start spamming with "> " prompts

                    return Results.Ok(stateMachine.Result);
                }
                finally
                {
                    KillProcess(process, logger);
                }
            });

        application.MapPost("/deriveAddress/{coinId}",
            async ([FromRoute] string coinId, [FromQuery] string? derivationPath, [FromBody] string mnemonic, [FromServices] ILoggerFactory loggerFactory, CancellationToken cancellationToken) =>
            {
                var logger = loggerFactory.CreateLogger("DeriveAddress Endpoint");

                if (string.IsNullOrWhiteSpace(coinId))
                    return Results.BadRequest(new StateMachineErrorResult { Error = "CoinId must be provided" });

                if (string.IsNullOrWhiteSpace(derivationPath))
                    return Results.BadRequest(
                        new StateMachineErrorResult { Error = "Derivation path must be provided" });

                if (string.IsNullOrWhiteSpace(mnemonic))
                    return Results.BadRequest(new StateMachineErrorResult { Error = "Mnemonic must be provided" });

                var executableCheckResult = CheckExecutablePath(logger);
                if (executableCheckResult != null)
                    return executableCheckResult;

                using var process = new Process { StartInfo = GetProcessStartInfo() };
                var stateMachineLogger = loggerFactory.CreateLogger<StateMachine>();
                var stateMachine = new StateMachine(stateMachineLogger);
                stateMachine.ExpectDeriveAddressFlow(); // IStateMachineConfiguration

                var processListenerLogger = loggerFactory.CreateLogger<ProcessListener>();
                using var processListener = new ProcessListener(process, stateMachine, processListenerLogger);

                try
                {
                    await processListener.StartProcess(
                        expectedTriggerOutputLine: StateMachine.ExpectedApplicationStartupLines.Last(),
                        cancellationToken);
                    var stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    await processListener.PostCommand($"coin {coinId}",
                        expectedTriggerOutputLine: "", // expecting only one line
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    await processListener.PostCommand($"setMnemonic {mnemonic}",
                        expectedTriggerOutputLine: "", // expecting only one line
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    // "priDP" produces 2 lines in case of success and 1 line in case of error
                    await processListener.PostCommand($"addrDP {derivationPath}",
                        expectedTriggerOutputLine: StateMachine.ExpectedResultLinePrefix,
                        cancellationToken: cancellationToken);
                    stateMachineErrorResult =
                        await AwaitForStateMachineResult(stateMachine, logger, cancellationToken);
                    if (stateMachineErrorResult != null)
                        return stateMachineErrorResult;

                    //processListener.NoMoreCommands(); // do not close the StdIn stream because WalletConsole process will start spamming with "> " prompts

                    return Results.Ok(stateMachine.Result);
                }
                finally
                {
                    KillProcess(process, logger);
                }
            });
    }

    private static IResult? CheckExecutablePath(ILogger logger)
    {
        if (File.Exists(WALLET_CONSOLE_LOCATION))
            return null;

        logger.LogCritical("WalletConsole executable not found at '{WalletConsoleLocation}'", WALLET_CONSOLE_LOCATION);
        return Results.StatusCode(500);
    }

    private static async Task<IResult?> AwaitForStateMachineResult(IStateMachineState stateMachine, ILogger? logger, CancellationToken cancellationToken)
    {
        var stateMachineStepSuccess = await stateMachine.FlowTask.WithCancellation(cancellationToken);
        if (stateMachineStepSuccess)
            return null;

        logger?.LogError("Error: {Error}", stateMachine.Error.Error);
        return Results.BadRequest(stateMachine.Error);
    }

    private static ProcessStartInfo GetProcessStartInfo() =>
        new()
        {
            FileName = WALLET_CONSOLE_LOCATION,
            UseShellExecute = false, // "false" is important here for input/output redirection to work properly
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

    private static void KillProcess(Process process, ILogger logger)
    {
        var processName = process.ProcessName;

        try
        {
            process.Kill(entireProcessTree: true); // May throw in a case when the process has already exited
            //await process.WaitForExitAsync(cancellationToken); // Wait for graceful stop - do not do this for WalletConsole
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while killing {ProcessName} process", processName);
        }
    }
}
