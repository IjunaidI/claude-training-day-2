# INSTRUCTIONS — running the AssetDesk demo

Everything in this repo is built **from `SPEC.md`** by Claude Code. On training day the repo
contains `SPEC.md` and this file, nothing else. No boilerplate is pre-scaffolded — the agent runs
`dotnet new blazor` itself as instructed in SPEC.md §3.1.

Verified on: macOS (Darwin 25.5 / arm64), .NET SDK **10.0.302**, sqlite3 3.51.0.

---

## 1. Install before class (podium machine + attendees)

| Need | Install | Verify |
|---|---|---|
| Node 18+ | `brew install node` | `node --version` |
| Claude Code | `npm install -g @anthropic-ai/claude-code` | `claude --version` |
| .NET SDK 10 | `brew install --cask dotnet-sdk` or [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download) | `dotnet --version` → `10.x` |
| sqlite3 CLI | preinstalled on macOS | `sqlite3 --version` |
| curl | preinstalled on macOS | `curl --version` |

That is the whole list. **No npm install for the app** — it is .NET, not Node. The two NuGet
packages (`Dapper`, `Microsoft.Data.Sqlite`) restore automatically on first build, so the machine
needs internet for that first `dotnet build`. Nothing to install globally beyond the SDK.

Optional but nice: `brew install jq` for pretty-printing the API responses.

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

Sign in once with `claude` and complete the browser flow. Do **not** leave `ANTHROPIC_API_KEY` set
in the environment — it bills the API account instead of the subscription.

---

## 2. Start the demo

```bash
cd ~/Documents/GitHub/spec-driven-demo
claude
```

Pin the fast/cheap model before prompting (this is what makes the build land in minutes, and it has
been proven to work end to end with Sonnet):

```
/model sonnet
```

Then the prompt — one line, nothing more:

```
Read SPEC.md and build it.
```

Let it run. SPEC.md §8 tells it to work in milestones M0→M7 and §10 tells it not to stop and ask.
Expect a one-line progress note per milestone.

> **Keep two terminal tabs open** (`Cmd+T`): one running `claude`, one for `dotnet run` and `curl`.
> `Cmd+K` clears the screen between demos.

---

## 3. Boot the app

The project lives in `AssetDesk/`. Run from that folder:

```bash
cd AssetDesk
dotnet build                                       # first run also restores the 2 NuGet packages
dotnet run --urls http://localhost:5198            # pin the port so every command below works as-is
```

Then open <http://localhost:5198>.

**Pin the port with `--urls`.** The scaffold writes a *random* port into
`Properties/launchSettings.json`, so a freshly generated project will not be on 5198. Either pass
`--urls` as above, or read the real port from the `Now listening on: http://localhost:XXXX` line in
the startup output (also in `AssetDesk/Properties/launchSettings.json`).

Stop the app with `Ctrl+C`.

### Live-reload while iterating

```bash
dotnet watch --urls http://localhost:5198
```

Hot reload handles markup and method bodies. **Restart it manually** after adding a new component,
changing a `[Parameter]`, or editing `Program.cs` — SPEC.md §8 calls this out and it is the most
common "why isn't my change showing" moment.

---

## 4. Verify it works (the M0/M1 command-line checks)

These prove the data layer before any UI exists — the whole point of SPEC.md's build order.

```bash
# health
curl -s http://localhost:5198/api/health
# → {"ok":true}

# seed state: 5 employees, 12 assets
curl -s http://localhost:5198/api/state | python3 -m json.tool | head -30
curl -s http://localhost:5198/api/state \
  | python3 -c "import sys,json;d=json.load(sys.stdin);print('employees',len(d['employees']),'assets',len(d['assets']))"
# → employees 5 assets 12
```

Grab ids into shell vars so the mutation calls read cleanly:

```bash
STATE=$(curl -s http://localhost:5198/api/state)
ASSET=$(echo "$STATE" | python3 -c "import sys,json;d=json.load(sys.stdin);print([a['id'] for a in d['assets'] if a['status']=='InStock'][0])")
EMP=$(echo "$STATE"   | python3 -c "import sys,json;d=json.load(sys.stdin);print(d['employees'][0]['id'])")
```

```bash
# assign → return → repair
curl -s -X POST http://localhost:5198/api/assets/$ASSET/assign \
  -H 'Content-Type: application/json' -d "{\"employeeId\":\"$EMP\",\"assignedDate\":\"2026-07-31\"}"
curl -s -X POST http://localhost:5198/api/assets/$ASSET/return
curl -s -X POST http://localhost:5198/api/assets/$ASSET/status \
  -H 'Content-Type: application/json' -d '{"status":"Repair"}'

# create 3 assets in one transaction (sequential tags AST-1013/14/15)
curl -s -X POST http://localhost:5198/api/assets -H 'Content-Type: application/json' -d '{
  "tag":"AST-1013","category":"Headset","make":"Jabra","model":"Evolve2 65","serial":"JB2065X3",
  "condition":"New","purchaseDate":"2026-07-31","cost":210,"location":"HQ / Store Room",
  "notes":"","quantity":3}'
```

