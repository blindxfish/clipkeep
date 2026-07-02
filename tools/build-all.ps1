<#
.SYNOPSIS
  Build ClipForge release assets for every supported Windows architecture.

.DESCRIPTION
  For each of x64 / x86 / arm64 this publishes a self-contained single-file exe
  and compiles the Inno Setup installer around it, staging final-named artifacts
  into dist\release\ ready for tools\publish-release.ps1.

  Requires the .NET SDK and Inno Setup 6 (ISCC.exe). If ISCC isn't on PATH the
  script looks in the default per-user and Program Files install locations.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\build-all.ps1
#>
param(
  [string]$Version = "1.1.0"
)

$ErrorActionPreference = "Stop"
$root    = Split-Path $PSScriptRoot -Parent
$proj    = Join-Path $root "src\ClipForge.App\ClipForge.App.csproj"
$iss     = Join-Path $root "installer\ClipForge.iss"
$release = Join-Path $root "dist\release"

$iscc = @(
  "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
  "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
  "C:\Program Files\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw "ISCC.exe (Inno Setup 6) not found. Install it from https://jrsoftware.org/isdl.php" }

New-Item -ItemType Directory -Force -Path $release | Out-Null
Get-ChildItem $release -File -ErrorAction SilentlyContinue | Remove-Item -Force

$targets = @(
  @{ Rid = "win-x64";   Arch = "x64"   },
  @{ Rid = "win-x86";   Arch = "x86"   },
  @{ Rid = "win-arm64"; Arch = "arm64" }
)

foreach ($t in $targets) {
  $arch   = $t.Arch
  $outDir = Join-Path $root "dist\portable-$arch"
  Write-Host "=== Publishing $($t.Rid) ==="
  Remove-Item -Recurse -Force $outDir -ErrorAction SilentlyContinue
  dotnet publish $proj -c Release -r $t.Rid --self-contained true `
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $outDir --nologo -v q | Out-Null
  if ($LASTEXITCODE -ne 0) { throw "publish failed for $($t.Rid)" }

  $exe = Join-Path $outDir "ClipForge.exe"
  if (-not (Test-Path $exe)) { throw "missing published exe: $exe" }
  Copy-Item $exe (Join-Path $release "ClipForge-$Version-$arch-portable.exe") -Force

  Write-Host "=== Compiling installer $arch ==="
  & $iscc "/DMyArch=$arch" "/DMySourceExe=$exe" $iss | Out-Null
  if ($LASTEXITCODE -ne 0) { throw "ISCC failed for $arch" }

  $setup = Join-Path $root "installer\ClipForge-Setup-$Version-$arch.exe"
  if (-not (Test-Path $setup)) { throw "missing installer: $setup" }
  Copy-Item $setup (Join-Path $release "ClipForge-Setup-$Version-$arch.exe") -Force
}

Write-Host "`n=== Release assets ($release) ==="
Get-ChildItem $release -File | ForEach-Object { "{0,-44} {1,7:N1} MB" -f $_.Name, ($_.Length / 1MB) }
