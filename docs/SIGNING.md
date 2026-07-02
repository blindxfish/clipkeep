# Code signing & Windows trust

Unsigned ClipForge builds trigger a Windows SmartScreen / "unknown publisher"
warning. That warning is **only** removed by Authenticode-signing the binaries
with a code-signing certificate whose identity Windows can verify — no build
flag or metadata field can substitute for it.

The build pipeline is signing-ready: `tools/build-all.ps1` signs the app exe
(before packaging) and each installer via `tools/sign.ps1`, which is a no-op
until you configure a signing method. Once configured, `build-all.ps1` produces
fully signed assets with zero extra steps.

## Choosing a certificate

| Option | ~Cost | SmartScreen | Notes |
|---|---|---|---|
| **Azure Trusted Signing** | ~$10/mo | Good reputation quickly | Microsoft-managed. Best value. Needs a validated identity. |
| **OV certificate** | ~$200–500/yr | Builds up over downloads (early users may still be warned) | Hardware token/HSM required. Budget option: Certum. |
| **EV certificate** | ~$400–700/yr | **Instant**, no warning | Hardware token; strictest vetting. |

All real certificates require validating **Nixon Software Solutions** as a legal
entity (registered business, verifiable address/phone, often a D-U-N-S number).
Allow several days to a few weeks for vetting.

> Recommendation: **Azure Trusted Signing** for cost + near-instant trust, or an
> **EV** cert if you want zero warnings on day one without Azure.

## Configuring the pipeline

Set env vars, then run `tools/build-all.ps1` — signing happens automatically.

### Azure Trusted Signing (recommended)
```powershell
$env:CLIPFORGE_SIGN = "trusted"
$env:TS_ENDPOINT    = "https://weu.codesigning.azure.net/"   # your region
$env:TS_ACCOUNT     = "<trusted-signing-account>"
$env:TS_PROFILE     = "<certificate-profile>"
az login                                                     # or set AZURE_* creds
powershell -ExecutionPolicy Bypass -File tools\build-all.ps1
```

### PFX file (e.g. an OV cert exported to .pfx)
```powershell
$env:CLIPFORGE_SIGN         = "pfx"
$env:CLIPFORGE_PFX          = "C:\path\to\nixon.pfx"
$env:CLIPFORGE_PFX_PASSWORD = "<password>"
powershell -ExecutionPolicy Bypass -File tools\build-all.ps1
```

### Certificate in the Windows store (EV/OV hardware token)
```powershell
$env:CLIPFORGE_SIGN        = "store"
$env:CLIPFORGE_CERT_SUBJECT = "Nixon Software Solutions"
powershell -ExecutionPolicy Bypass -File tools\build-all.ps1
```

`sign.ps1` timestamps every signature (RFC-3161) so it remains valid after the
certificate expires. Requires the Windows 10/11 SDK (`signtool.exe`) for the
`pfx`/`store` methods; the `trusted` method auto-installs the `sign` dotnet tool.

## Verifying a build
```powershell
signtool verify /pa /v dist\release\ClipForge-Setup-1.1.0-x64.exe
Get-FileHash dist\release\ClipForge-Setup-1.1.0-x64.exe -Algorithm SHA256
```
`build-all.ps1` also emits `dist/release/SHA256SUMS.txt` for download verification.

## Beyond signing — distributing everywhere

- **Microsoft Store / winget**: publishing to the Store gives Microsoft-backed
  trust and auto-updates; a `winget` manifest makes `winget install ClipForge`
  work. Both still benefit from signing.
- **SmartScreen reputation**: with OV certs, warnings fade as download volume
  accrues. EV and Trusted Signing shortcut this.
- **Keep the signing identity stable** — changing certificates resets reputation.
