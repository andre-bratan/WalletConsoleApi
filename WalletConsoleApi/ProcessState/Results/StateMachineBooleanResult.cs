namespace WalletConsoleApi.ProcessState.Results;

/// <summary>
/// Simple Result for <see cref="StateMachine"/> containing a boolean
/// </summary>
public class StateMachineBooleanResult : StateMachineResultBase
{
    public bool Result { get; set; }
}