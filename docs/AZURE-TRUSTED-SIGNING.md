# Signing ClipKeep with Azure Trusted Signing

Azure Trusted Signing is Microsoft's managed code-signing service (~$9.99/month).
It gives ClipKeep's direct-download `.exe` and installer builds a **trusted,
Microsoft-backed signature** so Windows shows **Nixon Software Solutions** as a
verified publisher and SmartScreen stops warning about an "unknown publisher" —
without you buying or storing a certificate on a hardware token.

Once it's set up, signing is automatic: `tools/build-all.ps1` calls
`tools/sign.ps1`, which signs the app exe and every installer.

> Official docs (authoritative if the portal UI differs from the steps below):
> https://learn.microsoft.com/azure/trusted-signing/

---

## Before you start

- An **Azure account with a subscription** (a pay-as-you-go subscription is
  fine). Sign up at https://portal.azure.com.
- **Nixon Software Solutions'** legal details (registered name, address, and a
  verifiable phone/website) for identity validation.
- **Eligibility note:** for a *public* certificate, Microsoft validates the
  identity. Organizations generally need a verifiable legal existence (Microsoft
  has required ~3 years of history for the public-trust org path); an
  **individual** validation path also exists (validated with a government ID).
  If the org path isn't available yet, the individual path still produces a
  trusted signature.

---

## Part A — One-time Azure setup (in the portal)

1. **Register the resource provider.**
   Portal → *Subscriptions* → your subscription → *Resource providers* → search
   `Microsoft.CodeSigning` → **Register**. (Wait until it shows "Registered".)

2. **Create a Trusted Signing account.**
   Portal → search **"Trusted Signing Accounts"** → **Create**.
   - Subscription + Resource group (create one, e.g. `rg-codesigning`).
   - **Region:** pick a supported one near you — this decides your signing
     **endpoint** (see Part B). e.g. *West Europe*, *North Europe*, *East US*.
   - Name: e.g. `nixon-signing`.
   - Pricing tier: **Basic** is enough for typical release volumes.
   - Review + create.

3. **Validate your identity.**
   Open the account → **Identity validations** → **New identity validation**.
   - Choose **Public** (required for public distribution).
   - Pick **Organization** (Nixon Software Solutions) or **Individual**.
   - Fill in the legal details exactly as registered and submit.
   - Microsoft reviews this — approval typically takes **a few hours to a few
     business days**. You may be contacted to confirm details. Wait for
     **"Completed / Approved"** before continuing.

4. **Create a certificate profile.**
   Account → **Certificate profiles** → **Create**.
   - Type: **Public Trust**.
   - Select the **approved identity validation** from step 3.
   - Name: e.g. `clipkeep-public` (this is your `TS_PROFILE`).

5. **Grant yourself the signer role (RBAC).**
   Account → **Access control (IAM)** → **Add role assignment** →
   role **"Trusted Signing Certificate Profile Signer"** → assign it to **your
   Azure user account** (the one you'll `az login` with). For CI, assign it to a
   **service principal** instead (see Part E).
   - Role changes can take a few minutes to propagate.

---

## Part B — Collect your three values

From the Trusted Signing account overview and the certificate profile:

| Value | Where | Example → env var |
|---|---|---|
| **Endpoint** | Account overview (region URI) | `https://weu.codesigning.azure.net/` → `TS_ENDPOINT` |
| **Account name** | The account resource name | `nixon-signing` → `TS_ACCOUNT` |
| **Certificate profile** | The profile you created | `clipkeep-public` → `TS_PROFILE` |

Common region endpoints (use the one matching your account's region):

- East US — `https://eus.codesigning.azure.net/`
- West Central US — `https://wcus.codesigning.azure.net/`
- West US 2 — `https://wus2.codesigning.azure.net/`
- West Europe — `https://weu.codesigning.azure.net/`
- North Europe — `https://neu.codesigning.azure.net/`

---

## Part C — Set up your machine

1. **Azure CLI** (for sign-in): install from
   https://learn.microsoft.com/cli/azure/install-azure-cli, then:
   ```powershell
   az login
   ```
   Sign in as the account you granted the signer role. If you have multiple
   subscriptions: `az account set --subscription "<name-or-id>"`.

2. **.NET SDK** — already required to build ClipKeep. `tools/sign.ps1`
   auto-installs the Microsoft **`sign`** global tool the first time it runs, so
   there's nothing else to install.

---

## Part D — Build signed releases

Set the env vars (in the same PowerShell session), then run the normal build:

```powershell
$env:CLIPFORGE_SIGN = "trusted"
$env:TS_ENDPOINT    = "https://weu.codesigning.azure.net/"   # your region
$env:TS_ACCOUNT     = "nixon-signing"                        # your account
$env:TS_PROFILE     = "clipkeep-public"                      # your profile

powershell -ExecutionPolicy Bypass -File tools\build-all.ps1
```

`build-all.ps1` signs the app exe *before* packaging and signs each installer
*after*, with RFC-3161 timestamps, and writes `dist/release/SHA256SUMS.txt`.

**Verify a signed build:**
```powershell
signtool verify /pa /v dist\release\ClipKeep-Setup-1.1.2-x64.exe
```
You should see a valid signature chaining to the Microsoft Trusted Signing
roots, with the subject naming Nixon Software Solutions.

> The Microsoft Store `.msix` files do **not** need this — the Store signs them
> on submission. Trusted Signing is only for the direct-download exe/installer.

---

## Part E — CI / unattended signing (optional)

For a build server (e.g. GitHub Actions) use a **service principal** instead of
`az login`:

1. Create an app registration / service principal in Entra ID.
2. Assign it the **Trusted Signing Certificate Profile Signer** role on the
   account (Part A step 5).
3. Provide these as environment variables/secrets — `DefaultAzureCredential`
   (used by the `sign` tool) picks them up automatically:
   ```
   AZURE_TENANT_ID
   AZURE_CLIENT_ID
   AZURE_CLIENT_SECRET
   ```
4. Set the same `CLIPFORGE_SIGN` / `TS_*` vars and run `tools\build-all.ps1`.

---

## Troubleshooting

- **"AuthorizationFailed" / 403 when signing** — the signer role isn't assigned
  to the identity you authenticated as, or hasn't propagated yet. Re-check Part A
  step 5 and wait a few minutes.
- **Signature succeeds but SmartScreen still warns briefly** — Trusted Signing
  trust is near-instant but can take a short time to fully propagate for a brand
  new publisher. It clears without any action from you.
- **`sign` tool not found** — ensure the .NET SDK is installed;
  `tools/sign.ps1` runs `dotnet tool install --global sign` on first use (make
  sure `%USERPROFILE%\.dotnet\tools` is on PATH).
- **Wrong endpoint** — `TS_ENDPOINT` must match the account's region exactly.

---

## Reference

- `tools/sign.ps1` — the signing wrapper (Trusted Signing / PFX / store cert).
- `tools/build-all.ps1` — calls the wrapper for every artifact.
- `docs/SIGNING.md` — signing overview and the other cert options.
