[CmdletBinding()]
param(
    [string]$Repository = 'muqimjon/volt-stream'
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'common.ps1')

$repoRoot = Get-RepoRoot
$version = Get-AppVersion
$builder = 'multiarch-builder'

& docker buildx inspect $builder *> $null
if ($LASTEXITCODE -ne 0) {
    & docker buildx create --name $builder --driver docker-container | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'buildx builder yaratilmadi.' }
}

Write-Host "Image ${Repository}:$version (linux/amd64, linux/arm64) yig'ilib Docker Hub'ga yuborilmoqda..."
& docker buildx build --builder $builder --platform linux/amd64,linux/arm64 `
    -f (Join-Path $repoRoot 'src\backend\VoltStream.WebApi\Dockerfile') `
    -t "${Repository}:$version" -t "${Repository}:latest" --push $repoRoot
if ($LASTEXITCODE -ne 0) { throw 'Image yig''ish yoki yuborish xato bilan tugadi (docker login qilinganmi?).' }

Write-Host "Tayyor: ${Repository}:$version va ${Repository}:latest" -ForegroundColor Green
