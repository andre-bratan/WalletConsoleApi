using System.Diagnostics;
using System.Text;
using WalletConsoleApi.Configuration;
using WalletConsoleApi.ProcessState;

namespace WalletConsoleApi.ProcessOutput;

public interface IProcessListener
{
    /// <summary>
    /// Starts the process and starts listening to its StdOut and StdErr streams asynchronously.
    /// </summary>
    /// <param name="expectedTriggerOutputLine">
    /// A string expected to be the last in the process's StdOut (StartsWith and EndsWith comparisons are actually used). <br/>
    /// Use <c>""</c> to expect only one line. <br/>
    /// Usage of <c>null</c> is not recommended as it will make the call to fully rely on the command output timeout timer
    /// </param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <returns>Completed Task</returns>
    Task StartProcess(string? expectedTriggerOutputLine = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a line to the process StdIn
    /// </summary>
    /// <param name="command">A command to post to the running process via StdIn</param>
    /// <param name="expectedTriggerOutputLine">
    /// A string expected to be the last in the process's StdOut (StartsWith and EndsWith comparisons are actually used). <br/>
    /// Use <c>""</c> to expect only one line. <br/>
    /// Usage of <c>null</c> is not recommended as it will make the call to fully rely on the command output timeout timer
    /// </param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <returns>Completed Task</returns>
    /// <remarks>Note: This method doesn't configure the <see cref="StateMachine"/></remarks>
    Task PostCommand(
        string command,
        string? expectedTriggerOutputLine = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Signal the end of input by closing the StdIn stream.
    /// </summary>
    /// <remarks>Check if this operation is applicable to the running process. Some processes may start spamming with prompt strings into StdOut trying to re-establish the closed StdIn connection.</remarks>
    void NoMoreCommandsLeft();
}

public sealed class ProcessListener : IProcessListener, IDisposable
{
    private const string WALLET_CONSOLE_INPUT_PROMPT = "> ";

    private readonly object stdoutLock = new();
    private readonly StringBuilder stdoutBuilder = new();
    private readonly object stderrLock = new();
    private readonly StringBuilder stderrBuilder = new();

    private readonly Process process;
    private readonly IStateMachineOperations stateMachine;
    private readonly ILogger<ProcessListener>? logger;

    private bool awaitingOutput;
    private string? awaitedTriggerOutputLine;

    private CancellationToken savedCancellationToken;

    public ProcessListener(
        Process process,
        IStateMachineOperations stateMachine,
        ILogger<ProcessListener>? logger = null)
    {
        this.stateMachine = stateMachine;
        this.logger = logger;
        this.process = process;

        // Subscribe to asynchronous reading events
        process.OutputDataReceived += OnProcessOnOutputDataReceived;
        process.ErrorDataReceived += OnProcessOnErrorDataReceived;

        commandTimeoutTimer = InitializeTimer();
    }

    public Task StartProcess(string? expectedTriggerOutputLine = null, CancellationToken cancellationToken = default)
    {
        savedCancellationToken = cancellationToken;

        awaitingOutput = true;
        awaitedTriggerOutputLine = expectedTriggerOutputLine;

        process.Start();

        // Start async reading of outputs
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        StartTimer();

        return Task.CompletedTask;
    }

    public Task PostCommand(
        string command,
        string? expectedTriggerOutputLine = null,
        CancellationToken cancellationToken = default)
    {
        if (awaitingOutput)
            throw new InvalidOperationException("Already processing a command");

        if (cancellationToken.IsCancellationRequested)
            return Task.CompletedTask;

        savedCancellationToken = cancellationToken;

        lock (stdoutLock)
        {
            if (stdoutBuilder.Length > 0)
            {
                var unexpectedData = stdoutBuilder.ToString();
                logger?.LogWarning("Unexpected data in StdOut: {UnexpectedData}", unexpectedData);
            }
            stdoutBuilder.Clear();
        }

        lock (stderrLock)
        {
            if (stderrBuilder.Length > 0)
            {
                var unexpectedData = stderrBuilder.ToString();
                logger?.LogWarning("Unexpected data in StdErr: {UnexpectedData}", unexpectedData);
            }
            stderrBuilder.Clear();
        }

        awaitingOutput = true;
        awaitedTriggerOutputLine = expectedTriggerOutputLine;
        StartTimer();

        // let the caller exit without waiting for the command to be executed
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        Task.Run(async () => await process.StandardInput.WriteLineAsync(command), cancellationToken);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

        return Task.CompletedTask;
    }

    public void NoMoreCommandsLeft()
    {
        process.StandardInput.Close();
    }

    private void OnProcessOnErrorDataReceived(object sender, DataReceivedEventArgs eventArgs)
    {
        if (savedCancellationToken.IsCancellationRequested)
            return;

        if (eventArgs.Data == null)
            return;

        ResetTimer();

        awaitingOutput = false;

        logger?.LogWarning("Unexpected data in StdErr: {Data}", eventArgs.Data);

        lock (stderrLock)
        {
            stderrBuilder.AppendLine(eventArgs.Data);
        }
    }

    private void OnProcessOnOutputDataReceived(object sender, DataReceivedEventArgs eventArgs)
    {
        if (savedCancellationToken.IsCancellationRequested)
            return;

        if (eventArgs.Data == null)
            return;

        var dataLine = eventArgs.Data;
        while (true)
        {
            if (dataLine.StartsWith(WALLET_CONSOLE_INPUT_PROMPT))
            {
                dataLine = dataLine.Substring(WALLET_CONSOLE_INPUT_PROMPT.Length);
                continue;
            }

            // BUG? Comment-out the following lines in case if your tests constantly hang - this may need further investigation
            if (dataLine.StartsWith(Constants.LineSeparator))
            {
                dataLine = dataLine.Substring(Constants.LineSeparator.Length);
                continue;
            }

            break;
        }

        if (string.IsNullOrWhiteSpace(dataLine))
            return;

        ResetTimer();

        if (!awaitingOutput)
        {
            logger?.LogWarning("Unexpected data in StdOut: {Data}", dataLine);
        }

        lock (stdoutLock)
        {
            stdoutBuilder.AppendLine(dataLine);
        }

        if (awaitedTriggerOutputLine != null &&
            (awaitedTriggerOutputLine == "" || // an empty line is a marker of a special case when we expect only one line (with any content)
             dataLine.StartsWith(awaitedTriggerOutputLine) || dataLine.EndsWith(awaitedTriggerOutputLine)))
        {
            PostOutputToStateMachine();
        }
    }

    private void PostOutputToStateMachine()
    {
        StopTimer();

        if (savedCancellationToken.IsCancellationRequested)
            return;

        awaitingOutput = false;

        lock (stdoutLock)
        {
            var result = stdoutBuilder.ToString(); // can be empty if the timer has fired without any output received
            stdoutBuilder.Clear();

            lock (stderrLock)
            {
                if (stderrBuilder.Length > 0)
                {
                    var errorResult = stderrBuilder.ToString();
                    stderrBuilder.Clear();

                    logger?.LogWarning("Unexpected data in StdErr: {Data}", errorResult);
                    logger?.LogWarning("Leftover StdOut: {Data}", result);

                    Task.Run(() => stateMachine.ProcessError(errorResult), savedCancellationToken);

                    return;
                }
            }

            Task.Run(() => stateMachine.ProcessOutput(result), savedCancellationToken);
        }
    }

    public void Dispose()
    {
        commandTimeoutTimer.Dispose();

        process.OutputDataReceived -= OnProcessOnOutputDataReceived;
        process.ErrorDataReceived -= OnProcessOnErrorDataReceived;
    }

    #region Timer
    //
    // This timer ensures we don't wait forever for a command to finish
    //
    // I.e., if, for some reason, the command is stuck or didn't produce the expected trigger line,
    // then the timer will post all previously buffered output to a state machine
    // (thus forcing it to make a decision of what to do next)
    //

    private const int COMMAND_OUTPUT_TIMEOUT_SECONDS = 1;

    private readonly object commandTimeoutTimerLock = new();
    private readonly Timer commandTimeoutTimer;
    private bool commandTimeoutTimerIsRunning;

    private static int CommandOutputTimeoutSeconds => !Debugger.IsAttached ? COMMAND_OUTPUT_TIMEOUT_SECONDS : 30; // this line is the reason why some tests run slow in debug ;)

    private Timer InitializeTimer() =>
        new(
            callback: CommandOutputTimeout,
            state: null,
            dueTime: Timeout.Infinite, // pause
            period: Timeout.Infinite
        );

    // thread-safe timer operation
    private void StartTimer()
    {
        lock (commandTimeoutTimerLock)
        {
            StartOrRestartTimer();
            commandTimeoutTimerIsRunning = true;
        }
    }

    // thread-safe timer operation
    private void ResetTimer()
    {
        lock (commandTimeoutTimerLock)
        {
            if (!commandTimeoutTimerIsRunning)
                return;

            StartOrRestartTimer();
        }
    }

    // thread-safe timer operation
    private void StopTimer()
    {
        lock (commandTimeoutTimerLock)
        {
            InfinitePauseTimer();
            commandTimeoutTimerIsRunning = false;
        }
    }

    // primitive timer operation
    private void StartOrRestartTimer()
    {
        commandTimeoutTimer.Change(
            dueTime: TimeSpan.FromSeconds(CommandOutputTimeoutSeconds),
            period: TimeSpan.FromSeconds(CommandOutputTimeoutSeconds)
        );
    }

    // primitive timer operation
    private void InfinitePauseTimer()
    {
        commandTimeoutTimer.Change(
            dueTime: Timeout.Infinite,
            period: Timeout.Infinite
        );
    }

    private void CommandOutputTimeout(object? state)
    {
        PostOutputToStateMachine();
    }

    #endregion Timer
}
