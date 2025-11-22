# Script to run Docker image of WalletConsoleApi application

$DockerRepository = 'localhost'
$TargetImage = 'walletconsoleapi'
$TargetTag = 'latest'

docker run --rm --name walletconsoleapi -e ASPNETCORE_URLS="http://+:8080;" -p 8080:8080 "$DockerRepository/$TargetImage`:$TargetTag"
