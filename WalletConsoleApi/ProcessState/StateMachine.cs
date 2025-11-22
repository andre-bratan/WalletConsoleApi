using System.Diagnostics;
using WalletConsoleApi.Configuration;
using WalletConsoleApi.ProcessState.Results;

namespace WalletConsoleApi.ProcessState;

public interface IStateMachine : IStateMachineConfiguration, IStateMachineOperations, IStateMachineState;

/// <summary>
/// Responsible for tracking the current state of the WalletConsole process.
/// </summary>
public class StateMachine : IStateMachine
{
    private const TaskCreationOptions TASK_CREATION_OPTIONS = TaskCreationOptions.RunContinuationsAsynchronously;

    // general
    private static readonly string ExpectedErrorLinePrefix = "Error:";
    private static readonly string ExpectedCommandExecutionErrorLinePrefix = "Error while executing command,";
    // mnemonic
    private static readonly string ExpectedInvalidMnemonicLinePrefix = "Not a valid mnemonic";
    private static readonly string ExpectedNewMnemonicLinePrefix = "New mnemonic set";
    private static readonly string ExpectedSetMnemonicLinePrefix = "Mnemonic set";
    // other
    private static readonly string ExpectedAddressLinePrefix = "Address is a valid";
    private static readonly string ExpectedCoinLinePrefix = "Set active coin to:";
    // public
    public static readonly IReadOnlyList<string> ExpectedApplicationStartupLines =
    [
        "Wallet-core Console",
        "Type 'help'",
        "Loading coins ...",
        "Set active coin to:"
    ];
    public static readonly string ExpectedListCoinsLineSuffix = "coins listed.";
    public static readonly string ExpectedResultLinePrefix = "Result:";

    private readonly ILogger<StateMachine>? logger;

    private TaskCompletionSource<bool> flowTaskCompletionSource = new(TASK_CREATION_OPTIONS);
    private int matchingLineCount; // counter for multi-line outputs

    public Task<bool> FlowTask => flowTaskCompletionSource.Task;

    public StateMachineFlows Flow { get; private set; }

    public StateMachineStates State { get; private set; }

    public StateMachineResultBase Result { get; private set; } = null!;

    public StateMachineErrorResult Error { get; private set; } = null!;

    public StateMachine(ILogger<StateMachine>? logger = null)
    {
        this.logger = logger;

        Flow = StateMachineFlows.None;
        State = StateMachineStates.Idle;
    }

    public void ExpectApplicationStartupFlow()
    {
        ValidateStartingNewFlow();

        Flow = StateMachineFlows.ApplicationStartup;
        State = StateMachineStates.ApplicationStarting;

        matchingLineCount = 0;
    }

    public void ExpectDumpMnemonicFlow()
    {
        ValidateStartingNewFlow();

        Flow = StateMachineFlows.DumpMnemonic;
        State = StateMachineStates.ApplicationStarting;
    }

    public void ExpectNewMnemonicFlow()
    {
        ValidateStartingNewFlow();

        Flow = StateMachineFlows.NewMnemonic;
        State = StateMachineStates.ApplicationStarting;
    }

    public void ExpectListCoinsFlow()
    {
        ValidateStartingNewFlow();

        Flow = StateMachineFlows.ListCoins;
        State = StateMachineStates.ApplicationStarting;
    }

    public void ExpectCheckAddressFlow()
    {
        ValidateStartingNewFlow();

        Flow = StateMachineFlows.CheckAddress;
        State = StateMachineStates.ApplicationStarting;
    }

    public void ExpectDerivationPathFlow()
    {
        ValidateStartingNewFlow();

        Flow = StateMachineFlows.DerivationPath;
        State = StateMachineStates.ApplicationStarting;
    }

    public void ExpectDumpSeedFlow()
    {
        ValidateStartingNewFlow();

        Flow = StateMachineFlows.DumpSeed;
        State = StateMachineStates.ApplicationStarting;
    }

    public void ExpectDumpXPubFlow()
    {
        ValidateStartingNewFlow();

        Flow = StateMachineFlows.DumpXPub;
        State = StateMachineStates.ApplicationStarting;
    }