**The invariant demo** — a fabricated employee id is rejected, which proves `Foreign Keys=True`
took effect in the connection string (SPEC.md §9). Verified output:

```bash
curl -s -X POST http://localhost:5198/api/assets/$ASSET/assign \
  -H 'Content-Type: application/json' -d '{"employeeId":"not-a-real-id","assignedDate":"2026-07-31"}'
# → {"error":"That asset no longer exists. Refresh to see current data."}
```

**Show the data really lives in a file** (works while the app is running, because of WAL mode):

```bash
sqlite3 AssetDesk/Data/assetdesk.db "select tag,status,assigned_to from assets order by tag;"
```

### Definition-of-done check

```bash
dotnet build     # must be: 0 Warning(s), 0 Error(s)
```

---

## 5. Reset the demo data

Stop the app first, then delete the database and restart — the seed re-creates itself on next boot.

```bash
# Ctrl+C the app, then:
rm -f AssetDesk/[Dd]ata/assetdesk.db*
dotnet run --urls http://localhost:5198
```

Two details worth saying out loud:

- **The `*` matters.** WAL mode leaves `assetdesk.db-wal` and `assetdesk.db-shm` beside the
  database; deleting only the base file leaves a confusing half-state.
- **`[Dd]ata` is not a typo.** SPEC.md puts source in `Data/` and the database in `data/`. macOS
  APFS is case-insensitive by default, so both resolve to the *same* physical folder and the
  database lands next to the `.cs` files. The `.gitignore` therefore ignores the sqlite files
  **by name** rather than ignoring `data/`, which would also hide `Data/*.cs` from git.

### Full reset before class (back to an empty repo)

```bash
rm -rf AssetDesk        # deletes the generated app entirely — SPEC.md rebuilds it
```

---

## 6. When it goes wrong

| Symptom | Fix |
|---|---|
| `Failed to bind to address http://127.0.0.1:5198: address already in use` | An old run is still listening. `lsof -nP -iTCP:5198 -sTCP:LISTEN` then `kill <PID>`, or just boot on another port: `dotnet run --urls http://localhost:5299`. Stale `dotnet watch` processes survive a closed terminal tab — this happened during the rehearsal. |
| App answers `/api/health` but `/api/state` 500s with `SQLite Error 14: 'unable to open database file'` | You are talking to a **stale process from a deleted directory**, not your app. Same fix as above: find the PID with `lsof`, confirm its path in `ps aux \| grep AssetDesk`, kill it. |
| Buttons and tabs do nothing, but the build is clean | Interactive render mode is missing. `Components/App.razor` needs `@rendermode="InteractiveServer"` on both `<HeadOutlet>` and `<Routes>` (SPEC.md §3.3 item 1). Invisible in build output — this is the #1 Blazor trap. |
| Search box only filters when you click away | `@bind:event="oninput"` is missing on the search input (SPEC.md §3.3 item 5). |
| `NU1903` NuGet audit warning breaks the zero-warning goal | Known: `SQLitePCLRaw.lib.e_sqlite3` is pinned transitively by `Microsoft.Data.Sqlite` 10.0.10 with no newer patch. It is suppressed via `<NoWarn>$(NoWarn);NU1903</NoWarn>` in the `.csproj`, with a comment explaining why. |
| `.razor` edit not showing up | Restart `dotnet watch` — hot reload does not pick up new components or changed parameters. |
| NuGet restore fails | No internet, or a corporate proxy. `dotnet nuget locals all --clear` then retry; phone hotspot for the podium machine as a last resort. |
| A new slash command or skill does not appear in `/` | Restart the session — `/exit` then `claude`. `/clear` is **not** enough; commands and skills register at session start only. |
| `CLAUDE.md` seems ignored | Run `/context`. If it is not listed, `claude` was launched outside the repo, or the filename is wrong (`CLAUDE.md`, exact). |
| Anything else odd in Claude Code | `/doctor` — it diagnoses install and config, and can apply fixes itself. |

**The stage rule:** if a fix takes longer than a minute in front of the room, switch to the deck
screenshot, note it on the board, and fix it at the break.

---

## 7. Command cheat sheet

```bash
# build & run
dotnet build
dotnet run   --urls http://localhost:5198
dotnet watch --urls http://localhost:5198

# create the project from scratch (SPEC.md §3.1 — the agent does this for you)
dotnet new blazor -o AssetDesk -f net10.0 --interactivity Server --all-interactive --empty
cd AssetDesk && dotnet add package Dapper && dotnet add package Microsoft.Data.Sqlite

# inspect
curl -s http://localhost:5198/api/health
curl -s http://localhost:5198/api/state | python3 -m json.tool
sqlite3 AssetDesk/Data/assetdesk.db ".tables"
sqlite3 AssetDesk/Data/assetdesk.db "select count(*) from assets;"

# reset data
rm -f AssetDesk/[Dd]ata/assetdesk.db*

# who is on my port
lsof -nP -iTCP:5198 -sTCP:LISTEN

# environment
dotnet --version && dotnet --list-sdks
claude --version && node --version
```
