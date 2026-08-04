# INSTRUCTIONS — AssetDesk on Windows

Every command here runs **from the repo root** in **PowerShell**. Nothing needs `cd AssetDesk`; the
`dotnet` commands take `--project AssetDesk` instead, so copy-paste works from one directory
throughout.

Written for Windows 10 1809+ / Windows 11, PowerShell 5.1 or 7, .NET SDK 10. The app and the spec
were rehearsed on macOS; the PowerShell below is the translated equivalent of that verified run. If a
command surprises you, the Git Bash note at the end of §1 puts you back on the exact set that was
tested. On macOS, use [INSTRUCTIONS-MACOS.md](INSTRUCTIONS-MACOS.md).

---

## Quick start

```powershell
git clone https://github.com/IjunaidI/spec-driven-demo.git C:\src\spec-driven-demo
cd C:\src\spec-driven-demo
claude                                                        # then: /model sonnet
                                                              # then: Read SPEC.md and build it.
dotnet build AssetDesk                                        # expect 0 Warning(s), 0 Error(s)
dotnet run --project AssetDesk --urls http://localhost:5198    # Ctrl+C to stop
start http://localhost:5198
```

Two more you will want:

```powershell
Get-ChildItem AssetDesk -Recurse -Filter 'assetdesk.db*' | Remove-Item -Force   # reset the demo data
git add -A; git commit -m wip                                                   # save your build first
git checkout checkpoint-3                                                       # a finished app to fall back on
```

Clone somewhere short and local — `C:\src`, not Desktop, Documents, or anything OneDrive syncs. See
§7.

Fresh machine? Do §1 first. Everything after §3 is optional reading.

**What you type vs. what the agent types.** This file is your half: installs, clone, boot, inspect,
reset. Everything *inside* the build is the agent's — `dotnet new`, `dotnet add package`, every file
it writes, the `.gitignore`. It reads the platform from its own environment and issues the matching
commands, so nothing in SPEC.md is OS-specific and you never translate for it. With Git for Windows
installed it shells out through Git Bash; without it, through PowerShell. Either way it works out its
own paths.

---

## 1. Install

| Need | Install (PowerShell, no admin required) | Verify |
|---|---|---|
| Claude Code | `irm https://claude.ai/install.ps1 \| iex` | `claude --version` |
| Git | `winget install --id Git.Git -e` | `git --version` |
| .NET SDK 10 | `winget install --id Microsoft.DotNet.SDK.10 -e`, or [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download) | `dotnet --version` → `10.x` |
| sqlite3 CLI *(optional)* | `winget install --id SQLite.SQLite -e` | `sqlite3 --version` |
| curl | built into Windows 10 1803+ | `curl.exe --version` |

**Open a new terminal after those installs** — winget and the .NET installer write to the machine
PATH, and your current session will not see it.

That is the whole list. **No Node, and no `npm install` for the app** — it is .NET, and Claude Code's
installer ships a self-contained binary. **No jq either**: PowerShell parses JSON natively, which is
what §4 uses. The two NuGet packages (`Dapper`, `Microsoft.Data.Sqlite`) restore on the first build,
so that build needs network. Nothing else.

`winget install Anthropic.ClaudeCode` also works if you would rather have it in your package manifest;
it does not auto-update, so `winget upgrade Anthropic.ClaudeCode` is then on you. The `install.ps1`
route updates itself in the background. Only the `sqlite3` row is optional, and only because it is
just for reading the database directly in §4.

Then sign in once — run `claude` and complete the browser flow. This needs a **Pro, Max, Team, or
Enterprise** plan; the free tier does not include Claude Code. Leave `ANTHROPIC_API_KEY` unset while
you do it, or you will be billed through the API account instead of the subscription.

Anything off, run `claude doctor` — it checks the install and settings without starting a session.

**Git for Windows is what gives Claude Code its Bash tool.** You need Git to clone anyway, so this
comes free; without it Claude Code runs shell commands through PowerShell instead. If it reports that
it cannot find Bash, point it at Git's copy in `.claude/settings.json`:

```json
{ "env": { "CLAUDE_CODE_GIT_BASH_PATH": "C:\\Program Files\\Git\\bin\\bash.exe" } }
```

**Prefer bash?** Every command in [INSTRUCTIONS-MACOS.md](INSTRUCTIONS-MACOS.md) from §2 onward runs
unchanged in Git Bash, which is the set that was actually verified end to end. You would need jq
(`winget install --id jqlang.jq -e`), and `open` becomes `start`. Pick one shell and stay in it —
mixing the two mid-session is how you end up in the wrong directory.

