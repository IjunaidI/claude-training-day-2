# INSTRUCTIONS — running the AssetDesk demo

Everything in this repo is built **from `SPEC.md`** by Claude Code. `main` tracks three files —
`SPEC.md`, this file, and `.gitignore` — and nothing else. No boilerplate is pre-scaffolded: the
agent runs `dotnet new blazor` itself as instructed in SPEC.md §3.1.

**Every command below runs from the repo root.** Nothing needs `cd AssetDesk`; the `dotnet` commands
take `--project AssetDesk` instead. Copy-paste works from one directory throughout.

Verified on: macOS (Darwin 25.5 / arm64), .NET SDK **10.0.302**, sqlite3 3.51.0, jq 1.7.

---

## 0. Branches — read this first

| Branch | What it is | Use it when |
|---|---|---|
| `main` | Spec-only starting point: `SPEC.md` + `INSTRUCTIONS.md` + `.gitignore` | **The class starts here.** Build live from the spec. |
| `dotnet-demo` | `main` **plus the finished .NET app** in `AssetDesk/` | The live build fails or runs long — check this out and demo the working app. |
| `nodejs-demo` | Archive of the pre-.NET attempt: a bare `create-next-app` scaffold in `simple-assets-management/` plus the old **v2.0 Next.js spec** | Historical reference only. **This is not a working app** — see the warning below. |

> **`nodejs-demo` is not a fallback.** It holds the stock `create-next-app` welcome page
> (`app/page.tsx` is the "To get started, edit the page.tsx file" template, 65 lines) and zero
> asset-management code — no components, no data layer, no API. It also needs a `pnpm install` with
> network before it would even start. `dotnet-demo` is the only branch you can fall back to on stage.

### Start the class on a clean `main`

```bash
git checkout main
rm -rf AssetDesk          # clears build output and the sqlite db, which are gitignored
                          # and therefore SURVIVE a branch switch
git status                # must be clean apart from the PDFs
ls                        # must show: INSTRUCTIONS.md  SPEC.md  (+ your PDFs)
```

That `rm -rf` is not optional. `bin/`, `obj/` and `assetdesk.db*` are gitignored, so
`git checkout main` cannot remove them. Left behind, the next live build scaffolds into a directory
that already holds a **populated** database — the seed is then skipped and the class's first
verification command prints the wrong asset count.

### Falling back mid-demo

```bash
rm -rf AssetDesk          # REQUIRED FIRST — see below
git checkout dotnet-demo
dotnet run --project AssetDesk --urls http://localhost:5198
```

**The `rm -rf` is what makes this work.** If the live build got as far as the scaffold, the files it
created are *untracked*, and git refuses to overwrite untracked files:

```
error: The following untracked working tree files would be overwritten by checkout:
	AssetDesk/AssetDesk.csproj
	AssetDesk/Components/App.razor
	... 9 more
Aborting
```

Neither `git switch` nor `git stash` helps — the files are untracked, not modified. `rm -rf AssetDesk`
then checking out is verified clean. (`git checkout -f dotnet-demo` also works but leaves six stray
scaffold files behind.)

---

## 1. Install before class (podium machine + attendees)

| Need | Install | Verify |
|---|---|---|
| Node 18+ | `brew install node` | `node --version` |
| Claude Code | `npm install -g @anthropic-ai/claude-code` | `claude --version` |
| .NET SDK 10 | `brew install --cask dotnet-sdk` or [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download) | `dotnet --version` → `10.x` |
| sqlite3 | preinstalled on macOS | `sqlite3 --version` |
| jq | preinstalled on macOS (`/usr/bin/jq`) | `jq --version` |
| curl, git | preinstalled on macOS | `curl --version` |

That is the whole list. **No npm install for the app** — it is .NET, not Node. The two NuGet packages
(`Dapper`, `Microsoft.Data.Sqlite`) restore automatically on first build, so the machine needs
internet for that first `dotnet build`. Nothing else to install.

**`dotnet: command not found`** — the installer drops `/etc/paths.d/dotnet` pointing at
`/usr/local/share/dotnet`, and `path_helper` only reads it for **new login shells**. Open a fresh
Terminal tab, or in the current one:

