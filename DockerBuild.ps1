# Script to build Docker image of WalletConsoleApi application

$DockerRepository = 'localhost'
$TargetImage = 'walletconsoleapi'
$TargetTag = 'latest'

$ErrorActionPreference = 'Stop'

if (-not (Test-Path './WalletConsoleApi/WalletConsole/walletconsole')) {
    Write-Host "WalletConsole utility is missing - you have to compile it by yourself first"
    Exit 1
}

docker build -t "$DockerRepository/$TargetImage`:$TargetTag" .
