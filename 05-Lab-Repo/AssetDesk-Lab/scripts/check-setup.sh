#!/usr/bin/env bash
# Workshop setup check for macOS and Linux.
# Run from the repository root:  bash scripts/check-setup.sh
failed=0
step() { # name, command, fix
  if out=$(eval "$2" 2>&1); then printf '[ OK ]  %s  %s\n' "$1" "$(echo "$out" | tail -n1)";
  else printf '[FAIL]  %s\n        Fix: %s\n' "$1" "$3"; failed=$((failed+1)); fi
}
step "git installed"          "git --version" "Install git, then open a new terminal."
step ".NET 10 SDK installed"  "dotnet --version | grep -E '^10\.'" "Install the .NET 10 SDK from https://dotnet.microsoft.com/download/dotnet/10.0, then open a new terminal."
step "Claude Code installed"  "claude --version" "Run: curl -fsSL https://claude.ai/install.sh | bash   then open a new terminal."
step "Claude Code signed in"  "claude -p 'Reply with the single word ok' | grep -qi '^ *ok' && echo 'claude -p answered'" "Run 'claude', complete the browser sign-in (or type /login), then /exit. If it says your organization has disabled access, bring this output to the setup desk."
step "Node.js 18+ and npx"    "node -e 'process.exit(parseInt(process.versions.node) >= 18 ? 0 : 1)' && echo \"node \$(node --version), npx \$(npx --version)\"" "Install Node.js LTS (macOS: 'brew install node', or the installer from https://nodejs.org), then open a new terminal. Lab 6 needs it."
step "NuGet and npm reachable" "curl -fsS -m 20 -o /dev/null https://api.nuget.org/v3/index.json && npm ping --fetch-timeout=20000 --fetch-retries=0 >/dev/null 2>&1 && echo 'api.nuget.org and registry.npmjs.org answered'" "Check your network. A proxy must allow api.nuget.org (packages for the build) and registry.npmjs.org (the Lab 6 browser server). Try: curl -I https://api.nuget.org/v3/index.json   and   npm ping"
if [ "$failed" -eq 0 ]; then echo; echo "All checks passed. You are ready for Day 2."; else echo; echo "$failed check(s) failed. Fix them before the workshop."; fi
