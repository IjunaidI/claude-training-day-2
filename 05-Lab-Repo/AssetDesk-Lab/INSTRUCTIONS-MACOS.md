# INSTRUCTIONS — AssetDesk on macOS

Every command here runs **from the repository root**. The `dotnet` commands take
`--project AssetDesk`, so copy-paste works from one directory throughout.
On Windows, use [INSTRUCTIONS-WINDOWS.md](INSTRUCTIONS-WINDOWS.md). The labs themselves are in
[lab/LAB-GUIDE.md](lab/LAB-GUIDE.md).

---

## Quick start

```bash
bash scripts/check-setup.sh                                    # six [ OK ] lines
dotnet build AssetDesk                                         # expect 0 Warning(s), 0 Error(s)
dotnet run --project AssetDesk --urls http://localhost:5198    # Ctrl+C to stop
open http://localhost:5198
```

`AssetDesk/` does not exist until the Lab 1 build creates it.

---

## 1. Install

| Need | Install | Verify |
|---|---|---|
| Claude Code | `curl -fsSL https://claude.ai/install.sh \| bash` | `claude --version` |
| .NET SDK 10 | `brew install --cask dotnet-sdk` or [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download/dotnet/10.0) | `dotnet --version` → `10.x` |
| Node.js LTS (18 or later) | `brew install node` or the installer from [nodejs.org](https://nodejs.org) | `node --version`, `npx --version` |
| git, curl | preinstalled | `git --version` |
| jq, sqlite3 *(optional)* | `brew install jq`; sqlite3 is preinstalled | `jq --version` |

Node.js is only for Lab 6: `npx` downloads and starts the Playwright browser server. The app itself
has no `npm install`. NuGet packages restore on the first build, so that build needs network. Lab 6
drives Google Chrome by default; without Chrome, see §7.

Sign in once: run `claude`, complete the browser flow, then `/exit`. This needs a **Pro, Max, Team,
or Enterprise** plan. Leave `ANTHROPIC_API_KEY` unset, or you are billed through the API instead of
the subscription.

**`dotnet: command not found`** — open a new Terminal tab, or `export PATH="/usr/local/share/dotnet:$PATH"`.

**`claude: command not found`** — the installer puts it in `~/.local/bin`:
`echo 'export PATH="$HOME/.local/bin:$PATH"' >> ~/.zshrc && source ~/.zshrc`

Anything else off: `claude doctor`.

---

## 2. Build it from the spec

Lab 1 in [lab/LAB-GUIDE.md](lab/LAB-GUIDE.md) walks you through it: a branch, plan mode, then the
build. Keep a second Terminal tab open (`Cmd+T`) for `dotnet`, `curl` and `git` while the first runs
`claude`.

---

## 3. Run it

```bash
dotnet build AssetDesk
dotnet run --project AssetDesk --urls http://localhost:5198
```

Then <http://localhost:5198>. **Always pass `--urls`**: the scaffold writes a random port into
`launchSettings.json`, and every check below and the Lab 6 browser check expect 5198.

While iterating: `dotnet watch --project AssetDesk --urls http://localhost:5198`. Restart it by hand
after adding a component, changing a `[Parameter]`, or editing `Program.cs`.

---

## 4. Verify it

```bash
curl -s http://localhost:5198/api/health
# → {"ok":true}
curl -s http://localhost:5198/api/state | jq '{employees: (.employees|length), assets: (.assets|length)}'
# → { "employees": 5, "assets": 12 }
```

13 or more assets means a database from an earlier run survived. Reset it (§5).

**The invariant check** (SPEC.md §9): a fabricated employee id is rejected.

```bash
FRESH=$(curl -s http://localhost:5198/api/state | jq -r 'first(.assets[] | select(.status=="InStock") | .id)')
curl -s -X POST http://localhost:5198/api/assets/$FRESH/assign \
  -H 'Content-Type: application/json' -d '{"employeeId":"not-a-real-id","assignedDate":"2026-01-15"}'
# → {"error":"That asset no longer exists. Refresh to see current data."}
```

**The status guard**: AST-1004 is seeded in repair (SPEC.md §7), so assigning it is refused.

```bash
REPAIR=$(curl -s http://localhost:5198/api/state | jq -r '.assets[] | select(.tag=="AST-1004") | .id')
curl -s -X POST http://localhost:5198/api/assets/$REPAIR/assign \
  -H 'Content-Type: application/json' -d '{"employeeId":"x","assignedDate":"2026-01-15"}'
# → {"error":"AST-1004 is in repair and cannot be assigned."}
```

---

## 5. Reset the data

Stop the app, delete the database **and** its `-wal` and `-shm` files, restart. The seed rebuilds.

```bash
# Ctrl+C, then:
find AssetDesk -name 'assetdesk.db*' -delete
dotnet run --project AssetDesk --urls http://localhost:5198
```

- **All three files.** Deleting only `assetdesk.db` leaves a confusing half-state, because WAL mode
  keeps live pages in the sidecar files. The `find` catches all three.
- **Do not use a `[Dd]ata/assetdesk.db*` glob.** When it matches nothing, zsh aborts with
  `no matches found` before `rm` runs, and you believe you reset it. `find … -delete` has no such
  failure mode.
- **Which folder?** SPEC.md puts source in `Data/` and the database in `data/`. APFS is
  case-insensitive, so they are the same folder. That is why the command searches instead of
  naming a path.

---

## 6. Falling back to the reference build

Behind at the DoD check? Commit your work, then start a branch from the finished app:

```bash
git add -A && git commit -m "wip: my build"
git switch -c build/<name>-ref origin/reference-build
find AssetDesk -name 'assetdesk.db*' -delete        # the old database survives a branch switch
dotnet run --project AssetDesk --urls http://localhost:5198
```

Your own build stays on the branch you left. Never commit to `reference-build` itself: the whole
room shares it.

---

## 7. When it goes wrong

| Symptom | Fix |
|---|---|
| `address already in use` on 5198 | An older run is still listening. `lsof -nP -iTCP:5198 -sTCP:LISTEN`, then `kill <PID>`. |
| `/api/state` shows 13+ assets | An old database survived. §5. |
| `MSB1003: Specify a project or solution file` | You ran `dotnet build` at the root. Name the project: `dotnet build AssetDesk`. |
| Buttons and tabs do nothing, build is clean | No interactive render mode. SPEC.md §3.3 item 1. |
| Search only filters when you click away | `@bind:event="oninput"` is missing. SPEC.md §3.3 item 5. |
| `NU1903` NuGet audit warning breaks zero-warnings | Known transitive advisory in `SQLitePCLRaw`. Suppress it with `<NoWarn>$(NoWarn);NU1903</NoWarn>` and a comment saying why. |
| NuGet restore fails | No network, or a proxy blocks api.nuget.org. `dotnet nuget locals all --clear`, retry. |
| A new skill or agent is missing from `/` | Skills register at startup. `/exit`, then `claude`. `/clear` is not enough. |
| Claude says it cannot edit `SPEC.md` | Working as intended: `.claude/settings.json` denies it. |
| `/mcp` does not list `microsoft-learn` or `playwright` | Servers load when a session starts. `/exit`, then `claude`. Run `claude mcp list` from the repository root: a server added with the default (local) scope belongs to the folder you added it in. |
| `npx: command not found` | Node.js is missing or not on PATH. Install it (§1), open a new Terminal tab, then `/exit` and `claude`. |
| `/mcp` shows `playwright` failed | Start it by hand to see why: `npx @playwright/mcp@latest --help`. The first run downloads the package, so it needs network. |
| npx hangs, or fails with `ETIMEDOUT`, `ECONNREFUSED`, `E403` or `SELF_SIGNED_CERT_IN_CHAIN` | A proxy blocks registry.npmjs.org. Ask IT for the proxy address and run `npm config set proxy http://<host>:<port>` and `npm config set https-proxy http://<host>:<port>`. Meanwhile do Lab 6 Part A: Microsoft Learn needs no npm. |
| Playwright says `Chromium distribution 'chrome' is not found` | Chrome is not installed. Install Google Chrome, or switch to Edge: `claude mcp remove playwright`, then `claude mcp add playwright -- npx @playwright/mcp@latest --browser msedge`. |
| The browser opens but shows `This site can't be reached` | The app is not running on 5198. Start it (§3) in another tab. |
| `/mcp` shows `microsoft-learn` failed | Your network blocks learn.microsoft.com. Open https://learn.microsoft.com in a browser to confirm, then ask IT. |
| Anything else in Claude Code | `/doctor`. |
