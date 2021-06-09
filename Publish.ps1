param ([Parameter(Mandatory)]$runtime)

$ErrorActionPreference = "Stop"

Remove-Item -LiteralPath "$runtime.zip" -ErrorAction Ignore

dotnet clean src/Ae.DiskUsage --configuration Release --runtime $runtime --framework net5.0

dotnet publish src/Ae.DiskUsage --configuration Release --runtime $runtime --framework net5.0

Compress-Archive -Path src/Ae.DiskUsage/bin/Release/net5.0/$runtime/publish/* -DestinationPath "$runtime.zip"