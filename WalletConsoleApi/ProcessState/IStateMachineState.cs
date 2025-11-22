using WalletConsoleApi.ProcessState.Results;

namespace WalletConsoleApi.ProcessState;

/// <summary>
/// State interface for <see cref="StateMachine"/>
/// </summary>
public interface IStateMachineState
{
    /// <summary>
    /// The Task to await for the <see cref="StateMachine"/> to finish the ongoing <see cref="Flow"/>
    /// </summary>
    /// <returns><c>true</c> in case of success or <c>false</c> in case of failure</returns>
    Task<bool> FlowTask { get; }

    /// <summary>
    /// Current <see cref="StateMachineFlows"/>
    /// </summary>
    StateMachineFlows Flow { get; }

    /// <summary>
    /// Current <see cref="StateMachineStates"/>
    /// </summary>
    StateMachineStates State { get; }

    /// <summary>
    /// Successful Result of the <see cref="StateMachine"/>
    /// </summary>
    StateMachineResultBase Result { get; }

    /// <summary>
    /// Error Result of the <see cref="StateMachine"/>
    /// </summary>
    StateMachineErrorResult Error { get; }
}
