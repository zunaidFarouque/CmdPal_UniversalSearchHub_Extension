# Install / reinstall the MSIX after you change code

**Audience:** Humans and AI assistants. Use this doc to rebuild and install **CmdPal Search Hub** on Windows after modifying the project.

## Facts (do not skip)

| Fact | Implication |
|------|-------------|
| `Package.appxmanifest` **Publisher** must match the signing certificate **Subject** | Default publisher is `CN=Zunaid Farouque`. Any PFX or self-signed cert used to sign the MSIX must use that exact subject unless you change the manifest. |
| **Unsigned** MSIX from `dotnet publish` / unsigned CI | Windows returns **0x800B0100** (*No signature was present*). **Developer Mode alone does not fix this** for a completely unsigned package. |
| Trust store for sideload | The signing cert (or its public `.cer`) must be in **Local Machine → Trusted People** for a normal install. **Current user** Trusted People is often **not** enough (**0x800B010A** / **0x800B0109**). |
| Same version reinstall | If the package is already installed with the **same** `Identity Version`, install may fail. Prefer bumping **Version** in `Package.appxmanifest`, **or** remove the old package first, **or** use the script’s **`-ForceUpdateFromAnyVersion`** (see script help). |

## Prerequisites

- **Windows 10/11** (x64 for the default RID).
- **[.NET 9 SDK](https://dotnet.microsoft.com/download)** (`dotnet --version` shows 9.x).
- **Windows SDK** (for `signtool.exe`) when using **ephemeral local signing** — usually installed with **Visual Studio** workload “Desktop development with C++” or standalone [Windows SDK](https://developer.microsoft.com/windows/downloads/windows-sdk/). The script searches under `C:\Program Files (x86)\Windows Kits\10\bin\`.
- **Administrator UAC prompt** when the script must import a certificate into **Local Machine** stores.
- Repo path: **`CmdPal_UniversalSearchHub_Extension`** (solution + project folder layout as in this repository).

## Install only (MSIX already built)

From the **repository root**, when you already have a `.msix` (for example under `CmdPal_UniversalSearchHub_Extension/AppPackages/...` after publish or from CI):

```powershell
pwsh ./scripts/Install-LocalSignedMsix.ps1
```

Optional explicit file:

```powershell
pwsh ./scripts/Install-LocalSignedMsix.ps1 -MsixPath "D:\full\path\to\CmdPal_UniversalSearchHub_Extension_0.0.1.0_x64.msix"
```

The script picks the newest matching `AppPackages` MSIX when `-MsixPath` is omitted. It **signs a temp copy** with a short-lived cert whose subject matches the manifest publisher, then opens **UAC** to import the public cert into **Local Machine\Trusted People** and run `Add-AppxPackage`. **Save this script as UTF-8** (not UTF-16) so Windows PowerShell 5.1 parses it.

**Also surfaced for AI:** root **`AGENTS.md`** and **`.cursor/rules/msix-local-install.mdc`**.

## Quick path (automated — build + install)

From the **repository root** (folder containing `CmdPal_UniversalSearchHub_Extension.sln`):

```powershell
pwsh ./scripts/Publish-AndInstall-Local.ps1
```

This will:

1. `dotnet restore` / `dotnet publish` (Release, **win-x64**, MSIX sideload package — mirrors CI flags).
2. Locate the newest `.msix` under `**/AppPackages/**`.
3. If the package is **unsigned**, **sign a temporary copy** with a short-lived self-signed cert matching `CN=Zunaid Farouque`, then launch an **elevated** PowerShell to import that cert into **Local Machine\Trusted People** and run `Add-AppxPackage`.

Optional switches (see script comment-based help):

```powershell
pwsh ./scripts/Publish-AndInstall-Local.ps1 -RemoveExisting
```

Use **`-RemoveExisting`** if a previous install blocks an update (same or lower version without `-ForceUpdateFromAnyVersion` behavior).

```powershell
pwsh ./scripts/Publish-AndInstall-Local.ps1 -SignedPublish
```

Uses **`CmdPal_CI_Signing.pfx`** at the repo root (create with `scripts/New-CmdPalSigningCertificate.ps1`). You must already trust **`CmdPal_CI_Public.cer`** on the machine (one-time). No ephemeral signing.

```powershell
pwsh ./scripts/Publish-AndInstall-Local.ps1 -SkipPublish -MsixPath "D:\full\path\to\package.msix"
```

Install only: re-signs a copy with an **ephemeral** cert (default path). If the `.msix` is **already signed** and you already trust that publisher, prefer **`Add-AppxPackage`** or **`Install-DownloadedMsix.ps1`** instead of `-SkipPublish` here (avoid double-signing).

## Manual path (high level)

1. **Publish** (same MSBuild properties as CI — see `Publish-AndInstall-Local.ps1` or `.github/workflows/build.yml`).
2. **If unsigned:** sign with `signtool` using a cert whose subject matches the manifest Publisher; export/trust the **.cer** to **Local Machine\Trusted People**; then `Add-AppxPackage`.
3. **If signed with your CI PFX:** trust **`CmdPal_CI_Public.cer`** once, then install the `.msix`.

Detailed signing and GitHub Actions secrets: **`docs/Signing.md`**.

## CI artifacts instead of local build

1. `pwsh ./scripts/Download-LatestBuild.ps1` → artifacts under **`artifacts/run-<id>/`**.
2. **Signed CI:** install **`install-trust-certificate`** `.cer` (Local Machine → Trusted People), then install the `.msix` from **`msix-win-x64`**, or use **`scripts/Install-DownloadedMsix.ps1`** (elevated if importing the cert).
3. **Unsigned CI:** use **`Publish-AndInstall-Local.ps1 -SkipPublish -MsixPath <path\to\downloaded.msix>`** so ephemeral signing + elevated install runs on that file, **or** configure **`CMDPAL_PFX_BASE64`** and download a signed run.

## Verify installation

```powershell
Get-AppxPackage -Name 'ZunaidFarouque.CmdPalSearchHub'
```

## Troubleshooting (HRESULTs)

| Code | Meaning | What to do |
|------|---------|------------|
| **0x800B0100** | No digital signature on the package | Sign the MSIX (script default) or use a signed publish / signed CI build. |
| **0x800B010A** / **0x800B0109** | Publisher / chain not trusted | Import the **matching** public `.cer` to **Local Machine → Trusted People** (elevated). Use the **same** cert that signed the `.msix`. |
| Deploy “higher version already installed” | Version not increased | Bump `Identity Version` in `Package.appxmanifest`, or remove the app, or use **`-ForceUpdateFromAnyVersion`** in the script. |

## Related scripts

| Script | Role |
|--------|------|
| `scripts/Install-LocalSignedMsix.ps1` | **Sign (if needed) + install** when an `.msix` already exists (no `dotnet publish`) |
| `scripts/Publish-AndInstall-Local.ps1` | **Build + sign (optional) + install** after code changes |
| `scripts/New-CmdPalSigningCertificate.ps1` | Create **`CmdPal_CI_Signing.pfx`** + **`CmdPal_CI_Public.cer`** for repeatable local/CI signing |
| `scripts/Download-LatestBuild.ps1` | Download latest successful workflow artifacts |
| `scripts/Install-DownloadedMsix.ps1` | Install from **`artifacts/run-*`** when you already have a `.msix` (+ optional `.cer`) |

## AI checklist (copy-paste workflow)

1. Confirm OS is **Windows** and user is at **repo root**.
2. If an `.msix` is **already built** (under `AppPackages` or a path the user gave), run **`pwsh ./scripts/Install-LocalSignedMsix.ps1`** (or **`-MsixPath`**). Otherwise run **`pwsh ./scripts/Publish-AndInstall-Local.ps1`** to build and install.
3. If UAC appears, user must **approve** (certificate import + install).
4. On failure, read **HRESULT** from output, map to the table above; ensure **signtool** / **.NET 9** / **Trusted People** (Local Machine).
5. For “same version” errors, bump **`Package.appxmanifest`** version or pass **`-RemoveExisting`** / **`-ForceUpdateFromAnyVersion`** per script documentation.
