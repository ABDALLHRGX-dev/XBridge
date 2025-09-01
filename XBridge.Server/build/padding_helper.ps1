# Package MSIX
param(
[string]$SolutionPath = "..\XBridge.Host.sln",
[string]$OutputFolder = "..\packages"
)


Write-Host "Packaging MSIX..."


dotnet publish $SolutionPath -c Release -o $OutputFolder /p:AppxBundle=Always /p:UapAppxPackageBuildMode=StoreUpload


Write-Host "MSIX package created at $OutputFolder"