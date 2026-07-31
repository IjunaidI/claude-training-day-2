# INSTRUCTIONS — AssetDesk on macOS

Every command here runs **from the repo root**. Nothing needs `cd AssetDesk`; the `dotnet` commands
take `--project AssetDesk` instead, so copy-paste works from one directory throughout.

Verified end to end on macOS (Darwin 25.5, arm64), .NET SDK 10.0.302, sqlite3 3.51.0, jq 1.7.1.
On Windows, use [INSTRUCTIONS-WINDOWS.md](INSTRUCTIONS-WINDOWS.md).

---

## Quick start

```bash
git clone https://github.com/IjunaidI/spec-driven-demo.git
cd spec-driven-demo
claude                                                        # then: /model sonnet
                                                              # then: Read SPEC.md and build it.
dotnet build AssetDesk                                        # expect 0 Warning(s), 0 Error(s)
dotnet run --project AssetDesk --urls http://localhost:5198    # Ctrl+C to stop
open http://localhost:5198
```

Two more you will want:

```bash
find AssetDesk -name 'assetdesk.db*' -delete                  # reset the demo data
rm -rf AssetDesk && git checkout dotnet-demo                  # bail out to the finished app
```

Fresh machine? Do §1 first. Everything after §3 is optional reading.

**What you type vs. what the agent types.** This file is your half: installs, clone, boot, inspect,
reset. Everything *inside* the build is the agent's — `dotnet new`, `dotnet add package`, every file
it writes, the `.gitignore`. It reads the platform from its own environment and issues the matching
commands, so nothing in SPEC.md is OS-specific and you never translate for it.

---

## 1. Install

| Need | Install | Verify |
|---|---|---|
| Claude Code | `curl -fsSL https://claude.ai/install.sh \| bash` | `claude --version` |
| .NET SDK 10 | `brew install --cask dotnet-sdk` or [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download) | `dotnet --version` → `10.x` |
| git, curl, sqlite3, jq | preinstalled | `sqlite3 --version && jq --version` |

That is the whole list. **No Node, and no `npm install` for the app** — it is .NET, and Claude Code's
installer ships a self-contained binary. The two NuGet packages (`Dapper`,
`Microsoft.Data.Sqlite`) restore on the first build, so that build needs network. Nothing else.

Prefer Homebrew for Claude Code: `brew install --cask claude-code`. It does not auto-update, so
`brew upgrade claude-code` is on you; the `install.sh` route updates itself in the background.

Then sign in once — run `claude` and complete the browser flow. This needs a **Pro, Max, Team, or
Enterprise** plan; the free tier does not include Claude Code. Leave `ANTHROPIC_API_KEY` unset while
you do it, or you will be billed through the API account instead of the subscription.

**`dotnet: command not found`** — the installer drops `/etc/paths.d/dotnet` pointing at
`/usr/local/share/dotnet`, and `path_helper` only reads it for **new login shells**. Open a fresh
Terminal tab, or in this one:

```bash
export PATH="/usr/local/share/dotnet:$PATH"
```

**`claude: command not found`** — the native installer puts it at `~/.local/bin/claude`:

```bash
echo 'export PATH="$HOME/.local/bin:$PATH"' >> ~/.zshrc && source ~/.zshrc
```

Anything else off, run `claude doctor` — it checks the install and settings without starting a
session.

---

## 2. Build it from the spec

```bash
git checkout main && rm -rf AssetDesk       # see §6 — start from a genuinely clean tree
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
one-line note per milestone and no questions. Keep a second terminal tab open (`Cmd+T`) for `dotnet`
and `curl` while the first one runs `claude`.

---

## 3. Run it

```bash
dotnet build AssetDesk                                        # also restores the 2 NuGet packages
dotnet run --project AssetDesk --urls http://localhost:5198
```

Then <http://localhost:5198>. `Ctrl+C` stops it.

**Pass `--urls`.** The scaffold writes a *random* port into
`AssetDesk/Properties/launchSettings.json`, so a freshly generated project will not be on 5198 and
every curl below would miss it. `--urls` overrides it. Without it, read the port off the
`Now listening on: http://localhost:XXXX` line.

While iterating:

