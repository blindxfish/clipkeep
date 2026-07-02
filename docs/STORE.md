# Publishing ClipKeep to the Microsoft Store (MSIX)

The Store distributes ClipKeep as an **MSIX** package. The big advantage: the
**Store signs the package on submission**, so users get a fully trusted install
with **no SmartScreen warning and no code-signing certificate to buy**. It also
gives automatic updates.

> "ClipKeep" is the Store/product brand (the bare word "ClipForge" was already
> taken on the Store). The executable is still `ClipForge.exe` and the code/repo
> names are unchanged; only the user-visible name is ClipKeep.

`tools/build-msix.ps1` produces `dist/release/ClipKeep-<ver>-<arch>.msix` for
x64 / x86 / arm64. Assets are generated from `src/ClipForge.App/Assets/logo.png`.

## One-time setup (only you can do this)

1. **Create a Partner Center account** — https://partner.microsoft.com/dashboard
   (individual ~$19, company ~$99, one-time). A **company** account lets the
   listing show *Nixon Software Solutions* as the publisher.
2. **Reserve the name "ClipKeep"** under Apps and games (confirmed available).
   The reserved name becomes the app's Store DisplayName.
3. From the app's **Product identity** page, copy the values Microsoft assigns
   and build with them:

   ```powershell
   $env:MSIX_IDENTITY_NAME     = "<Package/Identity/Name>"       # e.g. 1234NixonSoftware.ClipKeep
   $env:MSIX_PUBLISHER         = "<Package/Identity/Publisher>"  # e.g. CN=ABCD1234-....
   $env:MSIX_PUBLISHER_DISPLAY = "Nixon Software Solutions"
   $env:MSIX_DISPLAY_NAME      = "ClipKeep"
   powershell -ExecutionPolicy Bypass -File tools\build-msix.ps1
   ```

   Without the identity vars the script uses a **placeholder identity** — fine
   for local testing, but the Store rejects it until it matches your account.
   `MSIX_DISPLAY_NAME` defaults to `ClipKeep`.

## Submit

Upload the three `.msix` files (x64 / x86 / arm64) as packages in a new
submission. Do **not** sign them yourself — the Store re-signs. Fill in the
listing (description, screenshots, the 300x300 Store logo, privacy policy URL),
then submit for certification. You can upload all three architectures in one
submission; the Store hands each device the right one.

## Testing an MSIX locally (optional, before you have Partner Center)

Unsigned MSIX can't be installed by double-click. To sideload-test:

```powershell
# one-time: create a self-signed cert whose subject matches MSIX_PUBLISHER
$cert = New-SelfSignedCertificate -Type Custom -Subject "CN=Nixon Software Solutions" `
  -KeyUsage DigitalSignature -CertStoreLocation "Cert:\CurrentUser\My" `
  -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3")
# trust it (admin), then sign + install:
signtool sign /fd SHA256 /a /f <exported.pfx> /p <pw> dist\release\ClipKeep-1.1.1-x64.msix
Add-AppxPackage dist\release\ClipKeep-1.1.1-x64.msix
```

## Known limitation under MSIX — auto-start

ClipKeep's "Launch on Windows startup" writes an `HKCU\...\Run` value. MSIX
**virtualizes the registry**, so that toggle does **not** actually register the
app to run at login when installed from the Store.

The MSIX-correct approach is a `windows.startupTask` manifest extension plus the
`Windows.ApplicationModel.StartupTask` API to enable/disable it. That's a code
change (WinRT). Until it's implemented, Store users can still start the app from
the Start Menu and add it to startup manually via **Settings -> Apps -> Startup**.
Ask if you want this wired up properly for the packaged build.

## Which package for which channel?

| Channel | Format | Trust | Cert needed? |
|---|---|---|---|
| Microsoft Store | `.msix` (this doc) | Store-signed | **No** |
| Direct download / your site | `.exe` installer + portable (see SIGNING.md) | your signature | Yes (see SIGNING.md) |

Both are built from the same code; ship whichever channels you want.
