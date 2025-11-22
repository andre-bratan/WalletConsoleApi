namespace WalletConsoleApi.ProcessState;

/// <summary>
/// Defines different paths of processing a process's outputs
/// </summary>
public enum StateMachineFlows
{
    None,               // The StateMachine is idle
    Address,            // Full path: ApplicationStarting -> SettingCoin -> GettingAddress
    ApplicationStartup, // Debug-only, useless because doesn't produce any valuable output
    CheckAddress,       // Full path: ApplicationStarting -> SettingCoin -> CheckingAddress
    DefaultAddress,     // Full path: ApplicationStarting -> SettingCoin -> SettingMnemonic -> GettingDefaultAddress
    DerivationPath,     // Full path: ApplicationStarting -> SettingCoin -> GettingDerivationPath
    DeriveAddress,      // Full path: ApplicationStarting -> SettingCoin -> SettingMnemonic -> DerivingAddress
    DerivePrivateKey,   // Full path: ApplicationStarting -> SettingCoin -> SettingMnemonic -> DerivingPrivateKey
    DumpMnemonic,       // Full path: ApplicationStarting -> DumpingMnemonic
    DumpSeed,           // Full path: ApplicationStarting -> SettingMnemonic -> DumpingSeed
    DumpXPub,           // Full path: ApplicationStarting -> SettingCoin -> SettingMnemonic -> DumpingXPub
    PublicKey,          // Full path: ApplicationStarting -> SettingCoin -> GettingPublicKey
    ListCoins,          // Full path: ApplicationStarting -> ListingCoins
    NewMnemonic         // Full path: ApplicationStarting -> CreatingMnemonic -> DumpingMnemonic
}
