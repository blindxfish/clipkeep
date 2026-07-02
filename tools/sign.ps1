<#
.SYNOPSIS
  Authenticode-sign one or more files, if signing is configured.

.DESCRIPTION
  Picks a signing method from environment variables, in priority order:

    1. Azure Trusted Signing (recommended) - uses the 'sign' dotnet global tool.
         CLIPFORGE_SIGN=trusted
         TS_ENDPOINT   e.g. https://weu.codesigning.azure.net/
         TS_ACCOUNT    Trusted Signing account name
         TS_PROFILE    certificate profile name
       (Azure auth via DefaultAzureCredential: 'az login', or AZURE_* env vars.)

    2. PFX file - uses signtool.
         CLIPFORGE_SIGN=pfx
         CLIPFORGE_PFX           path to the .pfx
         CLIPFORGE_PFX_PASSWORD  its password

    3. Certificate in the Windows store - uses signtool (works with OV/EV
       hardware tokens once the token's cert is present in the store).
         CLIPFORGE_SIGN=store
         CLIPFORGE_CERT_SUBJECT  cert subject substring, e.g. Nixon Software Solutions

  If CLIPFORGE_SIGN is unset/none, this is a no-op (prints a notice) so unsigned
  dev/CI builds still succeed. Every signature is RFC-3161 timestamped so it
  stays valid after the certificate expires.

.EXAMPLE
  ./sign.ps1 -Path dist\release\ClipForge-Setup-1.1.0-x64.exe
#>
param(
  [Parameter(Mandatory)][string[]]$Path,
  [string]$TimestampUrl = "http://timestamp.digicert.com"
)

$ErrorActionPreference = "Stop"

$method = $env:CLIPFORGE_SIGN
if ([string]::IsNullOrWhiteSpace($method) -or $method -eq "none") {
  Write-Host "  [sign] CLIPFORGE_SIGN not set - leaving files UNSIGNED." -ForegroundColor Yellow
  return
}

function Get-SignTool {
  $cmd = Get-Command signtool.exe -ErrorAction SilentlyContinue
  if ($cmd) { return $cmd.Source }
  $pf   = [Environment]::GetEnvironmentVariable('ProgramFiles')
  $pf86 = [Environment]::GetEnvironmentVariable('ProgramFiles(x86)')
  $roots = @($pf86, $pf) | Where-Object { $_ } | ForEach-Object { Join-Path $_ 'Windows Kits\10\bin' }
  $found = $roots | Where-Object { Test-Path $_ } | ForEach-Object {
    Get-ChildItem $_ -Recurse -Filter signtool.exe -ErrorAction SilentlyContinue |
      Where-Object { $_.FullName -match '\\x64\\' }
  } | Sort-Object FullName -Descending | Select-Object -First 1
  if (-not $found) { throw 'signtool.exe not found. Install the Windows 10/11 SDK.' }
  return $found.FullName
}

switch ($method) {
  "trusted" {
    foreach ($v in "TS_ENDPOINT","TS_ACCOUNT","TS_PROFILE") {
      if (-not (Get-Item "env:$v" -ErrorAction SilentlyContinue)) { throw "Trusted Signing requires env var $v" }
    }
    if (-not (Get-Command sign -ErrorAction SilentlyContinue)) {
      Write-Host "  [sign] installing 'sign' global tool..." -ForegroundColor Cyan
      dotnet tool install --global sign | Out-Null
    }
    foreach ($f in $Path) {
      Write-Host "  [sign] trusted-signing $f" -ForegroundColor Cyan
      sign code trusted-signing $f `
        --trusted-signing-endpoint $env:TS_ENDPOINT `
        --trusted-signing-account $env:TS_ACCOUNT `
        --trusted-signing-certificate-profile $env:TS_PROFILE `
        --timestamp-url $TimestampUrl
      if ($LASTEXITCODE -ne 0) { throw "sign failed for $f" }
    }
  }
  "pfx" {
    if (-not $env:CLIPFORGE_PFX) { throw "CLIPFORGE_PFX not set" }
    $st = Get-SignTool
    foreach ($f in $Path) {
      Write-Host "  [sign] pfx $f" -ForegroundColor Cyan
      & $st sign /fd SHA256 /tr $TimestampUrl /td SHA256 `
        /f $env:CLIPFORGE_PFX /p $env:CLIPFORGE_PFX_PASSWORD $f
      if ($LASTEXITCODE -ne 0) { throw "signtool failed for $f" }
    }
  }
  "store" {
    if (-not $env:CLIPFORGE_CERT_SUBJECT) { throw "CLIPFORGE_CERT_SUBJECT not set" }
    $st = Get-SignTool
    foreach ($f in $Path) {
      Write-Host "  [sign] store $f" -ForegroundColor Cyan
      & $st sign /fd SHA256 /tr $TimestampUrl /td SHA256 `
        /n $env:CLIPFORGE_CERT_SUBJECT $f
      if ($LASTEXITCODE -ne 0) { throw "signtool failed for $f" }
    }
  }
  default { throw "Unknown CLIPFORGE_SIGN value. Use trusted, pfx, store, or none." }
}