```bash
dotnet watch --project AssetDesk --urls http://localhost:5198
```

Hot reload covers markup and method bodies. **Restart it by hand** after adding a component, changing
a `[Parameter]`, or editing `Program.cs`. SPEC.md §8 says so too, and it is still the most common
"why isn't my change showing".

---

## 4. Verify it

These are the M0/M1 checks, and they prove the data layer before any UI exists — the reason SPEC.md
orders the build that way.

```bash
curl -s http://localhost:5198/api/health
# → {"ok":true}

curl -s http://localhost:5198/api/state | jq '{employees: (.employees|length), assets: (.assets|length)}'
# → { "employees": 5, "assets": 12 }
```

> **13 assets means a database from an earlier run survived.** The seed only fires on an empty
> database. Reset it (§5).

Pull ids into variables so the mutations read cleanly:

```bash
STATE=$(curl -s http://localhost:5198/api/state)
ASSET=$(echo "$STATE" | jq -r 'first(.assets[] | select(.status=="InStock") | .id)')
EMP=$(echo "$STATE"   | jq -r '.employees[0].id')
```

```bash
# assign → return → repair
curl -s -X POST http://localhost:5198/api/assets/$ASSET/assign \
  -H 'Content-Type: application/json' -d "{\"employeeId\":\"$EMP\",\"assignedDate\":\"2026-07-31\"}" | jq .
curl -s -X POST http://localhost:5198/api/assets/$ASSET/return | jq .
curl -s -X POST http://localhost:5198/api/assets/$ASSET/status \
  -H 'Content-Type: application/json' -d '{"status":"Repair"}' | jq .

# three assets in one transaction → sequential tags AST-1013/14/15
curl -s -X POST http://localhost:5198/api/assets -H 'Content-Type: application/json' -d '{
  "tag":"AST-1013","category":"Headset","make":"Jabra","model":"Evolve2 65","serial":"JB2065X3",
  "condition":"New","purchaseDate":"2026-07-31","cost":210,"location":"HQ / Store Room",
  "notes":"","quantity":3}' | jq .
```

**The invariant check.** A fabricated employee id is rejected, which proves `Foreign Keys=True`
reached the connection string rather than a `PRAGMA` that only applied to one pooled connection
(SPEC.md §3.3 item 2, §4.4 invariant 5):

```bash
# re-select a still-in-stock asset — the block above left $ASSET in Repair
FRESH=$(curl -s http://localhost:5198/api/state | jq -r 'first(.assets[] | select(.status=="InStock") | .id)')
curl -s -X POST http://localhost:5198/api/assets/$FRESH/assign \
  -H 'Content-Type: application/json' -d '{"employeeId":"not-a-real-id","assignedDate":"2026-07-31"}'
# → {"error":"That asset no longer exists. Refresh to see current data."}
```

Reuse `$ASSET` instead and you get `AST-1003 is in repair and cannot be assigned.` — the status guard
(invariant 6) fires before the foreign key does. Both are `AssetDeskException` messages from §4.6, so
either proves the point; just know which one you are looking at.

**The data is a file.** WAL mode lets you read it while the app holds it open:

```bash
DB=$(find AssetDesk -name 'assetdesk.db' | head -1)
sqlite3 "$DB" "select tag,status,assigned_to from assets order by tag;"
```

Then walk SPEC.md §5's acceptance criteria in the browser and §9's definition of done. `dotnet build
AssetDesk` reporting `0 Warning(s), 0 Error(s)` is one of them, nullable warnings included.

---

## 5. Reset the data

Stop the app, delete the database, restart — the seed rebuilds itself.

```bash
# Ctrl+C, then:
find AssetDesk -name 'assetdesk.db*' -delete
dotnet run --project AssetDesk --urls http://localhost:5198
```

Three things behind that one line:

- **All three files, not just one.** WAL leaves `assetdesk.db-wal` and `assetdesk.db-shm` beside the
  database; deleting only the base file leaves a confusing half-state. The `find` catches all three.
- **Do not reach for a `[Dd]ata/assetdesk.db*` glob.** When it matches nothing zsh aborts the whole
  command with `no matches found` **before `rm` ever runs** — exit 1, database untouched, and you
  carry on believing you reset it. `find … -delete` has no such failure mode.