---

## 2. Build it from the spec

```powershell
git checkout main
Remove-Item AssetDesk -Recurse -Force -ErrorAction SilentlyContinue   # see §6 — start genuinely clean
claude
```

Pin the model before prompting. This is what makes the build land in minutes rather than an hour, and
it is the configuration the whole spec was rehearsed against:

```
/model sonnet
```

Then the prompt. One line, nothing more:

```
Read SPEC.md and build it.
```

SPEC.md §8 tells it to work in milestones M0→M7 and §10 tells it not to stop and ask, so expect a
one-line note per milestone and no questions. Keep a second Windows Terminal tab open
(`Ctrl+Shift+T`) for `dotnet` and `Invoke-RestMethod` while the first runs `claude`.

---

## 3. Run it

```powershell
dotnet build AssetDesk                                        # also restores the 2 NuGet packages
dotnet run --project AssetDesk --urls http://localhost:5198
```

Then <http://localhost:5198>. `Ctrl+C` stops it.

**Pass `--urls`.** The scaffold writes a *random* port into
`AssetDesk\Properties\launchSettings.json`, so a freshly generated project will not be on 5198 and
every check below would miss it. `--urls` overrides it, and pinning it to `http://localhost` also
keeps you out of the HTTPS dev-certificate detour. Without it, read the port off the
`Now listening on: http://localhost:XXXX` line.

While iterating:

```powershell
dotnet watch --project AssetDesk --urls http://localhost:5198
```

Hot reload covers markup and method bodies. **Restart it by hand** after adding a component, changing
a `[Parameter]`, or editing `Program.cs`. SPEC.md §8 says so too, and it is still the most common
"why isn't my change showing".

---

## 4. Verify it

These are the M0/M1 checks, and they prove the data layer before any UI exists — the reason SPEC.md
orders the build that way.

```powershell
$base = 'http://localhost:5198'

(Invoke-RestMethod "$base/api/health").ok
# → True

$state = Invoke-RestMethod "$base/api/state"
"$($state.employees.Count) employees, $($state.assets.Count) assets"
# → 5 employees, 12 assets
```

> **13 assets means a database from an earlier run survived.** The seed only fires on an empty
> database. Reset it (§5).

`Invoke-RestMethod` gives you deserialized objects, so `$state.assets | Format-Table tag, status,
assignedTo` is a readable dump with no jq in sight. Grab two ids:

```powershell
$emp   = $state.employees[0].id
$asset = ($state.assets | Where-Object status -eq 'InStock' | Select-Object -First 1).id
```

```powershell
# assign → return → repair
$body = @{ employeeId = $emp; assignedDate = '2026-07-31' } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "$base/api/assets/$asset/assign" -ContentType 'application/json' -Body $body
Invoke-RestMethod -Method Post -Uri "$base/api/assets/$asset/return"
Invoke-RestMethod -Method Post -Uri "$base/api/assets/$asset/status" -ContentType 'application/json' -Body '{"status":"Repair"}'

# three assets in one transaction → sequential tags AST-1013/14/15
$new = @{
    tag      = 'AST-1013'; category = 'Headset'; make  = 'Jabra'; model = 'Evolve2 65'
    serial   = 'JB2065X3'; condition = 'New';    cost  = 210;     notes = ''
    location = 'HQ / Store Room';  purchaseDate = '2026-07-31';   quantity = 3
} | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri "$base/api/assets" -ContentType 'application/json' -Body $new
```

**The invariant check.** A fabricated employee id is rejected, which proves `Foreign Keys=True`
reached the connection string rather than a `PRAGMA` that only applied to one pooled connection
(SPEC.md §3.3 item 2, §4.4 invariant 5). `Invoke-RestMethod` *throws* on a 400, so the `catch` is
where the interesting part lives:

```powershell
# re-select a still-in-stock asset — the block above left $asset in Repair
$fresh = ((Invoke-RestMethod "$base/api/state").assets |
          Where-Object status -eq 'InStock' | Select-Object -First 1).id
$bad = @{ employeeId = 'not-a-real-id'; assignedDate = '2026-07-31' } | ConvertTo-Json
try   { Invoke-RestMethod -Method Post -Uri "$base/api/assets/$fresh/assign" -ContentType 'application/json' -Body $bad }
catch { $_.ErrorDetails.Message; $_.Exception.Response.StatusCode }
# → {"error":"That asset no longer exists. Refresh to see current data."}
#   BadRequest
```

