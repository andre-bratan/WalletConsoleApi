using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace WalletConsoleApi.Integration.Tests.TestsInfrastructure;

// ReSharper disable once ClassNeverInstantiated.Global
public class DevelopmentWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
    }
}
