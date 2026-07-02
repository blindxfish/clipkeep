<#
.SYNOPSIS
  Create a GitHub release for ClipForge and upload the built installers + portables.

.DESCRIPTION
  Uses your existing Git credential (via `git credential fill`) to call the
  GitHub REST API — no separate token needed if you can already `git push`.
  Run this AFTER building the assets into dist\release\ (see tools\build-all.ps1),
  which should contain, per architecture (x64 / x86 / arm64):
    ClipForge-Setup-<ver>-<arch>.exe     (Inno Setup installer)
    ClipForge-<ver>-<arch>-portable.exe  (self-contained single-file exe)

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\publish-release.ps1
#>
param(
  [string]$Version = "1.1.1",
  [string]$Repo    = "blindxfish/clipforge"
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$tag  = "v$Version"

$assetDir = Join-Path $root "dist\release"
$assets = Get-ChildItem $assetDir -File -ErrorAction SilentlyContinue |
  Where-Object { $_.Name -like "*$Version*.exe" -or $_.Name -eq "SHA256SUMS.txt" }
if (-not $assets -or $assets.Count -eq 0) {
  throw "No release assets found in $assetDir. Build them first (tools\build-all.ps1)."
}

# Pull the GitHub token from your Git credential store (same one `git push` uses).
$fill  = "protocol=https`nhost=github.com`n" | git credential fill
$token = ($fill | Where-Object { $_ -like 'password=*' } | Select-Object -First 1) -replace '^password=', ''
if ([string]::IsNullOrWhiteSpace($token)) { throw "Could not obtain a GitHub token from git credentials. Try 'git push' once first." }

$headers = @{
  Authorization = "Bearer $token"
  "User-Agent"  = "clipforge-release"
  Accept        = "application/vnd.github+json"
}

$body = @"
## ClipKeep $Version

Local-first Windows clipboard manager — stores, organizes, and searches everything you copy. No cloud, no AI, no telemetry.

> ClipKeep is the app formerly branded ClipForge (the name was already taken on the Microsoft Store). Same app, same repo.

### What's new in $Version
- **Date on every clip** — each entry now leads with the date/time it was copied.
- **Date-range filter** — a From/To picker to narrow history by day.
- **Sort & counts** — "Newest / Oldest first" ordering plus live per-type counts in the sidebar.
- **Dark redesign** — custom title bar, rounded cards, restyled search, sidebar, details, and Settings; thin dark scrollbars and date pickers.
- **About section** — in Settings, with links to the project.

### Downloads
Pick the build for your CPU. All builds are self-contained (no .NET runtime required).

| Architecture | Installer | Portable |
|---|---|---|
| x64 (most PCs) | ``ClipKeep-Setup-$Version-x64.exe`` | ``ClipKeep-$Version-x64-portable.exe`` |
| ARM64 (Surface Pro X, etc.) | ``ClipKeep-Setup-$Version-arm64.exe`` | ``ClipKeep-$Version-arm64-portable.exe`` |
| x86 (32-bit) | ``ClipKeep-Setup-$Version-x86.exe`` | ``ClipKeep-$Version-x86-portable.exe`` |

Installers are per-user (Start Menu shortcut, uninstaller, no admin). Portables need no install — just run.

Verify your download against ``SHA256SUMS.txt`` (``Get-FileHash <file> -Algorithm SHA256``).

If these builds aren't code-signed yet, SmartScreen may warn about an *unknown publisher* — click **More info -> Run anyway**.

Made by [Nixon Software Solutions](https://nixonsolutions.org/). Free and open source.
"@

# Create the release (fails clearly if the tag release already exists).
$payload = @{ tag_name = $tag; name = "ClipKeep $Version"; body = $body; draft = $false; prerelease = $false } | ConvertTo-Json
$release = Invoke-RestMethod -Method Post -Headers $headers -ContentType "application/json" `
  -Uri "https://api.github.com/repos/$Repo/releases" -Body $payload
Write-Host "Created release: $($release.html_url)"

function Add-Asset($path, $name) {
  $uri = "https://uploads.github.com/repos/$Repo/releases/$($release.id)/assets?name=$name"
  Invoke-RestMethod -Method Post -Headers $headers -ContentType "application/octet-stream" -InFile $path -Uri $uri | Out-Null
  Write-Host "Uploaded $name"
}

foreach ($a in $assets) { Add-Asset $a.FullName $a.Name }

Write-Host "`nDone -> $($release.html_url)"
