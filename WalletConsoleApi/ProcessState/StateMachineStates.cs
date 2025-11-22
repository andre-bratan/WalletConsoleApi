namespace WalletConsoleApi.ProcessState;

/// <summary>
/// Defines different stages of a <see cref="StateMachineFlows"/>
/// </summary>
public enum StateMachineStates
{
    Idle,                   // Special idle state, all successful flows start from this state and return to it. Possible transitions: ApplicationStarting
    ApplicationStarting,    // Possible transitions: * (all)
    CheckingAddress,
    CreatingMnemonic,       // Possible transitions: DumpingMnemonic
    DerivingAddress,
    DerivingPrivateKey,
    DumpingMnemonic,
    DumpingSeed,
    DumpingXPub,
    GettingAddress,
    GettingDefaultAddress,
    GettingDerivationPath,
    GettingPublicKey,
    ListingCoins,
    SettingCoin,            // Possible transitions: GettingAddress, GettingDerivationPath, GettingPublicKey, CheckingAddress, SettingMnemonic
    SettingMnemonic,        // Possible transitions: DerivingAddress, DerivingPrivateKey, DumpingSeed, DumpingXPub, GettingDefaultAddress

    Error                   // Special state marking an error, no further transitions possible, any state can transition to Error
}
