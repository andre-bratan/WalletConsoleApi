FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
COPY ["WalletConsoleApi/WalletConsole/walletconsole", "WalletConsoleApi/WalletConsole/walletconsole"]
COPY ["WalletConsoleApi/WalletConsoleApi.csproj", "WalletConsoleApi/"]
RUN dotnet restore "WalletConsoleApi/WalletConsoleApi.csproj"
COPY . .
WORKDIR "/WalletConsoleApi"
RUN dotnet build "./WalletConsoleApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./WalletConsoleApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WalletConsoleApi.dll"]
