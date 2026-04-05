#requires -Version 5.1
<#
.SYNOPSIS
    One-shot: publish Release MSIX, sign, trust cert, install (from repository root).

.DESCRIPTION
    Wrapper for scripts/Publish-AndInstall-Local.ps1. Uses $PSScriptRoot so it works when invoked
    via pwsh -File <full-path-to-this-script> from any current directory.

    Always passes **-RemoveExisting** first (reinstall when manifest version is unchanged). Append any
    extra parameters supported by Publish-AndInstall-Local.ps1 (do not pass -RemoveExisting again).

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File ./build-install.ps1

.EXAMPLE
    pwsh -NoProfile -ExecutionPolicy Bypass -File ./build-install.ps1 -SignedPublish
#>
$ErrorActionPreference = 'Stop'
$child = Join-Path $PSScriptRoot 'scripts\Publish-AndInstall-Local.ps1'
if (-not (Test-Path -LiteralPath $child)) {
    throw "Expected script not found: $child"
}

& $child -RemoveExisting @args