namespace WalletConsoleApi.ProcessState.Results;

/// <summary>
/// Simple Result for <see cref="StateMachine"/> containing a string
/// </summary>
public class StateMachineStringResult : StateMachineResultBase
{
    public string Result { get; set; } = "";
}
