$script:RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

function Get-RepoRoot { $script:RepoRoot }

function Get-AppVersion {
    $props = Join-Path (Get-RepoRoot) 'Directory.Build.props'
    $match = [regex]::Match((Get-Content -LiteralPath $props -Raw), '<Version>(\d+\.\d+\.\d+)</Version>')
    if (-not $match.Success) { throw 'Directory.Build.props ichida <Version> topilmadi.' }
    $match.Groups[1].Value
}

function Get-ArtifactsDir {
    $dir = Join-Path (Get-RepoRoot) 'artifacts'
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    $dir
}

function Get-AndroidSdk {
    $sdk = @(
        $env:ANDROID_HOME,
        $env:ANDROID_SDK_ROOT,
        'C:\Program Files (x86)\Android\android-sdk',
        (Join-Path $env:LOCALAPPDATA 'Android\Sdk')
    ) | Where-Object { $_ -and (Test-Path -LiteralPath $_) } | Select-Object -First 1

    if (-not $sdk) { throw 'Android SDK topilmadi. ANDROID_HOME ni belgilang.' }
    $sdk
}

function Show-Artifact([string]$Path) {
    $size = (Get-Item -LiteralPath $Path).Length / 1MB
    Write-Host ("Tayyor: {0} ({1:N1} MB)" -f $Path, $size) -ForegroundColor Green
}