If `ErrorDetails.Message` comes back empty on Windows PowerShell 5.1, the `BadRequest` on its own is
still the proof — the write was refused. Reuse `$asset` instead of `$fresh` and you get
`AST-1003 is in repair and cannot be assigned.` — the status guard (invariant 6) fires before the
foreign key does. Both are `AssetDeskException` messages from §4.6, so either proves the point; just
know which one you are looking at.

**The data is a file.** WAL mode lets you read it while the app holds it open:

```powershell
$db = (Get-ChildItem AssetDesk -Recurse -Filter assetdesk.db | Select-Object -First 1).FullName
sqlite3 $db "select tag,status,assigned_to from assets order by tag;"
```

Then walk SPEC.md §5's acceptance criteria in the browser and §9's definition of done. `dotnet build
AssetDesk` reporting `0 Warning(s), 0 Error(s)` is one of them, nullable warnings included.

---

## 5. Reset the data

Stop the app, delete the database, restart — the seed rebuilds itself.

```powershell
# Ctrl+C, then:
Get-ChildItem AssetDesk -Recurse -Filter 'assetdesk.db*' | Remove-Item -Force
dotnet run --project AssetDesk --urls http://localhost:5198
```

Two things behind that one line:

- **All three files, not just one.** WAL leaves `assetdesk.db-wal` and `assetdesk.db-shm` beside the
  database; deleting only the base file leaves a confusing half-state. The `-Filter 'assetdesk.db*'`
  catches all three, and matching nothing is a silent no-op rather than an error.
- **Which folder is it in?** SPEC.md puts source in `Data\` and the database in `data\`. NTFS is
  case-insensitive, so those are the same physical directory and the `.db` lands beside the `.cs`
  files. That is why the commands search for the path instead of hardcoding it.

Back to a clean repo:

```powershell
git checkout main; Remove-Item AssetDesk -Recurse -Force -ErrorAction SilentlyContinue
```

---

## 6. Branches, and the one that saves you

| Branch | What it is | When |
|---|---|---|
| `main`, `checkpoint-1` | Checkpoint 1. Spec, runbook, instructions, and the reviewer | **Start here.** |
| `checkpoint-2` | + the `api-boundary` skill | Checkpoint 2 |
| `checkpoint-3` | + two finished apps to review. No build | Checkpoint 3 |
| `checkpoint-4` | + the two quality skills | Checkpoint 4 |
| `checkpoint-4-result` | Checkpoint 4's finished app | Only after your own checkpoint-4 build is done or abandoned |
| `dotnet-demo` | The original single-exercise build, against the unthinned spec | Historical reference |
| `nodejs-demo` | Archive of the pre-.NET attempt | Historical curiosity. **Not a working app.** |

The switching ritual — commit, switch, delete the database, restart Claude Code — is in
[CHECKPOINTS.md](CHECKPOINTS.md#switching-checkpoints). Follow it rather than the older
`Remove-Item -Recurse -Force AssetDesk` advice: committing your build first makes the delete
unnecessary and keeps your work.

### Starting clean

```powershell
git checkout main
Remove-Item AssetDesk -Recurse -Force -ErrorAction SilentlyContinue   # bin\, obj\ and assetdesk.db*
                                                                      # are gitignored and SURVIVE a switch
git status      # clean
Get-ChildItem   # INSTRUCTIONS-MACOS.md  INSTRUCTIONS-WINDOWS.md  README.md  SPEC.md
```

That `Remove-Item` is not optional. Because the build output and database are gitignored,
`git checkout main` cannot remove them, and the next build scaffolds into a directory that already
holds a **populated** database — the seed then skips, and your very first verification prints the
wrong count.

### Falling back mid-build

```powershell
Remove-Item AssetDesk -Recurse -Force      # REQUIRED FIRST
git checkout checkpoint-3
dotnet run --project AssetDesk --urls http://localhost:5198
```

The `Remove-Item` is what makes this work. If your build reached the scaffold, those files are
*untracked*, and git refuses to clobber untracked files:

```
error: The following untracked working tree files would be overwritten by checkout:
	AssetDesk/AssetDesk.csproj
	AssetDesk/Components/App.razor
	... 9 more
