[CmdletBinding()]
param(
    [switch]$SkipImage
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'common.ps1')

$repoRoot = Get-RepoRoot
$version = Get-AppVersion
$stageDir = Join-Path ([IO.Path]::GetTempPath()) "voltstream-server-$version"

if (Test-Path -LiteralPath $stageDir) { Remove-Item -LiteralPath $stageDir -Recurse -Force }
New-Item -ItemType Directory -Path $stageDir -Force | Out-Null

if (-not $SkipImage) {
    & docker version --format '{{.Server.Version}}' | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Docker javob bermadi. Docker Desktop ishga tushirilganini tekshiring.' }

    Write-Host "Docker image voltstream-api:$version yig'ilmoqda (web build ham shu yerda)..."
    & docker build -f (Join-Path $repoRoot 'src\backend\VoltStream.WebApi\Dockerfile') `
        -t "voltstream-api:$version" -t 'voltstream-api:latest' $repoRoot
    if ($LASTEXITCODE -ne 0) { throw 'Docker image yig''ish xato bilan tugadi.' }

    Write-Host 'Image faylga saqlanmoqda...'
    & docker save -o (Join-Path $stageDir "voltstream-api-$version.tar") "voltstream-api:$version" 'voltstream-api:latest'
    if ($LASTEXITCODE -ne 0) { throw 'docker save xato bilan tugadi.' }
}

foreach ($item in @('docker-compose.yml', '.env.example', 'docs\DEPLOY.md', 'docs\upgrade-from-v1.sql')) {
    Copy-Item -LiteralPath (Join-Path $repoRoot $item) -Destination $stageDir -Force
}

$readme = @"
# VoltStream server $version

1. Docker Desktop o'rnatilgan bo'lsin.
2. Image'ni yuklang (agar tar fayl shu papkada bo'lsa):
   docker load -i voltstream-api-$version.tar
3. .env.example ni .env deb nusxalang va to'ldiring (JWT_SECRET ni tasodifiy yarating).
4. Ishga tushiring:
   docker compose up -d
5. Tekshiring:
   curl -f http://localhost:5000/api/health

Mavjud bazani ko'chirish, tunnel va telefon (APK) bo'yicha DEPLOY.md ga qarang.
Yangilash: yangi tar faylni docker load qiling va docker compose up -d ni qayta bajaring.
"@

Set-Content -LiteralPath (Join-Path $stageDir 'README.md') -Value $readme -Encoding UTF8

$final = Join-Path (Get-ArtifactsDir) "voltstream-server-$version.zip"
if (Test-Path -LiteralPath $final) { Remove-Item -LiteralPath $final -Force }

Write-Host 'Arxivlanmoqda...'
Compress-Archive -Path (Join-Path $stageDir '*') -DestinationPath $final
Remove-Item -LiteralPath $stageDir -Recurse -Force
Show-Artifact $final
