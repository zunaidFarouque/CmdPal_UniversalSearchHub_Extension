# Creates a self-signed certificate for MSIX signing (local + GitHub Actions).
# Subject MUST match Package.appxmanifest Identity Publisher.
# Run: .\scripts\New-CmdPalSigningCertificate.ps1

$ErrorActionPreference = 'Stop'
$subject = 'CN=Zunaid Farouque'
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$pfxPath = Join-Path $repoRoot 'CmdPal_CI_Signing.pfx'
$cerPath = Join-Path $repoRoot 'CmdPal_CI_Public.cer'

$plain = Read-Host 'PFX password (press Enter for none)'
$pwd = if ([string]::IsNullOrEmpty($plain)) {
    [Security.SecureString]::new()
} else {
    ConvertTo-SecureString -String $plain -AsPlainText -Force
}

$cert = New-SelfSignedCertificate `
    -Subject $subject `
    -Type Custom `
    -KeySpec Signature `
    -KeyUsage DigitalSignature `
    -KeyAlgorithm RSA `
    -KeyLength 2048 `
    -HashAlgorithm SHA256 `
    -NotAfter (Get-Date).AddYears(5) `
    -CertStoreLocation 'Cert:\CurrentUser\My' `
    -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3')

try {
    Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $pwd | Out-Null
    Export-Certificate -Cert $cert -FilePath $cerPath -Type CERT | Out-Null
}
finally {
    Remove-Item -Path "Cert:\CurrentUser\My\$($cert.Thumbprint)" -Force
}

Write-Host ""
Write-Host "Wrote: $pfxPath  -> GitHub secret CMDPAL_PFX_BASE64"
Write-Host "Wrote: $cerPath  -> trust on PCs (Trusted People)"
Write-Host ""
Write-Host "Base64 PFX (copy for secret):"
[Convert]::ToBase64String([IO.File]::ReadAllBytes($pfxPath))
