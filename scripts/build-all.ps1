[CmdletBinding()]
param(
    [switch]$SkipExe,
    [switch]$SkipApk
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'common.ps1')

$version = Get-AppVersion
Write-Host "VoltStream $version" -ForegroundColor Cyan

if (-not $SkipExe) { & (Join-Path $PSScriptRoot 'build-exe.ps1') }
if (-not $SkipApk) { & (Join-Path $PSScriptRoot 'build-apk.ps1') }

Write-Host "`nartifacts:" -ForegroundColor Cyan
Get-ChildItem -Path (Get-ArtifactsDir) -File |
    Sort-Object Name |
    ForEach-Object { "{0,-45} {1,8:N1} MB" -f $_.Name, ($_.Length / 1MB) }