- **Which folder is it in?** SPEC.md puts source in `Data/` and the database in `data/`. APFS is
  case-insensitive, so those are the same physical directory and the `.db` lands beside the `.cs`
  files. That is why the commands discover the path with `find` instead of hardcoding it.

Back to a clean repo:

```bash
git checkout main && rm -rf AssetDesk
```

---

## 6. Branches, and the one that saves you

| Branch | What it is | When |
|---|---|---|
| `main` | Spec only: `SPEC.md`, `README.md`, the two instructions files, `.gitignore` | **Start here.** Build live from the spec. |
| `dotnet-demo` | `main` plus the finished app in `AssetDesk/` | Your build broke or ran long. Check it out and keep up. |
| `nodejs-demo` | Archive of the pre-.NET attempt — a bare `create-next-app` scaffold and the old v2.0 Next.js spec | Historical curiosity. **Not a working app.** |

`nodejs-demo` holds the stock `create-next-app` welcome page and zero asset-management code — no
components, no data layer, no API — and would need a `pnpm install` before it even started.
`dotnet-demo` is the only real fallback.

### Starting clean

```bash
git checkout main
rm -rf AssetDesk       # bin/, obj/ and assetdesk.db* are gitignored and SURVIVE a branch switch
git status             # clean
ls                     # INSTRUCTIONS-MACOS.md  INSTRUCTIONS-WINDOWS.md  README.md  SPEC.md
```

That `rm -rf` is not optional. Because the build output and database are gitignored, `git checkout
main` cannot remove them, and the next build scaffolds into a directory that already holds a
**populated** database — the seed then skips, and your very first verification prints the wrong
count.

### Falling back mid-build

```bash
rm -rf AssetDesk       # REQUIRED FIRST
git checkout dotnet-demo
dotnet run --project AssetDesk --urls http://localhost:5198
```

The `rm -rf` is what makes this work. If your build reached the scaffold, those files are
*untracked*, and git refuses to clobber untracked files:

```
error: The following untracked working tree files would be overwritten by checkout:
	AssetDesk/AssetDesk.csproj
	AssetDesk/Components/App.razor
	... 9 more
Aborting
```

Neither `git switch` nor `git stash` helps — the files are untracked, not modified. Delete then check
out. (`git checkout -f dotnet-demo` also works but leaves stray scaffold files behind.)

---

## 7. Two traps worth knowing before you hit them

**1. SPEC.md §3.2 ignores the database by name on purpose. Do not "fix" it to `data/`.**
`Data/` (source) and `data/` (database) are the same physical directory on case-insensitive APFS, so
a `data/` rule also matches `Data/Models.cs`, `Db.cs`, `Format.cs` and `AssetRepository.cs`.
`git check-ignore -v AssetDesk/Data/Models.cs` reports the match, and `git add -A` then stages
nothing but the `.gitignore` itself. Your entire data layer drops out of `git status` while the build
keeps succeeding — so the commit at the end of the session ships an app with no SQL in it. Ignoring
the three `assetdesk.db*` files by name is equivalent and safe everywhere. A spec that is correct on
Linux and wrong on a Mac, in one line.

**2. `--empty` does not give you an empty project on SDK 10.0.302.** The scaffold command in SPEC.md
§3.1 is accepted and still emits six files that §2 explicitly bans — component-scoped CSS and JS
interop:

```
Components/Layout/MainLayout.razor.css        Components/Pages/Error.razor
Components/Layout/ReconnectModal.razor        Components/Pages/NotFound.razor
Components/Layout/ReconnectModal.razor.css    Components/Layout/ReconnectModal.razor.js
```

§3.1 only orders that cleanup "if your SDK rejects `--empty`" — which it does not. Check before you
sign off on §9:

```bash
find AssetDesk -name '*.razor.css' -o -name '*.razor.js'      # must print nothing
```

---

## 8. When it goes wrong

