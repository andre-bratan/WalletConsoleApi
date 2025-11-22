namespace WalletConsoleApi.ProcessState.Results;

/// <summary>
/// Error Result for <see cref="StateMachine"/>
/// </summary>
public class StateMachineErrorResult : StateMachineResultBase
{
    public string Error { get; set; } = "";
}
