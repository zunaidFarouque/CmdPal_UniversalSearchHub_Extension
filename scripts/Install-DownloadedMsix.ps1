# Installs an MSIX from artifacts/run-<id>/ after Download-LatestBuild.ps1.
#
# Signed CI run: import CmdPal_CI_Public.cer to Local Machine "Trusted People" first
# (this script can do that when run elevated), then install the .msix.
#
# Unsigned run (no install-trust-certificate artifact): enable Windows Developer Mode
# (Settings → Privacy & security → For developers → Developer Mode), then run this script.
#
# Usage (repo root):
#   pwsh ./scripts/Install-DownloadedMsix.ps1 -RunId 23984141227
#   pwsh ./scripts/Install-DownloadedMsix.ps1 -MsixPath "...\CmdPal_..._x64.msix"

[CmdletBinding()]
param(
    [string] $RunId,
    [string] $MsixPath,
    [switch] $SkipCertificate
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$artifactsRoot = Join-Path $repoRoot 'artifacts'

if (-not $MsixPath) {
    if (-not $RunId) {
        $runDirs = @(Get-ChildItem -Path $artifactsRoot -Directory -ErrorAction SilentlyContinue | Where-Object { $_.Name -match '^run-\d+$' } | Sort-Object { [long]($_.Name -replace '^run-','') } -Descending)
        if ($runDirs.Count -eq 0) {
            throw "No artifacts/run-* folder found under $artifactsRoot. Run Download-LatestBuild.ps1 first or pass -MsixPath."
        }
        $runDir = $runDirs[0].FullName
        Write-Host "Using latest run folder: $($runDirs[0].Name)"
    }
    else {
        $runDir = Join-Path $artifactsRoot "run-$RunId"
        if (-not (Test-Path -LiteralPath $runDir)) {
            throw "Run folder not found: $runDir"
        }
    }
    $msixFiles = @(Get-ChildItem -LiteralPath $runDir -Recurse -Filter '*.msix' -File -ErrorAction SilentlyContinue)
    if ($msixFiles.Count -eq 0) {
        throw "No .msix file under $runDir"
    }
    if ($msixFiles.Count -gt 1) {
        Write-Warning "Multiple .msix files found; using: $($msixFiles[0].FullName)"
    }
    $MsixPath = $msixFiles[0].FullName
}

$MsixPath = (Resolve-Path -LiteralPath $MsixPath).Path

try {
    Import-Module Appx -ErrorAction Stop
}
catch {
    throw 'Appx module is not available (need Windows with full AppX support). Run this script on your Windows PC in PowerShell 5.1 or pwsh.'
}

if (-not $SkipCertificate) {
    $cer = $null
    if ($MsixPath -match '[\\/]run-(\d+)[\\/]') {
        $runFolder = Join-Path $artifactsRoot "run-$($Matches[1])"
        if (Test-Path -LiteralPath $runFolder) {
            $cer = Get-ChildItem -LiteralPath $runFolder -Recurse -Filter 'CmdPal_CI_Public.cer' -File -ErrorAction SilentlyContinue |
                Select-Object -First 1
        }
    }
    if (-not $cer) {
        $cer = Get-ChildItem -LiteralPath $artifactsRoot -Recurse -Filter 'CmdPal_CI_Public.cer' -File -ErrorAction SilentlyContinue |
            Select-Object -First 1
    }
    if ($cer) {
        Write-Host "Found certificate: $($cer.FullName)"
        $isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
        if (-not $isAdmin) {
            Write-Warning @"
Run PowerShell as Administrator once, then execute:

  Import-Certificate -FilePath '$($cer.FullName)' -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople'

Or double-click the .cer and install to Local Machine -> Trusted People (see docs/Signing.md).
"@
        }
        else {
            Import-Certificate -FilePath $cer.FullName -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople' | Out-Null
            Write-Host 'Imported public certificate to Local Machine Trusted People.'
        }
    }
    else {
        Write-Warning @"
No CmdPal_CI_Public.cer found (unsigned CI build or only msix-win-x64 downloaded).
Enable Developer Mode: Settings -> Privacy & security -> For developers -> Developer Mode ON
Then re-run this script. Or add CMDPAL_PFX_BASE64 to GitHub Actions, download install-trust-certificate from a new run, and trust the .cer (docs/Signing.md).
"@
    }
}

Write-Host "Installing: $MsixPath"
Add-AppxPackage -Path $MsixPath
Write-Host 'Done.'
