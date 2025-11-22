namespace WalletConsoleApi.ProcessState;

/// <summary>
/// Operation interface for <see cref="StateMachine"/>
/// </summary>
public interface IStateMachineOperations
{
    /// <summary>
    /// Passes the buffered Process's output (StdOut) to the <see cref="StateMachine"/>
    /// </summary>
    void ProcessOutput(string lines);

    /// <summary>
    /// Passes the buffered Process's error output (StdErr) to the <see cref="StateMachine"/>
    /// </summary>
    void ProcessError(string lines);
}
