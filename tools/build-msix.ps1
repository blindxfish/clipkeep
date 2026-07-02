<#
.SYNOPSIS
  Build MSIX packages of ClipForge for the Microsoft Store (x64 / x86 / arm64).

.DESCRIPTION
  For each architecture this publishes a self-contained (loose-file) build,
  stamps the MSIX manifest, and packs a .msix into dist\release\.

  Store identity comes from env vars (with placeholder defaults you MUST replace
  with the values Partner Center assigns when you reserve the app name):
    MSIX_IDENTITY_NAME     e.g. 1234NixonSoftware.ClipForge
    MSIX_PUBLISHER         e.g. CN=ABCD1234-1234-1234-1234-1234567890AB
    MSIX_PUBLISHER_DISPLAY e.g. Nixon Software Solutions
    MSIX_DISPLAY_NAME      the reserved Store name (must match EXACTLY), e.g.
                           "ClipForge - Clipboard Manager". Defaults to ClipForge.

  The Microsoft Store SIGNS the package on submission, so the .msix is produced
  unsigned. To sideload-test locally, sign it with a self-signed cert whose
  subject equals MSIX_PUBLISHER (see docs\STORE.md).

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\build-msix.ps1
#>
param(
  [string]$Version = "1.1.1",
  [string[]]$Arch  = @("x64","x86","arm64")
)

$ErrorActionPreference = "Stop"
$root     = Split-Path $PSScriptRoot -Parent
$proj     = Join-Path $root "src\ClipForge.App\ClipForge.App.csproj"
$pkgDir   = Join-Path $root "packaging\msix"
$template = Join-Path $pkgDir "AppxManifest.template.xml"
$assets   = Join-Path $pkgDir "Assets"
$release  = Join-Path $root "dist\release"
$logo     = Join-Path $root "src\ClipForge.App\Assets\logo.png"

# Store identity (replace placeholders with Partner Center values before submitting).
$identityName     = if ($env:MSIX_IDENTITY_NAME)     { $env:MSIX_IDENTITY_NAME }     else { "NixonSoftwareSolutions.ClipKeep" }
$publisher        = if ($env:MSIX_PUBLISHER)         { $env:MSIX_PUBLISHER }         else { "CN=Nixon Software Solutions" }
$publisherDisplay = if ($env:MSIX_PUBLISHER_DISPLAY) { $env:MSIX_PUBLISHER_DISPLAY } else { "Nixon Software Solutions" }
$displayName      = if ($env:MSIX_DISPLAY_NAME)      { $env:MSIX_DISPLAY_NAME }      else { "ClipKeep" }
if ($publisher -eq "CN=Nixon Software Solutions") {
  Write-Host "  [msix] Using PLACEHOLDER identity. Replace via MSIX_* env vars before Store submission." -ForegroundColor Yellow
}

# Locate makeappx.exe from the Windows SDK.
$pf86 = [Environment]::GetEnvironmentVariable('ProgramFiles(x86)')
$makeappx = @($pf86, $env:ProgramFiles) | Where-Object { $_ } |
  ForEach-Object { Join-Path $_ 'Windows Kits\10\bin' } | Where-Object { Test-Path $_ } |
  ForEach-Object { Get-ChildItem $_ -Recurse -Filter makeappx.exe -ErrorAction SilentlyContinue | Where-Object { $_.FullName -match '\\x64\\' } } |
  Sort-Object FullName -Descending | Select-Object -First 1
if (-not $makeappx) { throw "makeappx.exe not found. Install the Windows 10/11 SDK." }

# --- Generate Store tile assets from logo.png (transparent, centered, aspect-fit). ---
function New-Tile($w, $h, $outPath, $pad = 0.86) {
  Add-Type -AssemblyName System.Drawing
  $src = [System.Drawing.Image]::FromFile($logo)
  try {
    $bmp = New-Object System.Drawing.Bitmap($w, $h)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.Clear([System.Drawing.Color]::Transparent)
    $box = [Math]::Min($w, $h) * $pad
    $scale = [Math]::Min($box / $src.Width, $box / $src.Height)
    $dw = $src.Width * $scale; $dh = $src.Height * $scale
    $g.DrawImage($src, ($w - $dw) / 2, ($h - $dh) / 2, $dw, $dh)
    $bmp.Save($outPath, [System.Drawing.Imaging.ImageFormat]::Png)
    $g.Dispose(); $bmp.Dispose()
  } finally { $src.Dispose() }
}

New-Item -ItemType Directory -Force -Path $assets | Out-Null
$tiles = @{
  "StoreLogo.png"        = @(50,50)
  "Square44x44Logo.png"  = @(44,44)
  "Square71x71Logo.png"  = @(71,71)
  "Square150x150Logo.png"= @(150,150)
  "Square310x310Logo.png"= @(310,310)
  "Wide310x150Logo.png"  = @(310,150)
}
Write-Host "=== Generating tile assets ==="
foreach ($name in $tiles.Keys) {
  $dim = $tiles[$name]
  New-Tile $dim[0] $dim[1] (Join-Path $assets $name)
}

New-Item -ItemType Directory -Force -Path $release | Out-Null
$manifestTemplate = Get-Content $template -Raw

foreach ($arch in $Arch) {
  Write-Host "=== MSIX $arch ==="
  $rid   = "win-$arch"
  $stage = Join-Path $root "dist\msix-stage-$arch"
  Remove-Item -Recurse -Force $stage -ErrorAction SilentlyContinue
  New-Item -ItemType Directory -Force -Path $stage | Out-Null

  # Loose-file self-contained publish (single-file is discouraged inside MSIX).
  dotnet publish $proj -c Release -r $rid --self-contained true `
    -p:PublishSingleFile=false -o $stage --nologo -v q | Out-Null
  if ($LASTEXITCODE -ne 0) { throw "publish failed for $rid" }
  Get-ChildItem $stage -Filter *.pdb -Recurse | Remove-Item -Force -ErrorAction SilentlyContinue

  # Stamp the manifest and copy assets into the package root.
  $manifest = $manifestTemplate `
    -replace '__IDENTITY_NAME__', $identityName `
    -replace '__PUBLISHER__', $publisher `
    -replace '__PUBLISHER_DISPLAY__', $publisherDisplay `
    -replace '__DISPLAY_NAME__', $displayName `
    -replace '__VERSION__', "$Version.0" `
    -replace '__ARCH__', $arch
  Set-Content -Path (Join-Path $stage "AppxManifest.xml") -Value $manifest -Encoding UTF8
  Copy-Item $assets (Join-Path $stage "Assets") -Recurse -Force

  $out = Join-Path $release "ClipKeep-$Version-$arch.msix"
  & $makeappx.FullName pack /o /d $stage /p $out | Out-Null
  if ($LASTEXITCODE -ne 0) { throw "makeappx failed for $arch" }
  Write-Host "  -> $out"
}

Write-Host "`n=== MSIX packages ==="
Get-ChildItem $release -File -Filter *.msix | ForEach-Object { "{0,-40} {1,7:N1} MB" -f $_.Name, ($_.Length / 1MB) }
