param ([Parameter(Mandatory)]$runtime)

$ErrorActionPreference = "Stop"

Remove-Item -LiteralPath "$runtime.zip" -ErrorAction Ignore

dotnet clean src/Ae.DiskUsage

dotnet restore src/Ae.DiskUsage --runtime $runtime

dotnet publish src/Ae.DiskUsage `
    --configuration Release `
    --runtime $runtime `
    --framework net5.0 `
    --no-restore `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishTrimmed=true

Compress-Archive -Path src/Ae.DiskUsage/bin/Release/net5.0/$runtime/publish/* -DestinationPath "$runtime.zip"