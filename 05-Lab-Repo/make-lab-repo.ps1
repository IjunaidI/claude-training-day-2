# Builds the Day 2 training repository from AssetDesk-Lab\.
#   main             = AssetDesk-Lab\, one commit, tagged start
#   reference-build  = start + the finished app as AssetDesk\ (the fallback)
# Usage, from the package root:
#   powershell -ExecutionPolicy Bypass -File 05-Lab-Repo/make-lab-repo.ps1 [target-dir]
param([string]$Target = ".\assetdesk-lab-out")
$ErrorActionPreference = "Stop"

$src = Join-Path $PSScriptRoot "AssetDesk-Lab"
$ref = Join-Path $PSScriptRoot "..\01-Instructor\Lab-Solutions\reference-build\AssetDesk"
if (-not (Test-Path (Join-Path $src "SPEC.md"))) { throw "Missing $src\SPEC.md" }
if (-not (Test-Path (Join-Path $ref "AssetDesk.csproj"))) { throw "Missing reference build at $ref" }
if (Test-Path $Target) { throw "$Target already exists. Delete it or pass another path." }

# Copy a folder, dotfiles included, then drop build output, databases, MCP leftovers and editor litter.
function Copy-Clean([string]$From, [string]$To) {
    New-Item -ItemType Directory -Force -Path $To | Out-Null
    Get-ChildItem -LiteralPath $From -Force | Copy-Item -Destination $To -Recurse -Force
    Get-ChildItem -LiteralPath $To -Recurse -Force -Directory |
        Where-Object { $_.Name -in @("bin", "obj", ".playwright-mcp") } | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    Get-ChildItem -LiteralPath $To -Recurse -Force -File |
        Where-Object { $_.Name -like "*.db" -or $_.Name -like "*.db-shm" -or $_.Name -like "*.db-wal" -or
                       $_.Name -in @(".DS_Store", ".mcp.json") } | Remove-Item -Force
}

# git writes progress to stderr; stop only on a non-zero exit code.
function Invoke-Git { & git @args; if ($LASTEXITCODE -ne 0) { throw "git $args failed" } }

Copy-Clean $src $Target
Push-Location $Target
try {
    Invoke-Git init -q -b main
    Invoke-Git add -A
    Invoke-Git commit -q -m "Initial lab state"
    Invoke-Git tag start

    Invoke-Git switch -q -c reference-build
    Copy-Clean $ref (Join-Path (Get-Location) "AssetDesk")
    Invoke-Git add -A
    Invoke-Git commit -q -m "Reference build (fallback)"
    Invoke-Git switch -q main

    Write-Host "Created $(Get-Location)"
    Invoke-Git log --oneline --all --decorate
    Write-Host ""
    Write-Host "Push it to an empty GitHub repository:"
    Write-Host "  cd $(Get-Location)"
    Write-Host "  git remote add origin <url>"
    Write-Host "  git push -u origin main reference-build --tags"
} finally { Pop-Location }
