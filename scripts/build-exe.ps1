[CmdletBinding()]
param(
    [string]$Runtime = 'win-x64',
    [switch]$Compress
)

$ErrorActionPreference = 'Stop'
$env:MSBUILDDISABLENODEREUSE = '1'

. (Join-Path $PSScriptRoot 'common.ps1')

$repoRoot = Get-RepoRoot
$version = Get-AppVersion
$project = Join-Path $repoRoot 'src\frontend\VoltStream.WPF\VoltStream.WPF.csproj'
$stageDir = Join-Path $repoRoot "src\frontend\VoltStream.WPF\bin\Release\publish-$Runtime"

$arguments = @(
    'publish', $project,
    '-c', 'Release',
    '-r', $Runtime,
    '--self-contained', 'true',
    '-p:PublishSingleFile=true',
    '-p:IncludeNativeLibrariesForSelfExtract=true',
    '-p:PublishTrimmed=false',
    '-p:PublishReadyToRun=true',
    '-p:DebugType=None',
    '-p:DebugSymbols=false',
    '-o', $stageDir
)

if ($Compress) { $arguments += '-p:EnableCompressionInSingleFile=true' }

Write-Host "Desktop $version ($Runtime) yig'ilmoqda..."
& dotnet @arguments
if ($LASTEXITCODE -ne 0) { throw 'Desktop publish xato bilan tugadi.' }

$built = Join-Path $stageDir 'VoltStream.WPF.exe'
if (-not (Test-Path -LiteralPath $built)) { throw "VoltStream.WPF.exe topilmadi: $stageDir" }

$final = Join-Path (Get-ArtifactsDir) "voltstream-desktop-$version-$Runtime.exe"
Copy-Item -LiteralPath $built -Destination $final -Force
Show-Artifact $final
