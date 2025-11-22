namespace WalletConsoleApi.ProcessState.Results;

/// <summary>
/// Result for <see cref="StateMachine"/> listing all known coins
/// </summary>
public class StateMachineCoinsResult : StateMachineResultBase
{
    public List<CoinInformation> Result { get; set; } = [];
}

/// <summary>
/// Coin information DTO
/// </summary>
public class CoinInformation
{
    public string Network { get; set; } = "";

    public string Id { get; set; } = "";

    public string Name { get; set; } = "";
}
