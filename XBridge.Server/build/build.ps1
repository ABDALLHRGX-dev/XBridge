# Build script for XBridge.Server
param(
[string]$Configuration = "Release"
)


Write-Host "Starting build for XBridge.Server in $Configuration mode..."


dotnet restore ..\XBridge.Host.sln
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }


dotnet build ..\XBridge.Host.sln --configuration $Configuration
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }


Write-Host "Build completed successfully."