Aborting
```

Neither `git switch` nor `git stash` helps — the files are untracked, not modified. Delete then check
out. If `Remove-Item` fails with *the process cannot access the file*, something still has a handle on
`bin\` — stop the app and any `dotnet watch`, and close the folder in Explorer.

---

## 7. Windows-specific traps

**1. `curl` is not curl in Windows PowerShell 5.1.** It is an alias for `Invoke-WebRequest`, so
`curl -s http://...` fails with *A parameter cannot be found that matches parameter name 's'*. Write
`curl.exe` when you mean the real thing, or use the `Invoke-RestMethod` commands in §4. PowerShell 7
dropped the alias, which is exactly why the same line behaves differently on two machines in the same
room. Passing raw JSON to `curl.exe` from PowerShell also mangles the embedded double quotes —
another reason §4 does not use it.

**2. Clone somewhere short, local, and unsynced.** `C:\src\spec-driven-demo`. Under OneDrive —
which now covers Desktop and Documents by default — the sync client grabs handles on `bin\` and
`obj\` mid-build and you get intermittent `MSB3021: Unable to copy file` and *the process cannot
access the file*. Deep paths also run at the 260-character limit once `obj\Debug\net10.0\` nests
under them. Defender's real-time scanning slows every build; excluding your `src` folder is worth it
if you build all day.

**3. NTFS is case-insensitive, so SPEC.md §3.2 ignores the database by name on purpose. Do not "fix"
it to `data/`.** `Data\` (source) and `data\` (database) resolve to the same physical directory, so a
`data/` rule also matches `Data\Models.cs`, `Db.cs`, `Format.cs` and `AssetRepository.cs`.
`git check-ignore -v AssetDesk\Data\Models.cs` reports the match, and `git add -A` then stages
nothing but the `.gitignore` itself. Your entire data layer drops out of `git status` while the build
keeps succeeding — so the commit at the end of the session ships an app with no SQL in it. Ignoring
the three `assetdesk.db*` files by name is equivalent and safe everywhere. Same trap on macOS, and a
spec that would look perfectly fine on Linux.

**4. `--empty` does not give you an empty project on SDK 10.** The scaffold command in SPEC.md §3.1
is accepted and still emits six files that §2 explicitly bans — component-scoped CSS and JS interop:

```
Components\Layout\MainLayout.razor.css        Components\Pages\Error.razor
Components\Layout\ReconnectModal.razor        Components\Pages\NotFound.razor
Components\Layout\ReconnectModal.razor.css    Components\Layout\ReconnectModal.razor.js
```

§3.1 only orders that cleanup "if your SDK rejects `--empty`" — which it does not. Check before you
sign off on §9:

```powershell
Get-ChildItem AssetDesk -Recurse -Include '*.razor.css', '*.razor.js'    # must print nothing
```

---

## 8. When it goes wrong

| Symptom | Fix |
|---|---|
| `git checkout checkpoint-3` → `untracked working tree files would be overwritten` | Your build already scaffolded `AssetDesk\`. `Remove-Item AssetDesk -Recurse -Force`, then check out (§6). `git stash`/`git switch` will not help — untracked, not modified. |
| `Failed to bind to address http://127.0.0.1:5198: address already in use` | An older run is still listening: `Get-NetTCPConnection -LocalPort 5198 -State Listen -ErrorAction SilentlyContinue`, then `Stop-Process -Id <OwningProcess> -Force`. Or move: `--urls http://localhost:5299`. Stale `dotnet watch` processes outlive a closed tab — `Get-Process dotnet` lists them. |
| `/api/health` answers but `/api/state` 500s with `SQLite Error 14: 'unable to open database file'` | You are talking to a **stale process from a directory that no longer exists**, not your app. Find it with `Get-Process dotnet \| Select-Object Id, Path`, and kill it. |
| `/api/state` shows 13 assets, not 12 | An old database survived. `Get-ChildItem AssetDesk -Recurse -Filter 'assetdesk.db*' \| Remove-Item -Force` and restart. |
| `curl : A parameter cannot be found that matches parameter name 's'` | `curl` is an alias for `Invoke-WebRequest` in PowerShell 5.1. Use `curl.exe`, or §4's `Invoke-RestMethod`. |
| `Remove-Item` → `The process cannot access the file` | The app, `dotnet watch`, an open Explorer window, or OneDrive has a handle on `bin\`. Stop them and retry; move the clone out of OneDrive (§7). |
| `MSBUILD : error MSB1003: Specify a project or solution file` | You ran `dotnet build` with no project. Everything here is root-relative: `dotnet build AssetDesk`, `dotnet run --project AssetDesk`. |
| `dotnet` or `claude` not recognized after installing | New terminal. winget and the .NET installer update the machine PATH; the session you ran them in never re-reads it. |
| Browser warns about the certificate | You dropped `--urls http://localhost:5198` and launchSettings picked the HTTPS profile. Put `--urls` back, or run `dotnet dev-certs https --trust` once. |
| Windows Defender Firewall prompts on first run | Cancel is fine. `--urls http://localhost:5198` binds loopback only, and loopback needs no exception. |
| Buttons and tabs do nothing, but the build is clean | No interactive render mode. `Components\App.razor` needs `@rendermode="InteractiveServer"` on both `<HeadOutlet>` and `<Routes>` (SPEC.md §3.3 item 1). Invisible in build output — the #1 Blazor trap. |
| Search only filters when you click away | `@bind:event="oninput"` is missing on the search input (SPEC.md §3.3 item 5). |
| `NU1903` NuGet audit warning breaks the zero-warning goal | Known: `SQLitePCLRaw.lib.e_sqlite3` is pinned transitively by `Microsoft.Data.Sqlite` 10.0.10 and has no newer patch. `dotnet-demo` suppresses it with `<NoWarn>$(NoWarn);NU1903</NoWarn>` and a comment saying why. |
| A `.razor` edit is not showing up | Restart `dotnet watch`. Hot reload does not pick up new components or changed parameters. |
| NuGet restore fails | No network, or a corporate proxy. `dotnet nuget locals all --clear` and retry; hotspot as a last resort. |
| `warning: LF will be replaced by CRLF` on every `git add` | Cosmetic. Git is normalizing line endings on a repo authored on macOS; nothing breaks, and the build does not care. |
| A new slash command or skill is missing from `/` | Restart the session: `/exit`, then `claude`. `/clear` is **not** enough — commands and skills register at startup only. |
| `CLAUDE.md` looks ignored | `/context`. If it is not listed, `claude` was launched outside the repo or the filename is wrong (`CLAUDE.md`, exactly). |
| Anything else odd in Claude Code | `/doctor` — it diagnoses install and config and can apply fixes itself. |
| `/review-build` is missing from the `/` list after switching checkpoints | Skills register at startup only. `/exit`, then `claude`. `/clear` will not do it. |
| `git switch checkpoint-N` refuses over your build | You did not commit it. `git add -A; git commit -m wip`, then switch. Your build stays on the branch you left. |
| `MSBUILD : error MSB1011: more than one project` | `checkpoint-3` holds two apps. Name the one you mean: `dotnet build AssetDesk` or `dotnet build reference\AssetDesk-noskills`. |