    public void ExpectDefaultAddressFlow()
    {
        ValidateStartingNewFlow();

        Flow = StateMachineFlows.DefaultAddress;
        State = StateMachineStates.ApplicationStarting;
    }

    public void ExpectDerivePrivateKeyFlow()
    {
        ValidateStartingNewFlow();

        Flow = StateMachineFlows.DerivePrivateKey;
        State = StateMachineStates.ApplicationStarting;
    }

    public void ExpectGetPublicKeyFlow()
    {
        ValidateStartingNewFlow();

        Flow = StateMachineFlows.PublicKey;
        State = StateMachineStates.ApplicationStarting;
    }

    public void ExpectGetAddressFlow()
    {
        ValidateStartingNewFlow();

        Flow = StateMachineFlows.Address;
        State = StateMachineStates.ApplicationStarting;
    }

    public void ExpectDeriveAddressFlow()
    {
        ValidateStartingNewFlow();

        Flow = StateMachineFlows.DeriveAddress;
        State = StateMachineStates.ApplicationStarting;
    }

    public void ProcessOutput(string lines)
    {
        // Note: "lines" can be empty - this means the TimeoutTimer has fired without any output captured

        var outputLines = lines.Split(
            Constants.LineSeparator,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
        );

        switch (State)
        {
            case StateMachineStates.Idle:
                SetUnexpectedResultError(lines);
                break;

            case StateMachineStates.ApplicationStarting:
                if (outputLines.Length == 0)
                {
                    SetNoDataError();
                    return;
                }

                foreach (var outputLine in outputLines)
                {
                    if (ExpectedApplicationStartupLines.Any(x => outputLine.StartsWith(x)))
                    {
                        logger?.LogDebug("Received expected data from StdOut for {State} state: {Data}", State, outputLine);

                        matchingLineCount++;
                    }
                    else
                    {
                        logger?.LogWarning("Unexpected data in StdOut for {State} state: {UnexpectedData}", State, outputLine);
                    }
                }

                if (matchingLineCount >= ExpectedApplicationStartupLines.Count)
                {
                    if (matchingLineCount > ExpectedApplicationStartupLines.Count)
                        logger?.LogWarning("Too many lines in StdOut for {State} state: {Data}", State, outputLines);
                    else
                        logger?.LogDebug("Received all expected data from StdOut for {State} state", State);

                    switch (Flow)
                    {
                        case StateMachineFlows.ApplicationStartup:
                            // A Flow "short-circuited" to the ApplicationStartup step only is actually useless and is for debugging purposes only
                            Flow = StateMachineFlows.None;
                            State = StateMachineStates.Idle;
                            break;
                        case StateMachineFlows.DumpMnemonic:
                            State = StateMachineStates.DumpingMnemonic;
                            break;
                        case StateMachineFlows.NewMnemonic:
                            State = StateMachineStates.CreatingMnemonic;
                            break;
                        case StateMachineFlows.ListCoins:
                            State = StateMachineStates.ListingCoins;
                            break;
                        case StateMachineFlows.DerivationPath:
                            State = StateMachineStates.SettingCoin;
                            break;
                        case StateMachineFlows.CheckAddress:
                            State = StateMachineStates.SettingCoin;
                            break;
                        case StateMachineFlows.DumpSeed:
                            State = StateMachineStates.SettingMnemonic;
                            break;
                        case StateMachineFlows.DumpXPub:
                            State = StateMachineStates.SettingCoin;
                            break;
                        case StateMachineFlows.DefaultAddress:
                            State = StateMachineStates.SettingCoin;
                            break;
                        case StateMachineFlows.DerivePrivateKey:
                            State = StateMachineStates.SettingCoin;
                            break;
                        case StateMachineFlows.PublicKey:
                            State = StateMachineStates.SettingCoin;
                            break;
                        case StateMachineFlows.Address:
                            State = StateMachineStates.SettingCoin;
                            break;
                        case StateMachineFlows.DeriveAddress:
                            State = StateMachineStates.SettingCoin;
                            break;
                        default:
                            throw new NotSupportedException($"Unexpected Flow: {Flow}");
                    }

                    SetResult(new StateMachineStringResult { Result = "WalletConsole utility started" });
                }

                break;

            case StateMachineStates.DumpingMnemonic:
                if (outputLines.Length == 0)
                {
                    SetNoDataError();
                    return;
                }

                if (outputLines.Length > 1)
                {
                    SetTooManyLinesError(outputLines);
                    return;
                }

                var expectedMnemonic = outputLines.First();
                if (!expectedMnemonic.StartsWith(ExpectedResultLinePrefix))
                {
                    SetUnexpectedResultError(expectedMnemonic);
                    return;
                }

                logger?.LogDebug("Received all expected data from StdOut for {State} state", State);

                var mnemonicResult = expectedMnemonic.Substring(ExpectedResultLinePrefix.Length).Trim();

                Flow = StateMachineFlows.None;
                State = StateMachineStates.Idle;

                SetResult(new StateMachineStringResult { Result = mnemonicResult });

                break;

            case StateMachineStates.CreatingMnemonic:
                if (outputLines.Length == 0)
                {
                    SetNoDataError();
                    return;
                }

                if (outputLines.Length > 1)
                {
                    SetTooManyLinesError(outputLines);
                    return;
                }

                var expectedNewMnemonicResponse = outputLines.First();
                if (!expectedNewMnemonicResponse.StartsWith(ExpectedNewMnemonicLinePrefix))
                {
                    SetFilteredError(expectedNewMnemonicResponse);
                    return;
                }

                logger?.LogDebug("Received all expected data from StdOut for {State} state", State);

                State = StateMachineStates.DumpingMnemonic;

                SetResult(new StateMachineStringResult { Result = "Ok" });

                break;

            case StateMachineStates.SettingMnemonic:
                if (outputLines.Length == 0)
                {
                    SetNoDataError();
                    return;
                }

                if (outputLines.Length > 1)
                {
                    SetTooManyLinesError(outputLines);
                    return;
                }

                var expectedSetMnemonicResponse = outputLines.First();
                if (!expectedSetMnemonicResponse.StartsWith(ExpectedSetMnemonicLinePrefix))
                {
                    if (expectedSetMnemonicResponse.StartsWith(ExpectedInvalidMnemonicLinePrefix))
                        SetError("Not a valid mnemonic");
                    else if (expectedSetMnemonicResponse.StartsWith(ExpectedErrorLinePrefix))
                        SetFilteredError(expectedSetMnemonicResponse);
                    else
                        SetUnexpectedResultError(expectedSetMnemonicResponse);

                    return;
                }

                logger?.LogDebug("Received all expected data from StdOut for {State} state", State);

                switch (Flow)
                {
                    case StateMachineFlows.DumpSeed:
                        State = StateMachineStates.DumpingSeed;
                        break;
                    case StateMachineFlows.DumpXPub:
                        State = StateMachineStates.DumpingXPub;
                        break;
                    case StateMachineFlows.DefaultAddress:
                        State = StateMachineStates.GettingDefaultAddress;
                        break;
                    case StateMachineFlows.DerivePrivateKey:
                        State = StateMachineStates.DerivingPrivateKey;
                        break;
                    case StateMachineFlows.DeriveAddress:
                        State = StateMachineStates.DerivingAddress;
                        break;
                    default:
                        throw new NotSupportedException($"Unexpected Flow: {Flow}");
                }

                SetResult(new StateMachineStringResult { Result = "Mnemonic set" });

                break;

            case StateMachineStates.ListingCoins:
                if (outputLines.Length == 0)
                {
                    SetNoDataError();
                    return;
                }

                var coinsResult = new List<CoinInformation>();
                foreach (var outputLine in outputLines)
                {
                    if (outputLine.EndsWith(ExpectedListCoinsLineSuffix))
                        continue; // exclude last line with total number of coins

                    var coinLines = outputLine.Split('\t').Select(x => x.Trim().Trim('\'')).ToList(); // lines like "btg      bitcoingold     'bitcoin gold'" get transformed into ["btg", "bitcoingold", "bitcoin gold"]
                    var coin = new CoinInformation
                    {
                        Network = coinLines[0],
                        Id = coinLines[1],
                        Name = coinLines[2]
                    };

                    coinsResult.Add(coin);
                }

                Flow = StateMachineFlows.None;
                State = StateMachineStates.Idle;

                SetResult(new StateMachineCoinsResult { Result = coinsResult });

                break;

            case StateMachineStates.SettingCoin:
                if (outputLines.Length == 0)
                {
                    SetNoDataError();
                    return;
                }

                if (outputLines.Length > 1)
                {
                    SetTooManyLinesError(outputLines);
                    return;
                }

                var expectedCoinResponse = outputLines.First();
                if (!expectedCoinResponse.StartsWith(ExpectedCoinLinePrefix))
                {
                    SetFilteredError(expectedCoinResponse);
                    return;
                }

                logger?.LogDebug("Received all expected data from StdOut for {State} state", State);

                switch (Flow)
                {
                    case StateMachineFlows.DerivationPath:
                        State = StateMachineStates.GettingDerivationPath;
                        break;
                    case StateMachineFlows.CheckAddress:
                        State = StateMachineStates.CheckingAddress;
                        break;
                    case StateMachineFlows.DumpXPub:
                        State = StateMachineStates.SettingMnemonic;
                        break;
                    case StateMachineFlows.DefaultAddress:
                        State = StateMachineStates.SettingMnemonic;
                        break;
                    case StateMachineFlows.DerivePrivateKey:
                        State = StateMachineStates.SettingMnemonic;
                        break;
                    case StateMachineFlows.PublicKey:
                        State = StateMachineStates.GettingPublicKey;
                        break;
                    case StateMachineFlows.Address:
                        State = StateMachineStates.GettingAddress;
                        break;
                    case StateMachineFlows.DeriveAddress:
                        State = StateMachineStates.SettingMnemonic;
                        break;
                    default:
                        throw new NotSupportedException($"Unexpected Flow: {Flow}");
                }

                SetResult(new StateMachineStringResult { Result = "Coin set" });

                break;

            case StateMachineStates.GettingDerivationPath:
                if (outputLines.Length == 0)
                {
                    SetNoDataError();
                    return;
                }

                if (outputLines.Length > 1)
                {
                    SetTooManyLinesError(outputLines);
                    return;
                }

                var expectedDerivationPath = outputLines.First();
                if (!expectedDerivationPath.StartsWith(ExpectedResultLinePrefix))
                {
                    SetUnexpectedResultError(expectedDerivationPath);
                    return;
                }

                logger?.LogDebug("Received all expected data from StdOut for {State} state", State);

                var derivationPathResult = expectedDerivationPath.Substring(ExpectedResultLinePrefix.Length).Trim();

                Flow = StateMachineFlows.None;
                State = StateMachineStates.Idle;

                SetResult(new StateMachineStringResult { Result = derivationPathResult });

                break;

            case StateMachineStates.CheckingAddress:
                if (outputLines.Length == 0)
                {
                    SetNoDataError();
                    return;
                }

                if (outputLines.Length > 1)
                {
                    SetTooManyLinesError(outputLines);
                    return;
                }

                logger?.LogDebug("Received all expected data from StdOut for {State} state", State);

                var expectedAddressLine = outputLines.First();
                if (!expectedAddressLine.StartsWith(ExpectedAddressLinePrefix))
                {
                    SetResult(new StateMachineBooleanResult { Result = false });
                    return;
                }

                Flow = StateMachineFlows.None;
                State = StateMachineStates.Idle;

                SetResult(new StateMachineBooleanResult { Result = true });

                break;

            case StateMachineStates.DumpingSeed:
                if (outputLines.Length == 0)
                {
                    SetNoDataError();
                    return;
                }

                if (outputLines.Length > 1)
                {
                    SetTooManyLinesError(outputLines);
                    return;
                }

                var expectedSeedResponse = outputLines.First();
                if (!expectedSeedResponse.StartsWith(ExpectedResultLinePrefix))
                {
                    SetUnexpectedResultError(expectedSeedResponse);
                    return;
                }

                logger?.LogDebug("Received all expected data from StdOut for {State} state", State);

                var seed = expectedSeedResponse.Substring(ExpectedResultLinePrefix.Length).Trim();

                Flow = StateMachineFlows.None;
                State = StateMachineStates.Idle;

                SetResult(new StateMachineStringResult { Result = seed });

                break;

            case StateMachineStates.DumpingXPub:
                if (outputLines.Length == 0)
                {
                    SetNoDataError();
                    return;
                }

                if (outputLines.Length > 1)
                {
                    SetTooManyLinesError(outputLines);
                    return;
                }

                var expectedXPubResponse = outputLines.First();
                if (!expectedXPubResponse.StartsWith(ExpectedResultLinePrefix))
                {
                    SetUnexpectedResultError(expectedXPubResponse);
                    return;
                }

                logger?.LogDebug("Received all expected data from StdOut for {State} state", State);

                var xpub = expectedXPubResponse.Substring(ExpectedResultLinePrefix.Length).Trim();

                Flow = StateMachineFlows.None;
                State = StateMachineStates.Idle;

                SetResult(new StateMachineStringResult { Result = xpub });

                break;

            case StateMachineStates.GettingDefaultAddress:
                if (outputLines.Length == 0)
                {
                    SetNoDataError();
                    return;
                }

                if (outputLines.Length > 1)
                {
                    SetTooManyLinesError(outputLines);
                    return;
                }

                var expectedDefaultAddressResponse = outputLines.First();
                if (!expectedDefaultAddressResponse.StartsWith(ExpectedResultLinePrefix))
                {
                    SetUnexpectedResultError(expectedDefaultAddressResponse);
                    return;
                }

                logger?.LogDebug("Received all expected data from StdOut for {State} state", State);

                var defaultAddress = expectedDefaultAddressResponse.Substring(ExpectedResultLinePrefix.Length).Trim();

                Flow = StateMachineFlows.None;
                State = StateMachineStates.Idle;

                SetResult(new StateMachineStringResult { Result = defaultAddress });

                break;

            case StateMachineStates.DerivingPrivateKey:
                if (outputLines.Length == 0)
                {
                    SetNoDataError();
                    return;
                }

                var expectedDerivedPrivateKeyResponse = outputLines.Last();
                if (!expectedDerivedPrivateKeyResponse.StartsWith(ExpectedResultLinePrefix))
                {
                    SetFilteredError(expectedDerivedPrivateKeyResponse);
                }

                logger?.LogDebug("Received all expected data from StdOut for {State} state", State);

                var privateKey = expectedDerivedPrivateKeyResponse.Substring(ExpectedResultLinePrefix.Length).Trim();

                Flow = StateMachineFlows.None;
                State = StateMachineStates.Idle;

                SetResult(new StateMachineStringResult { Result = privateKey });

                break;

            case StateMachineStates.GettingPublicKey:
                if (outputLines.Length == 0)
                {
                    SetNoDataError();
                    return;
                }

                var expectedPublicKeyResponse = outputLines.Last();
                if (!expectedPublicKeyResponse.StartsWith(ExpectedResultLinePrefix))
                {
                    SetFilteredError(expectedPublicKeyResponse);
                }

                logger?.LogDebug("Received all expected data from StdOut for {State} state", State);

                var publicKey = expectedPublicKeyResponse.Substring(ExpectedResultLinePrefix.Length).Trim();

                Flow = StateMachineFlows.None;
                State = StateMachineStates.Idle;

                SetResult(new StateMachineStringResult { Result = publicKey });

                break;

            case StateMachineStates.GettingAddress:
                if (outputLines.Length == 0)
                {
                    SetNoDataError();
                    return;
                }

                if (outputLines.Length > 1)
                {
                    SetTooManyLinesError(outputLines);
                    return;
                }

                var expectedAddressResponse = outputLines.First();
                if (!expectedAddressResponse.StartsWith(ExpectedResultLinePrefix))
                {
                    SetFilteredError(expectedAddressResponse);
                }

                logger?.LogDebug("Received all expected data from StdOut for {State} state", State);

                var address = expectedAddressResponse.Substring(ExpectedResultLinePrefix.Length).Trim();

                Flow = StateMachineFlows.None;
                State = StateMachineStates.Idle;

                SetResult(new StateMachineStringResult { Result = address });

                break;

            case StateMachineStates.DerivingAddress:
                if (outputLines.Length == 0)
                {
                    SetNoDataError();
                    return;
                }

                if (outputLines.Length > 1)
                {
                    SetTooManyLinesError(outputLines);
                    return;
                }

                var expectedDerivedAddressResponse = outputLines.First();
                if (!expectedDerivedAddressResponse.StartsWith(ExpectedResultLinePrefix))
                {
                    SetFilteredError(expectedDerivedAddressResponse);
                }

                logger?.LogDebug("Received all expected data from StdOut for {State} state", State);

                var derivedAddress = expectedDerivedAddressResponse.Substring(ExpectedResultLinePrefix.Length).Trim();

                Flow = StateMachineFlows.None;
                State = StateMachineStates.Idle;

                SetResult(new StateMachineStringResult { Result = derivedAddress });

                break;

            case StateMachineStates.Error:
                throw new UnreachableException($"If you see this message it means there is a missed {nameof(ValidateStartingNewFlow)} call in one of 'Expect*Flow' methods"); // this should never happen
            default:
                throw new NotSupportedException($"State {State} is not supported");
        }
    }

