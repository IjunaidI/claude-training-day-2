# Workshop setup check for Windows.
# Run from the repository root:
#   powershell -ExecutionPolicy Bypass -File scripts/check-setup.ps1
$failed = 0
function Test-Step([string]$Name, [scriptblock]$Check, [string]$Fix) {
    try {
        $global:LASTEXITCODE = 0   # a failed earlier step must not fail this one
        $result = & $Check
        if ($LASTEXITCODE -ne $null -and $LASTEXITCODE -ne 0) { throw "exit code $LASTEXITCODE" }
        Write-Host "[ OK ]  $Name  $result" -ForegroundColor Green
    } catch {
        Write-Host "[FAIL]  $Name" -ForegroundColor Red
        Write-Host "        Fix: $Fix" -ForegroundColor Yellow
        $script:failed++
    }
}
Test-Step "git installed"         { (git --version) }                        "Install Git for Windows from https://git-scm.com/downloads/win, then open a new terminal."
Test-Step ".NET 10 SDK installed" { $v = dotnet --version; if (-not $v.StartsWith("10.")) { throw "found $v" }; $v } "Install the .NET 10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0, then open a new terminal."
Test-Step "Claude Code installed" { (claude --version) }                     "Run: irm https://claude.ai/install.ps1 | iex   then open a new terminal."
Test-Step "Claude Code signed in" { $out = (claude -p "Reply with the single word ok") -join "`n"; if ($LASTEXITCODE -ne 0 -or $out.Trim() -notmatch '^ok') { throw "no answer" }; "claude -p answered" } "Run 'claude', complete the browser sign-in (or type /login), then /exit. If it says your organization has disabled access, bring this output to the setup desk."
# Lab 6 starts the browser server with 'cmd /c npx', so check npx exactly that way.
Test-Step "Node.js 18+ and npx"   { $v = (node --version); if ([int]($v.TrimStart('v').Split('.')[0]) -lt 18) { throw "found $v" }; $n = (cmd /c npx --version); "node $v, npx $n" } "Install Node.js LTS: winget install --id OpenJS.NodeJS.LTS -e   (or the installer from https://nodejs.org), then open a new terminal. Lab 6 needs it."
Test-Step "NuGet and npm reachable" {
    $r = Invoke-WebRequest -UseBasicParsing -Uri "https://api.nuget.org/v3/index.json" -TimeoutSec 20
    if ($r.StatusCode -ne 200) { throw "NuGet answered $($r.StatusCode)" }
    cmd /c "npm ping --fetch-timeout=20000 --fetch-retries=0 >nul 2>&1"
    "api.nuget.org and registry.npmjs.org answered"
} "Check your network. The proxy must allow api.nuget.org (packages for the build) and registry.npmjs.org (the Lab 6 browser server). Try: curl.exe -I https://api.nuget.org/v3/index.json   and   npm ping"
if ($failed -eq 0) { Write-Host "`nAll checks passed. You are ready for Day 2." -ForegroundColor Green }
else { Write-Host "`n$failed check(s) failed. Fix them before the workshop, or bring this output to the setup desk." -ForegroundColor Red }
