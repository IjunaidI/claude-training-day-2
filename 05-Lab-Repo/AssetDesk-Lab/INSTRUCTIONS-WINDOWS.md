# INSTRUCTIONS — AssetDesk on Windows

Every command here runs **from the repository root** in **PowerShell**. The `dotnet` commands take
`--project AssetDesk`, so copy-paste works from one directory throughout.
On macOS, use [INSTRUCTIONS-MACOS.md](INSTRUCTIONS-MACOS.md). The labs themselves are in
[lab/LAB-GUIDE.md](lab/LAB-GUIDE.md).

Clone somewhere short, local and unsynced, such as `C:\src`. Not Desktop, Documents, or anything
OneDrive syncs (§7).

---

## Quick start

```powershell
powershell -ExecutionPolicy Bypass -File scripts/check-setup.ps1   # six [ OK ] lines
dotnet build AssetDesk                                             # expect 0 Warning(s), 0 Error(s)
dotnet run --project AssetDesk --urls http://localhost:5198        # Ctrl+C to stop
start http://localhost:5198
```

`AssetDesk\` does not exist until the Lab 1 build creates it.

---

## 1. Install

| Need | Install (PowerShell, no admin required) | Verify |
|---|---|---|
| Claude Code | `irm https://claude.ai/install.ps1 \| iex` | `claude --version` |
| Git | `winget install --id Git.Git -e` | `git --version` |
| .NET SDK 10 | `winget install --id Microsoft.DotNet.SDK.10 -e`, or [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download/dotnet/10.0) | `dotnet --version` → `10.x` |
| Node.js LTS (18 or later) | `winget install --id OpenJS.NodeJS.LTS -e`, or the installer from [nodejs.org](https://nodejs.org) | `node --version`, `npx --version` |
| curl | built into Windows 10 1803+ | `curl.exe --version` |

**Open a new terminal after installing.** The current session does not see the new PATH.

Node.js is only for Lab 6: `npx` downloads and starts the Playwright browser server. The app itself
has no `npm install`. No jq either: PowerShell parses JSON itself. NuGet packages restore on the
first build, so that build needs network. Lab 6 drives Google Chrome by default; without Chrome, use
Edge (§7).

Sign in once: run `claude`, complete the browser flow, then `/exit`. This needs a **Pro, Max, Team,
or Enterprise** plan. Leave `ANTHROPIC_API_KEY` unset, or you are billed through the API instead of
the subscription.

Git for Windows also gives Claude Code its Bash tool. If Claude Code reports that it cannot find Bash,
add this to `.claude/settings.local.json`:

```json
{ "env": { "CLAUDE_CODE_GIT_BASH_PATH": "C:\\Program Files\\Git\\bin\\bash.exe" } }
```

Anything else off: `claude doctor`.

---

## 2. Build it from the spec

Lab 1 in [lab/LAB-GUIDE.md](lab/LAB-GUIDE.md) walks you through it: a branch, plan mode, then the
build. Keep a second Windows Terminal tab open (`Ctrl+Shift+T`) for `dotnet`, `Invoke-RestMethod`
and `git` while the first runs `claude`.

---

## 3. Run it

```powershell
dotnet build AssetDesk
dotnet run --project AssetDesk --urls http://localhost:5198
```

Then <http://localhost:5198>. **Always pass `--urls`**: the scaffold writes a random port into
`launchSettings.json`, and every check below and the Lab 6 browser check expect 5198. It also keeps you
off HTTPS and its dev certificate.

While iterating: `dotnet watch --project AssetDesk --urls http://localhost:5198`. Restart it by hand
after adding a component, changing a `[Parameter]`, or editing `Program.cs`.

---

## 4. Verify it

```powershell
$base = 'http://localhost:5198'
(Invoke-RestMethod "$base/api/health").ok
# → True
$state = Invoke-RestMethod "$base/api/state"
"$($state.employees.Count) employees, $($state.assets.Count) assets"
# → 5 employees, 12 assets
```

13 or more assets means a database from an earlier run survived. Reset it (§5).

**The invariant check** (SPEC.md §9): a fabricated employee id is rejected. `Invoke-RestMethod`
throws on a 400, so the answer is in the `catch`:

```powershell
$fresh = ($state.assets | Where-Object status -eq 'InStock' | Select-Object -First 1).id
$bad = @{ employeeId = 'not-a-real-id'; assignedDate = '2026-01-15' } | ConvertTo-Json
try   { Invoke-RestMethod -Method Post -Uri "$base/api/assets/$fresh/assign" -ContentType 'application/json' -Body $bad }
catch { $_.ErrorDetails.Message }
# → {"error":"That asset no longer exists. Refresh to see current data."}
```

**The status guard**: AST-1004 is seeded in repair (SPEC.md §7), so assigning it is refused.

```powershell
$repair = ($state.assets | Where-Object tag -eq 'AST-1004').id
try   { Invoke-RestMethod -Method Post -Uri "$base/api/assets/$repair/assign" -ContentType 'application/json' -Body $bad }
catch { $_.ErrorDetails.Message }
# → {"error":"AST-1004 is in repair and cannot be assigned."}
```

In Windows PowerShell 5.1, `curl` is an alias for `Invoke-WebRequest`. Type `curl.exe` when you
mean curl.

---

## 5. Reset the data

Stop the app, delete the database **and** its `-wal` and `-shm` files, restart. The seed rebuilds.

```powershell
# Ctrl+C, then:
Get-ChildItem AssetDesk -Recurse -Filter 'assetdesk.db*' | Remove-Item -Force
dotnet run --project AssetDesk --urls http://localhost:5198
```

- **All three files.** Deleting only `assetdesk.db` leaves a confusing half-state, because WAL mode
  keeps live pages in the sidecar files. The filter catches all three.
- **Which folder?** SPEC.md puts source in `Data\` and the database in `data\`. NTFS is
  case-insensitive, so they are the same folder. That is why the command searches instead of
  naming a path.

---

## 6. Falling back to the reference build

Behind at the DoD check? Commit your work, then start a branch from the finished app:

```powershell
git add -A; git commit -m "wip: my build"
git switch -c build/<name>-ref origin/reference-build
Get-ChildItem AssetDesk -Recurse -Filter 'assetdesk.db*' | Remove-Item -Force   # the old database survives a branch switch
dotnet run --project AssetDesk --urls http://localhost:5198
```

Your own build stays on the branch you left. Never commit to `reference-build` itself: the whole
room shares it.

---

## 7. When it goes wrong

| Symptom | Fix |
|---|---|
| `address already in use` on 5198 | `Get-NetTCPConnection -LocalPort 5198 -State Listen`, then `Stop-Process -Id <OwningProcess> -Force`. |
| `/api/state` shows 13+ assets | An old database survived. §5. |
| `MSB1003: Specify a project or solution file` | You ran `dotnet build` at the root. Name the project: `dotnet build AssetDesk`. |
| `MSB3021` / `MSB3027: Unable to copy file`, *the process cannot access the file* | Something holds `bin\`: the running app, `dotnet watch`, or OneDrive. Stop it and rebuild. Move the clone out of OneDrive. |
| `dotnet` or `claude` not recognized after installing | Open a new terminal. |
| Buttons and tabs do nothing, build is clean | No interactive render mode. SPEC.md §3.3 item 1. |
| Search only filters when you click away | `@bind:event="oninput"` is missing. SPEC.md §3.3 item 5. |
| `NU1903` NuGet audit warning breaks zero-warnings | Known transitive advisory in `SQLitePCLRaw`. Suppress it with `<NoWarn>$(NoWarn);NU1903</NoWarn>` and a comment saying why. |
| NuGet restore fails | No network, or the proxy blocks api.nuget.org. `dotnet nuget locals all --clear`, retry. |
| `warning: LF will be replaced by CRLF` | Cosmetic. Nothing breaks. |
| A new skill or agent is missing from `/` | Skills register at startup. `/exit`, then `claude`. `/clear` is not enough. |
| Claude says it cannot edit `SPEC.md` | Working as intended: `.claude/settings.json` denies it. |
| `/mcp` does not list `microsoft-learn` or `playwright` | Servers load when a session starts. `/exit`, then `claude`. Run `claude mcp list` from the repository root: a server added with the default (local) scope belongs to the folder you added it in. |
| `npx` is not recognized | Node.js is missing or not on PATH. Install it (§1), open a new terminal, then `/exit` and `claude`. |
| `/mcp` shows `playwright` failed, or `Connection closed` | On Windows, `npx` must start through `cmd /c`. `claude mcp remove playwright`, then `claude mcp add playwright -- cmd /c npx @playwright/mcp@latest`. Still failing? Start it by hand to see why: `cmd /c npx @playwright/mcp@latest --help`. |
| npx hangs, or fails with `ETIMEDOUT`, `ECONNREFUSED`, `E403` or `SELF_SIGNED_CERT_IN_CHAIN` | The proxy blocks registry.npmjs.org. Ask IT for the proxy address and run `npm config set proxy http://<host>:<port>` and `npm config set https-proxy http://<host>:<port>`. Meanwhile do Lab 6 Part A: Microsoft Learn needs no npm. |
| Playwright says `Chromium distribution 'chrome' is not found` | Chrome is not installed. Use Edge, which every Windows machine has: `claude mcp remove playwright`, then `claude mcp add playwright -- cmd /c npx @playwright/mcp@latest --browser msedge`. |
| The browser opens but shows `This site can't be reached` | The app is not running on 5198. Start it (§3) in another tab. |
| `/mcp` shows `microsoft-learn` failed | Your network blocks learn.microsoft.com. Open https://learn.microsoft.com in a browser to confirm, then ask IT. |
| Anything else in Claude Code | `/doctor`. |