    public void ProcessError(string lines)
    {
        logger?.LogError("Error in StdErr: {Data}", lines);

        SetError("Internal error");
    }

    private void SetFilteredError(string errorResponse)
    {
        // Filter out some of these error messages' prefixes:
        // - "Unknown command: ..." - leave as is
        // - "Error: ..." - filter out

        if (errorResponse.StartsWith(ExpectedErrorLinePrefix))
        {
            errorResponse = errorResponse.Substring(ExpectedErrorLinePrefix.Length).Trim();

            // capitalize the first letter
            errorResponse = char.ToUpper(errorResponse[0]) + errorResponse.Substring(1);
        }

        if (errorResponse.StartsWith(ExpectedCommandExecutionErrorLinePrefix))
        {
            errorResponse = errorResponse.Substring(ExpectedCommandExecutionErrorLinePrefix.Length).Trim();
        }

        SetError(errorResponse);
    }

    private void SetNoDataError()
    {
        logger?.LogWarning("No data in StdOut for {State} state", State);

        SetError("No data");
    }

    private void SetTooManyLinesError(string[] outputLines)
    {
        logger?.LogWarning("Too many lines in StdOut for {State} state: {Data}", State, outputLines);

        SetError("Too many lines");
    }

    private void SetUnexpectedResultError(string unexpectedData)
    {
        logger?.LogWarning("Unexpected data in StdOut for {State} state: {Data}", State, unexpectedData);

        SetError("Unexpected result");
    }

    private void SetResult(StateMachineResultBase result)
    {
        Result = result;
        FlipTaskCompletionSource(true);
    }

    private void SetError(string error)
    {
        State = StateMachineStates.Error;

        Error = new StateMachineErrorResult { Error = error };
        FlipTaskCompletionSource(false);
    }

    /// <summary>
    /// Replace the current TaskCompletionSource with a new one, thus allowing all awaiters of the old to continue
    /// </summary>
    private void FlipTaskCompletionSource(bool result)
    {
        var oldTaskCompletionSource = flowTaskCompletionSource;

        flowTaskCompletionSource = new TaskCompletionSource<bool>(TASK_CREATION_OPTIONS);

        oldTaskCompletionSource.SetResult(result);
    }

    private void ValidateStartingNewFlow()
    {
        if (State == StateMachineStates.Error)
            throw new InvalidOperationException("State machine is in failed state");

        if (State != StateMachineStates.Idle)
            throw new InvalidOperationException($"Already processing {Flow}");
    }
}