| Symptom | Fix |
|---|---|
| `git checkout dotnet-demo` → `untracked working tree files would be overwritten` | Your build already scaffolded `AssetDesk/`. `rm -rf AssetDesk`, then check out (§6). `git stash`/`git switch` will not help — untracked, not modified. |
| `Failed to bind to address http://127.0.0.1:5198: address already in use` | An older run is still listening. `lsof -nP -iTCP:5198 -sTCP:LISTEN` then `kill <PID>`, or move: `--urls http://localhost:5299`. Stale `dotnet watch` processes outlive a closed tab. |
| `/api/health` answers but `/api/state` 500s with `SQLite Error 14: 'unable to open database file'` | You are talking to a **stale process from a directory that no longer exists**, not your app. `lsof` for the PID, confirm with `ps aux \| grep AssetDesk`, kill it. |
| `/api/state` shows 13 assets, not 12 | An old database survived. `find AssetDesk -name 'assetdesk.db*' -delete` and restart. |
| `rm -f …/[Dd]ata/assetdesk.db*` → `zsh: no matches found` | zsh aborts on an unmatched glob and `rm` never runs, so nothing was deleted. Use `find AssetDesk -name 'assetdesk.db*' -delete`. |
| `MSBUILD : error MSB1003: Specify a project or solution file` | You ran `dotnet build` with no project. Everything here is root-relative: `dotnet build AssetDesk`, `dotnet run --project AssetDesk`. |
| Buttons and tabs do nothing, but the build is clean | No interactive render mode. `Components/App.razor` needs `@rendermode="InteractiveServer"` on both `<HeadOutlet>` and `<Routes>` (SPEC.md §3.3 item 1). Invisible in build output — the #1 Blazor trap. |
| Search only filters when you click away | `@bind:event="oninput"` is missing on the search input (SPEC.md §3.3 item 5). |
| `NU1903` NuGet audit warning breaks the zero-warning goal | Known: `SQLitePCLRaw.lib.e_sqlite3` is pinned transitively by `Microsoft.Data.Sqlite` 10.0.10 and has no newer patch. `dotnet-demo` suppresses it with `<NoWarn>$(NoWarn);NU1903</NoWarn>` and a comment saying why. |
| A `.razor` edit is not showing up | Restart `dotnet watch`. Hot reload does not pick up new components or changed parameters. |
| NuGet restore fails | No network, or a corporate proxy. `dotnet nuget locals all --clear` and retry; hotspot as a last resort. |
| On `nodejs-demo`, `git checkout` refuses over `.DS_Store` | That branch tracks `.DS_Store` (the others ignore it) and Finder rewrites it whenever you open the folder. `git checkout -- .DS_Store`, then switch. |
| A new slash command or skill is missing from `/` | Restart the session: `/exit`, then `claude`. `/clear` is **not** enough — commands and skills register at startup only. |
| `CLAUDE.md` looks ignored | `/context`. If it is not listed, `claude` was launched outside the repo or the filename is wrong (`CLAUDE.md`, exactly). |
| Anything else odd in Claude Code | `/doctor` — it diagnoses install and config and can apply fixes itself. |

---

## 9. Cheat sheet

All from the repo root.

```bash
# branches
git checkout main && rm -rf AssetDesk                    # clean starting point
rm -rf AssetDesk && git checkout dotnet-demo             # fall back to the finished app

# build & run
dotnet build AssetDesk
dotnet run   --project AssetDesk --urls http://localhost:5198
dotnet watch --project AssetDesk --urls http://localhost:5198

# what the agent runs for you (SPEC.md §3.1)
dotnet new blazor -o AssetDesk -f net10.0 --interactivity Server --all-interactive --empty
dotnet add AssetDesk package Dapper
dotnet add AssetDesk package Microsoft.Data.Sqlite

# inspect
curl -s http://localhost:5198/api/health
curl -s http://localhost:5198/api/state | jq '{employees:(.employees|length), assets:(.assets|length)}'
DB=$(find AssetDesk -name 'assetdesk.db' | head -1) && sqlite3 "$DB" "select count(*) from assets;"

# reset data
find AssetDesk -name 'assetdesk.db*' -delete

# spec spot-checks
find AssetDesk -name '*.razor.css' -o -name '*.razor.js'   # must print nothing

# who has my port
lsof -nP -iTCP:5198 -sTCP:LISTEN

# environment
dotnet --version && dotnet --list-sdks
claude --version && claude doctor
```
