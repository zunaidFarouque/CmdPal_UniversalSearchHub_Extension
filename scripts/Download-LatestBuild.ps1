# Downloads all artifacts from the latest successful GitHub Actions "Build" run into artifacts/run-<id>/.
#
# Authentication (pick one):
#   - GitHub CLI: install gh, run `gh auth login` (token used via `gh auth token`), OR
#   - PAT: set environment variable GH_TOKEN or GITHUB_TOKEN (needs repo + actions:read for private repos).
#
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

function Get-GitHubAuthToken {
    if (Get-Command gh -ErrorAction SilentlyContinue) {
        $t = & gh auth token 2>$null
        if ($LASTEXITCODE -eq 0 -and $t) {
            return $t.Trim()
        }
    }
    if ($env:GH_TOKEN) { return $env:GH_TOKEN.Trim() }
    if ($env:GITHUB_TOKEN) { return $env:GITHUB_TOKEN.Trim() }
    return $null
}

$token = Get-GitHubAuthToken
if (-not $token) {
    throw @'
No GitHub credentials found. Do one of the following:
  1) Install GitHub CLI from https://cli.github.com/ and run: gh auth login
  2) Set environment variable GH_TOKEN to a Personal Access Token (classic: repo + read:actions, or fine-grained: Actions read + Contents read).
'@
}

$fullRepo = "$Owner/$Repo"
Write-Host "Repository: $fullRepo (branch $Branch, workflow $Workflow)"

$headers = @{
    Authorization            = "Bearer $token"
    Accept                   = 'application/vnd.github+json'
    'X-GitHub-Api-Version'   = '2022-11-28'
    'User-Agent'             = 'CmdPalSearchHub-Download-LatestBuild'
}

$runsUri = "https://api.github.com/repos/$Owner/$Repo/actions/workflows/$Workflow/runs?branch=$Branch&status=success&per_page=1"
try {
    $runsResp = Invoke-RestMethod -Uri $runsUri -Headers $headers -Method Get
}
catch {
    throw "Failed to list workflow runs (${runsUri}): $($_.Exception.Message)"
}

$workflowRuns = @($runsResp.workflow_runs)
if ($workflowRuns.Count -eq 0) {
    throw "No successful run found for workflow '$Workflow' on branch '$Branch'."
}

$run = $workflowRuns[0]
$runId = [string] $run.id
$dest = Join-Path (Join-Path $repoRoot 'artifacts') "run-$runId"
New-Item -ItemType Directory -Path $dest -Force | Out-Null

Write-Host "Run ID:  $runId"
Write-Host "Run URL: $($run.html_url)"
Write-Host "Downloading artifacts to:`n  $dest"

$artUri = "https://api.github.com/repos/$Owner/$Repo/actions/runs/$runId/artifacts"
$artsResp = Invoke-RestMethod -Uri $artUri -Headers $headers -Method Get

$artifacts = @($artsResp.artifacts)
if ($artifacts.Count -eq 0) {
    throw "Run $runId has no artifacts (upload step may have failed or been skipped)."
}

foreach ($artifact in $artifacts) {
    $zipUrl = "https://api.github.com/repos/$Owner/$Repo/actions/artifacts/$($artifact.id)/zip"
    $zipPath = Join-Path $dest "$($artifact.name).zip"
    Write-Host "  Downloading: $($artifact.name) ($($artifact.size_in_bytes) bytes)"
    Invoke-WebRequest -Uri $zipUrl -Headers $headers -OutFile $zipPath -UseBasicParsing

    $extractDir = Join-Path $dest $artifact.name
    New-Item -ItemType Directory -Path $extractDir -Force | Out-Null
    Expand-Archive -Path $zipPath -DestinationPath $extractDir -Force
    Remove-Item -LiteralPath $zipPath -Force
}

Write-Host "`nContents:"
Get-ChildItem -Path $dest -Recurse -File | ForEach-Object { Write-Host "  $($_.FullName)" }
