<#
.SYNOPSIS
  Create a GitHub release for ClipForge and upload the built installers.

.DESCRIPTION
  Uses your existing Git credential (via `git credential fill`) to call the
  GitHub REST API — no separate token needed if you can already `git push`.
  Run this AFTER building the assets:
    dist\portable\ClipForge.exe          (self-contained portable exe)
    installer\ClipForge-Setup-<ver>.exe  (Inno Setup installer)

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File tools\publish-release.ps1
#>
param(
  [string]$Version = "1.1.0",
  [string]$Repo    = "blindxfish/clipforge"
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$tag  = "v$Version"

$installer = Join-Path $root "installer\ClipForge-Setup-$Version.exe"
$portable  = Join-Path $root "dist\portable\ClipForge.exe"
foreach ($f in @($installer, $portable)) {
  if (-not (Test-Path $f)) { throw "Missing asset: $f  (build it first)" }
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
## ClipForge $Version

Local-first Windows clipboard manager — stores, organizes, and searches everything you copy. No cloud, no AI, no telemetry.

### Downloads
| File | What it is |
|---|---|
| ``ClipForge-Setup-$Version.exe`` | Installer (Start Menu shortcut, uninstaller, per-user, no admin) |
| ``ClipForge-$Version-portable.exe`` | Portable single file — no install, just run |

Both are self-contained (no .NET runtime required). The builds aren't code-signed, so SmartScreen may warn about an *unknown publisher* — click **More info -> Run anyway**.

**Highlights:** clipboard capture (text + images), rule-based classification, SQLite FTS5 search, Quick Paste (Ctrl+Shift+V), favorites, sensitive-content exclusion, application blacklist, retention cleanup.
"@

# Create the release (idempotent-ish: fails clearly if the tag release already exists).
$payload = @{ tag_name = $tag; name = "ClipForge $Version"; body = $body; draft = $false; prerelease = $false } | ConvertTo-Json
$release = Invoke-RestMethod -Method Post -Headers $headers -ContentType "application/json" `
  -Uri "https://api.github.com/repos/$Repo/releases" -Body $payload
Write-Host "Created release: $($release.html_url)"

function Add-Asset($path, $name) {
  $uri = "https://uploads.github.com/repos/$Repo/releases/$($release.id)/assets?name=$name"
  Invoke-RestMethod -Method Post -Headers $headers -ContentType "application/octet-stream" -InFile $path -Uri $uri | Out-Null
  Write-Host "Uploaded $name"
}

Add-Asset $installer "ClipForge-Setup-$Version.exe"
Add-Asset $portable  "ClipForge-$Version-portable.exe"

Write-Host "`nDone -> $($release.html_url)"