```bash
export PATH="/usr/local/share/dotnet:$PATH"
```

**`claude: command not found`** — npm's global bin is off PATH:

```bash
echo 'export PATH="$(npm config get prefix)/bin:$PATH"' >> ~/.zshrc && source ~/.zshrc
```

Sign in once with `claude` and complete the browser flow. Do **not** leave `ANTHROPIC_API_KEY` set in
the environment — it bills the API account instead of the subscription.

---

## 2. Start the demo

```bash
cd <your clone of spec-driven-demo>
git checkout main && rm -rf AssetDesk        # §0 — start clean
claude
```

Pin the fast, cheap model before prompting — this is what makes the build land in minutes, and it has
been proven end to end with Sonnet:

```
/model sonnet
```

Then the prompt — one line, nothing more:

```
Read SPEC.md and build it.
```

SPEC.md §8 tells it to work in milestones M0→M7 and §10 tells it not to stop and ask. Expect a
one-line progress note per milestone.

> **Keep two terminal tabs open** (`Cmd+T`): one running `claude`, one for `dotnet` and `curl`.
> `Cmd+K` clears the screen between demos.

---

## 3. Boot the app

From the repo root:

```bash
dotnet build AssetDesk                                          # also restores the 2 NuGet packages
dotnet run --project AssetDesk --urls http://localhost:5198
```

Then open <http://localhost:5198>. Stop with `Ctrl+C`.

**Pin the port with `--urls`.** The scaffold writes a *random* port into
`AssetDesk/Properties/launchSettings.json`, so a freshly generated project will not be on 5198 and
the curl commands below would miss it. Passing `--urls` overrides it. (Without it, read the port off
the `Now listening on: http://localhost:XXXX` startup line.)

### Live-reload while iterating

```bash
dotnet watch --project AssetDesk --urls http://localhost:5198
```

Hot reload handles markup and method bodies. **Restart it manually** after adding a new component,
changing a `[Parameter]`, or editing `Program.cs` — SPEC.md §8 calls this out, and it is the most
common "why isn't my change showing" moment.

---

## 4. Verify it works (the M0/M1 command-line checks)

These prove the data layer before any UI exists — the whole point of SPEC.md's build order.

```bash
# health
curl -s http://localhost:5198/api/health
# → {"ok":true}

# seed state: exactly 5 employees and 12 assets
curl -s http://localhost:5198/api/state | jq '{employees: (.employees|length), assets: (.assets|length)}'
# → { "employees": 5, "assets": 12 }
```

> Seeing **13** assets means a database from an earlier run survived — the seed only runs on an empty
> database. Reset it (§5).

Grab ids into shell variables so the mutation calls read cleanly:

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

# create 3 assets in one transaction (sequential tags AST-1013/14/15)
curl -s -X POST http://localhost:5198/api/assets -H 'Content-Type: application/json' -d '{
  "tag":"AST-1013","category":"Headset","make":"Jabra","model":"Evolve2 65","serial":"JB2065X3",
  "condition":"New","purchaseDate":"2026-07-31","cost":210,"location":"HQ / Store Room",
  "notes":"","quantity":3}' | jq .
