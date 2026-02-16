Param(
    [string]$configuration = "Release"
)

# Build the solution
msbuild .\Halundns\Halundns.csproj /p:Configuration=$configuration

# Create artifacts
$outDir = "./out"
if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
New-Item -ItemType Directory -Path $outDir | Out-Null

# Copy Release output
Copy-Item -Path "Halundns\bin\$configuration\*" -Destination $outDir -Recurse -Force

# Zip
$zipName = "HalunDns_$configuration.zip"
if (Test-Path $zipName) { Remove-Item $zipName -Force }
Compress-Archive -Path "$outDir\*" -DestinationPath $zipName
Write-Host "Release package created: $zipName"