---

## 9. Cheat sheet

All from the repo root.

```powershell
# branches
git checkout main; Remove-Item AssetDesk -Recurse -Force -ErrorAction SilentlyContinue
git add -A; git commit -m wip
git checkout checkpoint-3

# build & run
dotnet build AssetDesk
dotnet run   --project AssetDesk --urls http://localhost:5198
dotnet watch --project AssetDesk --urls http://localhost:5198

# what the agent runs for you (SPEC.md §3.1)
dotnet new blazor -o AssetDesk -f net10.0 --interactivity Server --all-interactive --empty
dotnet add AssetDesk package Dapper
dotnet add AssetDesk package Microsoft.Data.Sqlite

# inspect
$base  = 'http://localhost:5198'
(Invoke-RestMethod "$base/api/health").ok
$state = Invoke-RestMethod "$base/api/state"; "$($state.employees.Count) / $($state.assets.Count)"
$state.assets | Format-Table tag, status, assignedTo
$db = (Get-ChildItem AssetDesk -Recurse -Filter assetdesk.db | Select-Object -First 1).FullName
sqlite3 $db "select count(*) from assets;"

# reset data
Get-ChildItem AssetDesk -Recurse -Filter 'assetdesk.db*' | Remove-Item -Force

# spec spot-checks
Get-ChildItem AssetDesk -Recurse -Include '*.razor.css', '*.razor.js'    # must print nothing

# who has my port
Get-NetTCPConnection -LocalPort 5198 -State Listen -ErrorAction SilentlyContinue
Get-Process dotnet | Select-Object Id, Path

# environment
dotnet --version; dotnet --list-sdks
claude --version; claude doctor
```