```

**The invariant demo** — a fabricated employee id is rejected, proving `Foreign Keys=True` took
effect in the connection string (SPEC.md §9). Verified output:

```bash
# re-select a still-in-stock asset: the block above left $ASSET in Repair
FRESH=$(curl -s http://localhost:5198/api/state | jq -r 'first(.assets[] | select(.status=="InStock") | .id)')
curl -s -X POST http://localhost:5198/api/assets/$FRESH/assign \
  -H 'Content-Type: application/json' -d '{"employeeId":"not-a-real-id","assignedDate":"2026-07-31"}'
# → {"error":"That asset no longer exists. Refresh to see current data."}
```

Reuse `$ASSET` here instead and you get `AST-1003 is in repair and cannot be assigned.` — the
status guard (invariant 6) fires before the foreign key does. Both are `AssetDeskException`
messages from SPEC.md §4.6, so either one still makes the point; just know which you are showing.

**Show the data really lives in a file** (works while the app runs, thanks to WAL mode):

```bash
DB=$(find AssetDesk -name 'assetdesk.db' | head -1)
sqlite3 "$DB" "select tag,status,assigned_to from assets order by tag;"
```

### Definition-of-done check

```bash
dotnet build AssetDesk     # must report: 0 Warning(s), 0 Error(s)
```

---

## 5. Reset the demo data

Stop the app first, then delete the database — the seed rebuilds itself on the next boot.

```bash
# Ctrl+C the app, then:
find AssetDesk -name 'assetdesk.db*' -delete
dotnet run --project AssetDesk --urls http://localhost:5198
```

Two details worth saying out loud:

- **Delete all three files, not just one.** WAL mode leaves `assetdesk.db-wal` and `assetdesk.db-shm`
  beside the database; removing only the base file leaves a confusing half-state. The `find` above
  catches all three.
- **Do not use a `[Dd]ata/assetdesk.db*` glob.** When it matches nothing, zsh aborts the command with
  `no matches found` **before `rm` runs at all** — exit 1, database untouched, and you demo stale rows
  believing you reset them. `find … -delete` has no such failure mode.
- **Which folder is it in?** SPEC.md puts source in `Data/` and the database in `data/`. macOS APFS is
  case-insensitive, so both resolve to the *same* physical directory and the db lands next to the
  `.cs` files. That is why the commands above discover the path with `find` instead of hardcoding it.

### Full reset before class (back to an empty repo)

```bash
git checkout main
rm -rf AssetDesk        # deletes the generated app entirely — SPEC.md rebuilds it,
                        # and dotnet-demo still has the finished copy
```

---

## 6. Two rehearsal findings worth knowing

**1. Why SPEC.md §3.2 ignores the database by name — already fixed, do not "correct" it back.**
The spec used to say "Add `data/` to `.gitignore`", which is a trap on this machine: `Data/` (source)
and `data/` (database) are the same physical directory on case-insensitive APFS, so a `data/` rule also
matches `Data/Models.cs`, `Db.cs`, `Format.cs` and `AssetRepository.cs`. Verified during rehearsal —
`git check-ignore -v AssetDesk/Data/Models.cs` reports the `data/` rule matching, and `git add -A`
then stages nothing but the `.gitignore` itself. The data layer vanishes from `git status` while the
build keeps succeeding, so a commit at the end of class ships an app with no SQL in it. §3.2 now orders
`bin/`, `obj/` and the three `assetdesk.db*` files ignored **by name** instead, with the reason inline.
If an agent proposes replacing that with `data/`, say no and explain why — it is a good five-second
lesson in specs that are correct on Linux and wrong on a Mac.

**2. `--empty` does not give an empty project on SDK 10.0.302.** The scaffold command in SPEC.md §3.1
is accepted, and still emits six files that SPEC.md §2 explicitly bans:

```
Components/Layout/MainLayout.razor.css        Components/Pages/Error.razor
Components/Layout/ReconnectModal.razor        Components/Pages/NotFound.razor
Components/Layout/ReconnectModal.razor.css    Components/Layout/ReconnectModal.razor.js
```

That is component-scoped CSS and JS interop, both forbidden. SPEC.md only orders the cleanup "if your
SDK rejects `--empty`" — which it does not. Check for these before the definition-of-done review:

```bash
find AssetDesk -name '*.razor.css' -o -name '*.razor.js'      # must print nothing
```

---

## 7. When it goes wrong

| Symptom | Fix |
|---|---|
| `git checkout dotnet-demo` → `untracked working tree files would be overwritten` | The live build already scaffolded `AssetDesk/`. `rm -rf AssetDesk` then check out again (§0). `git stash` and `git switch` will not help — those files are untracked, not modified. |
| `Failed to bind to address http://127.0.0.1:5198: address already in use` | An old run is still listening. `lsof -nP -iTCP:5198 -sTCP:LISTEN` then `kill <PID>`, or boot elsewhere: `--urls http://localhost:5299`. Stale `dotnet watch` processes survive a closed terminal tab. |
| App answers `/api/health` but `/api/state` 500s with `SQLite Error 14: 'unable to open database file'` | You are talking to a **stale process from a deleted directory**, not your app. Find the PID with `lsof`, confirm its path with `ps aux \| grep AssetDesk`, kill it. |
| `/api/state` shows 13 assets, not 12 | A database from an earlier run survived. `find AssetDesk -name 'assetdesk.db*' -delete` and restart. |
| `rm -f …/[Dd]ata/assetdesk.db*` → `zsh: no matches found` | zsh aborts on an unmatched glob and `rm` never runs. Use `find AssetDesk -name 'assetdesk.db*' -delete`. |
| `MSBUILD : error MSB1003: Specify a project or solution file` | You ran `dotnet build` with no project. Every command here is root-relative: `dotnet build AssetDesk`, `dotnet run --project AssetDesk`. |
| Buttons and tabs do nothing, but the build is clean | Interactive render mode is missing. `Components/App.razor` needs `@rendermode="InteractiveServer"` on both `<HeadOutlet>` and `<Routes>` (SPEC.md §3.3 item 1). Invisible in build output — the #1 Blazor trap. |
| Search box only filters when you click away | `@bind:event="oninput"` is missing on the search input (SPEC.md §3.3 item 5). |
| `NU1903` NuGet audit warning breaks the zero-warning goal | Known: `SQLitePCLRaw.lib.e_sqlite3` is pinned transitively by `Microsoft.Data.Sqlite` 10.0.10 with no newer patch. Suppressed on `dotnet-demo` via `<NoWarn>$(NoWarn);NU1903</NoWarn>` in the `.csproj`, with a comment explaining why. |
| `.razor` edit not showing up | Restart `dotnet watch` — hot reload does not pick up new components or changed parameters. |
| NuGet restore fails | No internet, or a corporate proxy. `dotnet nuget locals all --clear` then retry; phone hotspot for the podium machine as a last resort. |
| Stuck on `nodejs-demo`: `git checkout` refuses over `.DS_Store` | That branch tracks `.DS_Store` (the others ignore it) and Finder rewrites it on any folder view. `git checkout -- .DS_Store` then switch. |
| A new slash command or skill does not appear in `/` | Restart the session — `/exit` then `claude`. `/clear` is **not** enough; commands and skills register at session start only. |
| `CLAUDE.md` seems ignored | Run `/context`. If it is not listed, `claude` was launched outside the repo, or the filename is wrong (`CLAUDE.md`, exact). |
| Anything else odd in Claude Code | `/doctor` — it diagnoses install and config, and can apply fixes itself. |

**The stage rule:** if a fix takes longer than a minute in front of the room, switch to the deck
screenshot, note it on the board, and fix it at the break.

---

## 8. Command cheat sheet

All from the repo root.

```bash
# branches
git checkout main && rm -rf AssetDesk                    # clean starting point
rm -rf AssetDesk && git checkout dotnet-demo             # fall back to the finished app

# build & run
dotnet build AssetDesk
dotnet run   --project AssetDesk --urls http://localhost:5198
dotnet watch --project AssetDesk --urls http://localhost:5198

# create the project from scratch (SPEC.md §3.1 — the agent does this for you)
dotnet new blazor -o AssetDesk -f net10.0 --interactivity Server --all-interactive --empty
dotnet add AssetDesk package Dapper
dotnet add AssetDesk package Microsoft.Data.Sqlite

# inspect
curl -s http://localhost:5198/api/health
curl -s http://localhost:5198/api/state | jq '{employees:(.employees|length), assets:(.assets|length)}'
DB=$(find AssetDesk -name 'assetdesk.db' | head -1) && sqlite3 "$DB" "select count(*) from assets;"

# reset data
find AssetDesk -name 'assetdesk.db*' -delete

# spec-compliance spot checks
find AssetDesk -name '*.razor.css' -o -name '*.razor.js'   # must print nothing

# who is on my port
lsof -nP -iTCP:5198 -sTCP:LISTEN

# environment
dotnet --version && dotnet --list-sdks
claude --version && node --version
```
