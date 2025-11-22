# Script to compile Trust Wallet Core Console utility on Windows
# Warning: Can compile doesn't mean be able to run the utility - Windows is not supported by Trust Wallet Core

$SourceRepository = 'https://github.com/trustwallet/wallet-core.git'
$TargetCloneDirectory = 'wallet-core'
$TargetDockerImage = 'wallet-core:latest'

$ErrorActionPreference = 'Stop'

Push-Location

try {

	# Check if there is nothing to do
	if (Test-Path 'walletconsole') {
		Write-Host "WalletConsole is already compiled; exiting."
		Exit 0
	}

    # Check if Docker is running
	docker info >$null 2>&1
	if (-not ($LASTEXITCODE -eq 0)) {
		Write-Host "Docker is not running"
		Exit 1
	}

	# Build the Docker image
	docker image inspect $TargetDockerImage >$null 2>&1
	if ($LASTEXITCODE -eq 0) {
		Write-Host "Docker image '$TargetDockerImage' already exists; skipping build."
	} else {

		# Clone the repository
		if (-not (Test-Path $TargetCloneDirectory)) {
			Write-Host "Cloning Trust Wallet Core repository...."
			git clone "$SourceRepository" "$TargetCloneDirectory"
			Write-Host "Done."
		} else {
			Write-Host "Directory '$TargetCloneDirectory' already exists; skipping clone."
		}

		Push-Location -Path "$TargetCloneDirectory"

		# Fix CRLF issues on Windows :)
		if (-not (Test-Path '.gitattributes')) {
			Write-Host "Fixing CRLF file endings in the repository...."
			'* text=auto eol=lf' | Out-File -FilePath '.gitattributes' -Encoding utf8
			git rm --cached -r .
			git reset --hard
			Write-Host "Done."
		} else {
			Write-Host "File 'gitattributes' already exists; skipping CRLF fix."
		}

		Write-Host "Docker image '$TargetDockerImage' not found, building..."
		docker build -t "$TargetDockerImage" .
		if ($?) {
			Write-Host "Docker image '$TargetDockerImage' built successfully."
		} else {
			Write-Host "Failed to build Docker image '$TargetDockerImage'."
			Exit 1
		}

		Pop-Location
	}

	# Build the utility and get its copy outside the Docker container
	docker run -it --rm --name wallet-core --pull=never -v .:/opt "$TargetDockerImage" bash -c "cd /wallet-core/build/walletconsole && make && cp ./walletconsole /opt && exit"

} finally {
	Pop-Location
}
