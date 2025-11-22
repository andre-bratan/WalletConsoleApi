namespace WalletConsoleApi.ProcessState;

/// <summary>
/// Configuration interface for <see cref="StateMachine"/>
/// </summary>
public interface IStateMachineConfiguration
{
    /// <summary>
    /// Configures the <see cref="StateMachine"/> to process given outputs according to the <see cref="StateMachineFlows.ApplicationStartup"/> flow
    /// </summary>
    void ExpectApplicationStartupFlow();

    /// <summary>
    /// Configures the <see cref="StateMachine"/> to process given outputs according to the <see cref="StateMachineFlows.DumpMnemonic"/> flow
    /// </summary>
    void ExpectDumpMnemonicFlow();

    /// <summary>
    /// Configures the <see cref="StateMachine"/> to process given outputs according to the <see cref="StateMachineFlows.NewMnemonic"/> flow
    /// </summary>
    void ExpectNewMnemonicFlow();

    /// <summary>
    /// Configures the <see cref="StateMachine"/> to process given outputs according to the <see cref="StateMachineFlows.ListCoins"/> flow
    /// </summary>
    void ExpectListCoinsFlow();

    /// <summary>
    /// Configures the <see cref="StateMachine"/> to process given outputs according to the <see cref="StateMachineFlows.CheckAddress"/> flow
    /// </summary>
    void ExpectCheckAddressFlow();

    /// <summary>
    /// Configures the <see cref="StateMachine"/> to process given outputs according to the <see cref="StateMachineFlows.DerivationPath"/> flow
    /// </summary>
    void ExpectDerivationPathFlow();

    /// <summary>
    /// Configures the <see cref="StateMachine"/> to process given outputs according to the <see cref="StateMachineFlows.DumpSeed"/> flow
    /// </summary>
    void ExpectDumpSeedFlow();

    /// <summary>
    /// Configures the <see cref="StateMachine"/> to process given outputs according to the <see cref="StateMachineFlows.DumpXPub"/> flow
    /// </summary>
    void ExpectDumpXPubFlow();

    /// <summary>
    /// Configures the <see cref="StateMachine"/> to process given outputs according to the <see cref="StateMachineFlows.DefaultAddress"/> flow
    /// </summary>
    void ExpectDefaultAddressFlow();

    /// <summary>
    /// Configures the <see cref="StateMachine"/> to process given outputs according to the <see cref="StateMachineFlows.DerivePrivateKey"/> flow
    /// </summary>
    void ExpectDerivePrivateKeyFlow();

    /// <summary>
    /// Configures the <see cref="StateMachine"/> to process given outputs according to the <see cref="StateMachineFlows.PublicKey"/> flow
    /// </summary>
    void ExpectGetPublicKeyFlow();

    /// <summary>
    /// Configures the <see cref="StateMachine"/> to process given outputs according to the <see cref="StateMachineFlows.Address"/> flow
    /// </summary>
    void ExpectGetAddressFlow();

    /// <summary>
    /// Configures the <see cref="StateMachine"/> to process given outputs according to the <see cref="StateMachineFlows.DeriveAddress"/> flow
    /// </summary>
    void ExpectDeriveAddressFlow();
}
