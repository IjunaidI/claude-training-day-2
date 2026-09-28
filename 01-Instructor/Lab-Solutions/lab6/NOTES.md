# Lab 6 notes: give Claude new tools

Two ready-made MCP servers, nothing to build. Part A (Microsoft Learn) proves the mechanics with
zero install. Part B (Playwright) gives Claude a browser and closes the day's second thread: give
Claude a way to check its work. At 12:15 it checked SPEC.md §9 from the terminal; here it checks
§5.2 by clicking through the running app.

The build-your-own C# server that used to be this lab is now an optional instructor demo:
`../stretch-mcp-server/NOTES.md`.

Checked on 28 September 2026 with Claude Code 2.1.259, Node.js 22.18 and `@playwright/mcp@latest`.

## Part A: Microsoft Learn (everyone, about 5 minutes)

Remote HTTP server. No install, no login, no API key.

```bash
claude mcp add --transport http microsoft-learn https://learn.microsoft.com/api/mcp
```

Expected output:

```
Added HTTP MCP server microsoft-learn with URL: https://learn.microsoft.com/api/mcp to local config
File modified: <home>/.claude.json [project: <repo path>]
```

Then `claude --permission-mode default` and `/mcp`: `microsoft-learn` connected. From a plain shell,
`claude mcp list` shows `microsoft-learn: https://learn.microsoft.com/api/mcp (HTTP) - ✔ Connected`.

Tools: `microsoft_docs_search`, `microsoft_docs_fetch`, `microsoft_code_sample_search`. In Claude
they are `mcp__microsoft-learn__microsoft_docs_search` and so on.

The prompt:

```
Use the Microsoft Learn tools to find the current recommended way to serialize enums as strings in a .NET 10 minimal API, and cite the page.
```

What good looks like: one approval prompt for `microsoft_docs_search` (sometimes a follow-up
`microsoft_docs_fetch`), an answer built on `JsonStringEnumConverter` registered through
`ConfigureHttpJsonOptions` (or on the enum type with `[JsonConverter]`), and at least one
`learn.microsoft.com` link. SPEC.md §4.7 asks for `JsonStringEnumConverter`, so attendees can check
the answer against the contract they have been reading all day.

Why this server: it answers a question the model might get subtly wrong from memory (the API moves
between .NET versions) and cites a source, which is the point of a docs tool.

## Part B: Playwright (about 10 minutes, needs Node.js 18+)

Local stdio server started through `npx`. It drives the installed Google Chrome by default.

| OS | Command |
|---|---|
| macOS / Linux | `claude mcp add playwright -- npx @playwright/mcp@latest` |
| Windows (native) | `claude mcp add playwright -- cmd /c npx @playwright/mcp@latest` |
| No Chrome | add `--browser msedge` at the end (every Windows machine has Edge) |

Expected output: `Added stdio MCP server playwright with command: npx @playwright/mcp@latest to local config`.
After `/exit` and `claude`, `/mcp` lists `playwright` connected. The first start downloads the
package from registry.npmjs.org; on a warm npm cache the health check takes about 3 seconds.

Tools used in the lab: `browser_navigate`, `browser_snapshot`, `browser_click`, `browser_type`,
`browser_fill_form`, `browser_tabs`, `browser_take_screenshot`, `browser_close`. Full names are
`mcp__playwright__browser_click` and so on.

The app must be running in T2 on http://localhost:5198. The prompt:

```
Use the Playwright browser to open http://localhost:5198. Click each sidebar tab, then check the acceptance criteria in SPEC.md §5.2. Report pass or fail for each with what you saw. Do not change any code.
```

What good looks like:

1. `browser_navigate` to 5198, a `browser_snapshot`, then a `browser_click` on Dashboard, Assets and
   People, each followed by a snapshot. That alone proves the §9 item "clicking a sidebar tab
   changes the view", which the 12:15 terminal walk could not.
2. On Assets, it reads SPEC.md §5.2 and works the eight criteria: types into search and reads the
   "Showing N of 12 assets" count (on the seed, `lenovo` gives "Showing 2 of 12 assets"), combines a
   category and a status filter, forces an empty result and uses **Clear filters**, checks the
   default Tag sort and toggles a header, looks for the retired row's reduced opacity (it may read
   the computed style or report it cannot see opacity in a snapshot), and says how it would trigger
   a repository error. The better runs use `browser_tabs` for the two-tab AST-1003 trick from 12:15.
