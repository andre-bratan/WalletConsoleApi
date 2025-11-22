namespace WalletConsoleApi;

public static class DependencyInjectionExtensions
{
    public static void RegisterApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddLogging(builder =>
        {
            builder
                .ClearProviders() // this also deletes Windows Event Log Provider
                .AddSimpleConsole(); // Note: AddConsole requires a different set of configuration values
        });
    }
}
