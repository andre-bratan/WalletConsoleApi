using System.Reflection;
using Microsoft.OpenApi.Models;
using WalletConsoleApi;

var builder = WebApplication.CreateBuilder(args);

// Swagger
// Swashbuckle is not available in .NET 9 or later. For an alternative, see Overview of OpenAPI support in ASP.NET Core API apps.
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc(
            "v1",
            new OpenApiInfo
            {
                Version = "v1", Title = "WalletConsole API", Description = "The ASP.NET application wrapping Trust Wallet Console with REST API"
            }
        );

        var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    }
);

builder.Services.RegisterApplicationServices(builder.Configuration);

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

//app.UseHttpsRedirection(); - it is not a good idea to use HTTPS redirection in API applications as clients are not expected to "understand" redirect responses

app.ConfigureMinimalApiEndpoints();

app.Run();
