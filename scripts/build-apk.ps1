[CmdletBinding()]
param(
    [switch]$Install,
    [string]$Device
)

$ErrorActionPreference = 'Stop'
$env:MSBUILDDISABLENODEREUSE = '1'

. (Join-Path $PSScriptRoot 'common.ps1')

$repoRoot = Get-RepoRoot
$version = Get-AppVersion
$project = Join-Path $repoRoot 'src\mobile\VoltStream.Mobile\VoltStream.Mobile.csproj'
$sdk = Get-AndroidSdk

if (-not $env:VOLTSTREAM_KEYSTORE_PASS) {
    Write-Warning 'VOLTSTREAM_KEYSTORE_PASS berilmagan: APK release kaliti bilan imzolanmaydi va o''rnatilgan ilova ustidan yangilanmaydi.'
}

Write-Host "APK $version yig'ilmoqda..."
& dotnet publish $project -f net10.0-android -c Release "-p:AndroidSdkDirectory=$sdk"
if ($LASTEXITCODE -ne 0) { throw 'APK yig''ish xato bilan tugadi.' }

$apk = Get-ChildItem -Path (Join-Path $repoRoot 'src\mobile\VoltStream.Mobile\bin\Release\net10.0-android') `
    -Filter '*-Signed.apk' -File -Recurse | Sort-Object LastWriteTime -Descending | Select-Object -First 1

if (-not $apk) { throw 'Imzolangan APK topilmadi.' }

$final = Join-Path (Get-ArtifactsDir) "voltstream-$version.apk"
Copy-Item -LiteralPath $apk.FullName -Destination $final -Force
Show-Artifact $final

if (-not $Install) { return }

$adb = Join-Path $sdk 'platform-tools\adb.exe'
if (-not (Test-Path -LiteralPath $adb)) { throw 'adb topilmadi.' }

if (-not $Device) {
    $attached = @(& $adb devices | Select-Object -Skip 1 |
        Where-Object { $_ -match '\sdevice$' } |
        ForEach-Object { ($_ -split '\s+')[0] })

    if ($attached.Count -eq 0) { throw 'Ulangan qurilma yo''q. USB va "USB debugging" ni tekshiring.' }
    if ($attached.Count -gt 1) {
        & $adb devices -l
        throw "Bir nechta qurilma ulangan. Qaysi biriga o'rnatishni ko'rsating: -Device $($attached[0])"
    }
    $Device = $attached[0]
}

Write-Host "O'rnatilmoqda: $Device"
& $adb -s $Device install -r $final
if ($LASTEXITCODE -ne 0) { throw 'Qurilmaga o''rnatish xato bilan tugadi.' }
