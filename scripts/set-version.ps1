[CmdletBinding()]
param(
    [ValidatePattern('^(major|minor|patch|\d+\.\d+\.\d+)$')][string]$Version = 'patch',
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'common.ps1')

$root = Get-RepoRoot
$current = [version](Get-AppVersion)
$next = switch ($Version) {
    'major' { [version]::new($current.Major + 1, 0, 0) }
    'minor' { [version]::new($current.Major, $current.Minor + 1, 0) }
    'patch' { [version]::new($current.Major, $current.Minor, $current.Build + 1) }
    default { [version]$Version }
}

if ($next -le $current -and -not $Force) { throw "$current -> $next pastga yoki o'zgarishsiz; Android versionCode kamaymaydi. Ataylab bo'lsa -Force." }
if ($next.Minor -ge 100 -or $next.Build -ge 100) { throw "minor va patch 100 dan kichik bo'lsin: versionCode = major*10000 + minor*100 + patch" }

$value = $next.ToString()
$code = $next.Major * 10000 + $next.Minor * 100 + $next.Build

$targets = @(
    @{ Path = 'Directory.Build.props';                                Pattern = '<Version>\d+\.\d+\.\d+</Version>';                       Value = "<Version>$value</Version>" }
    @{ Path = 'src\mobile\VoltStream.Mobile\VoltStream.Mobile.csproj'; Pattern = '<ApplicationDisplayVersion>[^<]+</ApplicationDisplayVersion>'; Value = "<ApplicationDisplayVersion>$value</ApplicationDisplayVersion>" }
    @{ Path = 'src\mobile\VoltStream.Mobile\VoltStream.Mobile.csproj'; Pattern = '<ApplicationVersion>\d+</ApplicationVersion>';           Value = "<ApplicationVersion>$code</ApplicationVersion>" }
    @{ Path = 'src\web\VoltStream.Web\package.json';                   Pattern = '"version": "\d+\.\d+\.\d+"';                             Value = "`"version`": `"$value`"" }
    @{ Path = 'src\web\VoltStream.Web\src\app\core\version.ts';        Pattern = "APP_VERSION = '\d+\.\d+\.\d+'";                          Value = "APP_VERSION = '$value'" }
)

foreach ($target in $targets) {
    $file = Join-Path $root $target.Path
    $content = Get-Content -LiteralPath $file -Raw
    if ($content -notmatch $target.Pattern) { throw "$($target.Path): '$($target.Pattern)' topilmadi" }
    Set-Content -LiteralPath $file -Value ($content -replace $target.Pattern, $target.Value) -NoNewline
    Write-Host "yangilandi: $($target.Path)"
}

Write-Host "$current -> $value (versionCode $code)"
