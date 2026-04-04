# Downloads all artifacts from the latest successful GitHub Actions "Build" run into artifacts/run-<id>/.
# Requires: GitHub CLI (gh) — https://cli.github.com/ — and `gh auth login`, or GH_TOKEN / GITHUB_TOKEN.
# Usage (repo root): pwsh ./scripts/Download-LatestBuild.ps1

[CmdletBinding()]
param(
    [string] $Branch = 'main',
    [string] $Workflow = 'build.yml',
    [string] $Owner,
    [string] $Repo
)

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    throw 'GitHub CLI (gh) not found. Install from https://cli.github.com/ and run gh auth login.'
}

if (-not $Owner -or -not $Repo) {
    Push-Location $repoRoot
    try {
        $origin = git remote get-url origin 2>$null
    }
    finally {
        Pop-Location
    }
    if (-not $origin) {
        throw 'Could not read git remote origin. Use -Owner and -Repo.'
    }
    if ($origin -match 'github\.com[:/]([^/]+)/([^/.]+)(?:\.git)?') {
        $Owner = $Matches[1]
        $Repo = $Matches[2]
    }
    else {
        throw "Could not parse owner/repo from origin: $origin"
    }
}

$fullRepo = "$Owner/$Repo"
Write-Host "Repository: $fullRepo (branch $Branch, workflow $Workflow)"

$raw = gh run list --workflow $Workflow --branch $Branch --status success -L 1 --repo $fullRepo --json databaseId,url,displayTitle 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "gh run list failed: $raw"
}

$runs = @($raw | ConvertFrom-Json)
if ($runs.Count -eq 0) {
    throw "No successful run found for workflow '$Workflow' on branch '$Branch'."
}

$run = $runs[0]
$runId = [string] $run.databaseId
$dest = Join-Path (Join-Path $repoRoot 'artifacts') "run-$runId"
New-Item -ItemType Directory -Path $dest -Force | Out-Null

Write-Host "Downloading run $runId to:`n  $dest"
Write-Host "Run URL: $($run.url)"

gh run download $runId --dir $dest --repo $fullRepo
if ($LASTEXITCODE -ne 0) {
    throw "gh run download failed (exit $LASTEXITCODE)."
}

Write-Host "`nContents:"
Get-ChildItem -Path $dest -Recurse -File | ForEach-Object { Write-Host "  $($_.FullName)" }