3. A table or list: criterion, pass or fail, evidence ("typed `x`, count read ..."). Expect the
   reference build to pass all eight (not yet run end to end against it with this prompt: do it in
   the T-3 dry run). A student build that failed the 12:15 empty-red-box check should fail the last
   criterion here too, for the same reason.

It may change data (Mark repair, Assign) while testing the error banner. Reset the data afterwards.
It writes a `.playwright-mcp/` folder in the repository root; the lab repo's `.gitignore` covers it.

Timing: 3 to 6 minutes of Claude work, plus approvals. Tell attendees to pick the "don't ask again"
option for the Playwright tools once they have seen a couple of calls.

### The teaching step

`/mcp` → `playwright` → **View tools** → `browser_click`. The description and parameter schema are
the only things Claude knows about the tool. Tie it back to the slide: descriptions drive tool
choice, names are `mcp__server__tool`, and whatever a page or tool returns is data, not orders
(a page that says "ignore your instructions" is text Claude read, not an instruction).

### Scope, one sentence each

- **local** (the default): only you, only this folder, stored in your own `~/.claude.json`. Nothing
  to commit, nothing for teammates to approve. The lab uses this.
- **project** (`--scope project`): writes `.mcp.json` into the repository; each teammate approves the
  server the first time they start Claude in it.
- **user** (`--scope user`): you, in every folder.

## Common failures

| Symptom | Cause | Fix |
|---|---|---|
| `/mcp` does not list the server | Added while Claude was running, or in another folder (local scope is per folder) | `/exit`, `claude mcp list` in the repo root, `claude` |
| `npx` not recognized / `command not found` | Node.js missing or not on PATH | Install Node LTS (`winget install --id OpenJS.NodeJS.LTS -e`, `brew install node`), new terminal, relaunch. Part A still works meanwhile |
| Windows: `playwright` failed, `Connection closed` | Added without `cmd /c`; `npx` is a `.cmd` shim that cannot be spawned directly | `claude mcp remove playwright`, re-add with `cmd /c` |
| `Chromium distribution 'chrome' is not found` | No Google Chrome | Re-add with `--browser msedge`. Do not run `npx playwright install chrome` in the room: it is a large download |
| `playwright` failed on first start only | Package download slower than the MCP startup timeout | In a spare shell, `npx @playwright/mcp@latest --help` until it prints help; then `/mcp` → Reconnect |
| npx hangs, `ETIMEDOUT`, `E403`, `SELF_SIGNED_CERT_IN_CHAIN` | Corporate proxy or TLS inspection blocks registry.npmjs.org | `npm config set proxy` / `https-proxy`, and `cafile` for TLS inspection. If the whole room is blocked, use the fallback demo below |
| Browser shows `This site can't be reached` | App not running on 5198 | Start it in T2 |
| Claude greps the Razor files instead of clicking | It took the cheaper path | `Use the Playwright tools. Check the running app, not the code.` |
| `microsoft-learn` failed | learn.microsoft.com blocked | Skip Part A |

The setup check's last line (`NuGet and npm reachable`) runs `npm ping`, so a blocked registry
should show up in pre-work, not in the room.

## Fallback: 60-second podium demo (npm blocked for the room)

Part A still runs for everyone. For Part B, run it once on the podium laptop. Add the server and
start it once during setup, so it is already connected, and keep a phone hotspot ready in case the
venue network blocks registry.npmjs.org for the podium too.

1. T2: `dotnet run --project AssetDesk --urls http://localhost:5198` on the `reference-build` branch.
2. `/mcp` on the projector: `playwright` connected, next to `microsoft-learn`.
3. Paste the Part B prompt. Narrate the first three calls (`browser_navigate`, `browser_snapshot`,
   `browser_click`) while the window moves, then let it finish off-screen if the room is short on
   time and show the pass/fail table.
4. `/mcp` → `playwright` → View tools → `browser_click`: read the description aloud.

Attendees still meet all three "Done when" items: they saw the browser run, they did Part A with a
citation, and they can name `mcp__playwright__browser_click`.
