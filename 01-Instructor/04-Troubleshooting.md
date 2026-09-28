# Troubleshooting — Day 2

Organised by symptom. Golden rule in the room: **if a fix takes more than one minute, pair the person with a neighbour and fix it at the next break.** Never debug one laptop on the projector. On Day 2 the second rule is: **a stalled build is never worth the afternoon.** Commit, then `git switch -c build/<name>-ref origin/reference-build`.

**First move for anything odd inside Claude Code:** `/doctor`. For the app, `INSTRUCTIONS-WINDOWS.md` and `INSTRUCTIONS-MACOS.md` §7 in the student repo have the same tables attendees can read themselves.

---

## 1. Install and sign-in (Lab 0)

| Symptom | Fix |
|---|---|
| `claude : The term 'claude' is not recognized` | Close the terminal and open a new one. If it persists, re-run `irm https://claude.ai/install.ps1 \| iex`. macOS: `echo 'export PATH="$HOME/.local/bin:$PATH"' >> ~/.zshrc && source ~/.zshrc`. |
| `'irm' is not recognized` | Command Prompt, not PowerShell. Open PowerShell (the prompt starts with `PS`). |
| `dotnet --version` shows 8 or 9, or `.NET 10 SDK installed` fails | Install the .NET 10 SDK and open a new terminal. |
| `[FAIL] Claude Code signed in` | Run `claude`, complete sign-in (or `/login`), `/exit`, re-run the check. If it says the organization has disabled access, the account's admin has turned off Claude Code or subscription access: that is an IT ticket, not a laptop fix. Pair the person until it is resolved. |
| "Your plan does not include Claude Code" | Free plan. Needs Pro, Max, Team, or Enterprise. |
| Surprise API bill risk | `ANTHROPIC_API_KEY` set in the environment makes Claude Code bill that API account. Remove it: `Remove-Item Env:ANTHROPIC_API_KEY` (macOS: `unset ANTHROPIC_API_KEY`) and check the shell profile. |
| The NuGet check fails, or restore fails with `NU1301` | NuGet is blocked. Ask IT to allow `api.nuget.org`, then `dotnet nuget locals all --clear` and retry. |
| The Node check fails, or `node` is not recognized | Install Node.js LTS (18 or later) and open a new terminal. Only Lab 6 Part B needs it; everything else works without it. |
| Claude Code on Windows cannot find Bash | Add `{ "env": { "CLAUDE_CODE_GIT_BASH_PATH": "C:\\Program Files\\Git\\bin\\bash.exe" } }` to `.claude/settings.local.json`. |
| Clone lives in OneDrive, Desktop or Documents on Windows | Move it to `C:\src`. OneDrive locks `bin\` and `obj\` files mid-build (`MSB3027`). |

## 2. The build (Lab 1)

| Symptom | Fix |
|---|---|
| Claude wrote files while "planning" | Plan mode was off. Esc, `/exit`, `git restore .`, `git clean -fd AssetDesk`, relaunch with `claude --permission-mode plan`. |
| The plan-approval menu has no "auto-accept edits" wording | Versions differ. Pick the option that approves the plan and auto-accepts edits. If only manual is offered, approve manually and press Shift+Tab to switch to accept edits. |
| Permission prompts for `mkdir`, `kill`, `lsof`, `sqlite3`, `sleep` | Expected: only `dotnet`, `curl` and a few `git` commands are pre-allowed. Approve anything that stays inside the repo. |
| Claude asks to delete `AssetDesk/data` (or `rm -r`, `Remove-Item -Recurse` on it) | **No.** On Windows and macOS `data/` and `Data/` are the same folder, so this deletes the data layer (SPEC.md §3.2). `rm -rf` is denied in `.claude/settings.json`; `rm -r` is not. Tell it: "Delete the three sqlite files by name." If it already happened: `git checkout -- AssetDesk/Data`. |
| Data layer missing from `git status`; `Data/*.cs` never committed | Someone put `data/` in a `.gitignore`. On a case-insensitive filesystem that also ignores `Data/`. Check: `git check-ignore -v AssetDesk/Data/Models.cs` prints the guilty line. Replace it with the three files by name (`assetdesk.db`, `assetdesk.db-shm`, `assetdesk.db-wal`), then `git add AssetDesk/Data` and commit. Do this **before** any branch switch: git overwrites ignored files on checkout. |
| T1 quiet for more than ten minutes | Look for a pending permission prompt first (scroll up). Otherwise press Esc and type `Continue with the next milestone. One line per milestone.` Twenty minutes quiet: fall back to the reference build. |
| It starts on `.razor` files before M1 is committed | Esc: "Stop. SPEC.md §8: finish and commit M1, verified with curl, before any component." |
| "You've reached your usage limit" | Note the reset time. Don't wait for it: commit, switch to `build/<name>-ref` at 12:15. Pro seats hit this most. |
| Claude says it cannot edit `SPEC.md` | Working as intended: `Edit(SPEC.md)` is denied. |
| It stops to ask a question | Answer with §10.6: "Pick the simplest reading, state the assumption in one line, and keep going." |
| No commits after milestones | Type into T1 while it works: "Commit after each milestone from now on." It reads the message at its next step. |
| Targets `net8.0`/`net9.0`, or creates `Startup.cs` / `_Host.cshtml` | SPEC.md §3.1 and §10.2. Esc and quote the clause. |
| `.csproj` gains a third package | SPEC.md §3.1: exactly two. Ask it to remove it. |
| `NU1903` NuGet audit warning breaks "zero warnings" | Known transitive advisory in `SQLitePCLRaw`. Suppress with `<NoWarn>$(NoWarn);NU1903</NoWarn>` and a comment saying which package and why (the `csharp-quality` skill allows exactly this one). |
| `MSB1003: Specify a project or solution file` | No solution file at the root. `dotnet build AssetDesk`, `dotnet test AssetDesk.Tests`. |
| Buttons and tabs do nothing; the build is clean | No interactive render mode. SPEC.md §3.3 item 1: `@rendermode="InteractiveServer"` on `HeadOutlet` and `Routes` in `App.razor`. |
| Search only filters when you click away | `@bind:event="oninput"` missing. SPEC.md §3.3 item 5. |
| A curl assign with a fake employee id succeeds | `Foreign Keys=True` is not in the connection string. SPEC.md §3.3 item 2. |
| `SQLite Error 1: no such table` on a fresh clone | The `data/` folder was not created. SPEC.md §3.3 item 3. |

## 3. Running and checking the app (12:15, Labs 4 and 6)

| Symptom | Fix |
|---|---|
| `address already in use` / `Failed to bind to address http://127.0.0.1:5198` | Something already listens on 5198, often the build agent's own `dotnet run`. Windows: `Get-NetTCPConnection -LocalPort 5198 -State Listen`, then `Stop-Process -Id <OwningProcess> -Force`. macOS: `lsof -nP -iTCP:5198 -sTCP:LISTEN`, then `kill <PID>`. |
| App starts on a random port or on HTTPS | They left out `--urls`. Always `dotnet run --project AssetDesk --urls http://localhost:5198`. |
| `/api/state` shows 13 or more assets | An old database survived. Reset (below). |
| Database reset | Stop the app. Windows: `Get-ChildItem AssetDesk -Recurse -Filter 'assetdesk.db*' \| Remove-Item -Force`. macOS: `find AssetDesk -name 'assetdesk.db*' -delete`. Never delete the folder. Never use a `[Dd]ata/assetdesk.db*` glob in zsh: when it matches nothing zsh aborts and nobody notices. |
| After switching to `build/<name>-ref`, the data looks wrong | The ignored database survives a branch switch. Reset it. |
| PowerShell: `curl -X POST ... -d '{...}'` fails or sends a mangled body | In Windows PowerShell 5.1, `curl` is `Invoke-WebRequest`, and quotes inside JSON get stripped even with `curl.exe`. Use the `Invoke-RestMethod` blocks in the lab guide, or `curl.exe -d "@body.json"` with the JSON in a file. |
| `Invoke-RestMethod` throws instead of printing the error | Expected on a 400. The lab guide wraps it in `try { } catch { $_.ErrorDetails.Message }`. |
| `{"error":"That asset no longer exists..."}` for a route with `AST-1003` in it | Routes take the GUID `id` from `/api/state`, not the tag. |
| "Assign AST-1004 in the UI" but there is no Assign button | Correct: SPEC.md §5.2 shows Assign only on in-stock rows. Use the two-tab check on AST-1003 (lab guide, 12:15 step 4). |
| The two-tab check shows an empty red box | The error is swallowed (`_error = ""`). On the reference build it is planted defect 1. Leave it; Lab 3 finds it. |
| `MSB3021` / `MSB3027`: *the process cannot access the file* (Windows) | Something holds `bin\`: the running app, `dotnet watch`, the build agent, or OneDrive. Stop it and rebuild. |
| `Failed to determine the https port for redirect` in the log | Expected with an http-only `--urls`. Not a warning. |
| 12:15: Claude starts fixing what its §9 walk finds | Esc: "Do not fix anything. Report only." `git restore .` if it already edited. |
| 12:15: Claude says an item passes but shows no output | "Show the command and its output for that item." A claim without output is not evidence. |
| 12:15: Claude marks the click-through items as not proven | Correct: it has no browser until Lab 6. The human does the two-tab spot-check. |
| Port 5198 busy after the §9 walk | Claude started the app and left it running. Ask it to stop the app it started, or stop the process as in the first row. |

## 4. Branches (Lab 1, Lab 2, Lab 4, fallback)

| Symptom | Fix |
|---|---|
| `fatal: a branch named 'build/<name>' already exists` | They ran it twice, or a teammate used the same name. `git switch build/<name>`, or pick `build/<name>2`. |
| Lab 2: T3 changed files other than `specs/asset-history.md` | `git status` in T2 shows them. `git restore <file>` for each; tell T3: "Only write specs/asset-history.md. Touch nothing else." |
| Lab 2: the build session committed `specs/asset-history.md` in a milestone commit | Fine. The spec has to be on the build branch for Lab 4 anyway. |
| Lab 2: T3's banner shows a different folder from T1 | T3 was started elsewhere. `/exit`, `cd` to the repository root, relaunch. Both sessions work in the same folder. |
| Someone made a worktree anyway (Day 1 habit) | Fine if the spec ends up committed on the build branch. Otherwise copy `specs/asset-history.md` into the main folder, commit it there, then `git worktree remove <path>`. |
| After switching to `build/<name>-ref`, the spec is gone | It was committed on `build/<name>`. `git checkout build/<name> -- specs/asset-history.md`, then commit it on `build/<name>-ref`. |
| Lab 4: `specs/asset-history.md` is missing or untracked on the feature branch | It was never committed on the build branch. It is still in the folder: commit it on the feature branch before the failing tests. |
| Someone ran `git switch reference-build` (no `-c`) | They are on the shared branch. If they committed nothing: `git switch -c build/<name>-ref`. If they committed: same command keeps their commits on the new branch; then `git branch -f reference-build origin/reference-build`. Nobody pushes `reference-build`. |
| `error: Your local changes ... would be overwritten by checkout` | Commit first: `git add -A`, `git commit -m "wip: my build"`, then switch. |
| `git push` asks for a password, or `403` | Credentials or access. Windows: Git Credential Manager opens a browser on the first push. macOS: `gh auth login` and answer Yes to "Authenticate Git". `403` with valid credentials: the invitation was not accepted, or the user is not in the Write team. |
| `Permission to ... denied` for Claude running `git push` | Working as intended: `Bash(git push *)` is denied. The human pushes from T2. |

## 5. Review and skills (Lab 3)

| Symptom | Fix |
|---|---|
| `/review-build` is not in the `/` menu | Claude was started outside the repository root, or on a branch without `.claude/skills/review-build/`. `cd` to the root and relaunch. |
| The main session reviews the code itself | "Use the review-build skill. Do not review it yourself." Or `@assetdesk-reviewer`. |
| QUALITY stays 0 after copying the skills | No restart (`/clear` is not enough: skills register at startup), or a nested copy: `.claude/skills/csharp-quality/csharp-quality/SKILL.md`. The file must be `.claude/skills/<name>/SKILL.md`. Fix the path, `/exit`, relaunch. |
| Claude refuses to copy the skills | Working as intended: `Edit(lab/**)` is denied, and `.claude/` always prompts. The human copies them in T2 (`Copy-Item -Recurse` / `cp -r`). |
| Counts differ from the neighbour's, or from slide 28 | Expected. A live review is not deterministic. What repeats is the direction: QUALITY rises in #2. (With the optional fix from the stretch card, it falls in a third review.) |
| A finding flags missing tests, interfaces, CI | The reviewer's rules say not to; ignore it. SPEC.md §2 forbids them. |
| The same defect reported once per file | One decision, one finding. Mention it; don't re-run. |
| After the optional fixes (stretch card), `dotnet build AssetDesk` shows a new warning | The fix removed a `NoWarn`/`!` and exposed a real null dereference. Ask Claude to fix the cause, not re-suppress it. |
| The reviewer on the reference build finds exactly the planted defects | That is the design (`Lab-Solutions/reference-build/PLANTED-DEFECTS.md`). |

## 6. Feature and tests (Lab 4)

From `Lab-Solutions/lab4/NOTES.md`, plus what we expect in the room.

| Symptom | Fix |
|---|---|
| `MSB1003` from `dotnet test` | No solution file. `dotnet test AssetDesk.Tests`. |
| Tests fail to compile against a build whose `AssetRepository` constructor is not `(string connectionString)` | The reference tests assume that constructor. Ask Claude to adapt the test fixture's one constructor line to the build. Never change the repository to fit the tests. |
| Tests compile-fail with `GetHistory`/`AssetEventType` missing | That is the red step. Good. |
| Claude wrote code and tests in one commit | The proof that tests can fail is gone. With time: "Split the last commit: tests first, then the implementation." Without: note it in the PR. |
| Claude tries to add "tests are allowed" to SPEC.md and is refused | Working as intended. "The amendment lives in the feature spec's §7." |
| Claude adds xunit to `AssetDesk.csproj` | Wrong project. Test packages go only in `AssetDesk.Tests.csproj`; the app keeps exactly two packages (SPEC.md §9). |
| Claude adds `Microsoft.AspNetCore.Mvc.Testing`, `FluentAssertions`, `Moq` | Habit. The feature spec's non-goals and §7 rule them out. |
| The seeded data changes after `dotnet test` | The tests open `AssetDesk/data/assetdesk.db`. Tests must use a temp file each. Reset the database by name. |
| AC-9 fails: one event left after a colliding create | Events written on a second connection, after `tx.Commit()`, or without `tx`. Every insert takes the mutation's `conn` and `tx`. |
| `dotnet test` takes minutes; `SQLite Error 5: 'database is locked'` | Same cause: an event written on a new connection while the mutation's transaction holds the write lock. |
| `CHECK constraint failed` from `SetStatus` | The insert runs even when the status did not change. Skip it when from = to. |
| `/api/assets/AST-1001/history` returns the unknown-id 400 | The route takes the GUID. Use the id lookup in the lab guide's Lab 4. |
| History is `[]` for seeded assets | Correct: no backfill. Return and re-assign first. |
| No tests after ten minutes | Hand them the reference tests (USB / shared folder), then: "The tests in AssetDesk.Tests are the acceptance criteria. Implement until dotnet test AssetDesk.Tests passes. Do not edit the tests." |
| They want `reference-implementation.patch` | Only on the reference build (it touches `Dtos.cs` and `AssetDeskClient.cs`), and only with under ten minutes left: `git apply --directory=AssetDesk <path>/reference-implementation.patch`. Never on an attendee's own build. |

## 7. The PR review in CI (Lab 5)

| Symptom | Fix |
|---|---|
| No **Claude review** run appears | The PR is a draft (drafts are skipped: click **Ready for review**), or Actions is disabled for the repo or org (**Settings → Actions → General**). |
| The PR's base is `main` | Edit the PR: base `build/<name>`. Otherwise the diff includes the whole build and the review takes ten minutes. |
| The attendee forked the repo and opened the PR from the fork | Fork PRs get no secrets, by design, so the run fails. Push the branch to the training repository itself and open the PR there. Never "fix" this with `pull_request_target`. |
| Run fails: `ANTHROPIC_API_KEY` missing, 401, or "credit balance" | The secret name, the key, or the Console workspace's spend limit. Fix once for the room. Then each attendee pushes an empty commit to rerun: `git commit --allow-empty -m "rerun review"`, `git push`. |
| "Resource not accessible by integration", or the OIDC token exchange fails | The Claude GitHub App is not installed on this repository, or the workflow lost `id-token: write`. Install the app (`Lab-Solutions/lab5/SETUP.md` §2). |
| "Workflow validation failed" | The PR changed `.github/workflows/claude-review.yml`. The app requires the workflow to match the default branch. Revert the file in the PR. |
| "the actor does not have write permissions" | The attendee is not in the Write team. Add them, then push an empty commit. |
| Green run, no comments | Read the run log: the review is there, so the comment step failed. Check `claude_args` still lists the four tools. |
| A run shows **Cancelled** | A newer push to the same PR superseded it (the `concurrency` block). Expected. |
| No QUALITY findings although the attendee did Lab 3 | The action reads `.claude/` from the **base** branch. The skills commit is not pushed to `build/<name>`. `git push` on the build branch, then push one commit to the PR. |
| The review found nothing | Plant the swallowed-message violation (lab guide, Lab 5 "If stuck"), push, read, revert. |
| The review answers nothing when they reply | Expected: the workflow runs on pushes, not comments. Replies are for the human approver. |
| All 20 runs queued, slow | The first run of the day is the slowest; runs start within a minute or two. If Actions is blocked outright, everyone uses the local fallback: `gh pr diff <n> \| claude -p "..." --allowedTools "Read,Grep,Glob" --max-turns 30` (the full prompt is in the lab guide). |
| The author cannot approve their own PR | Correct. A neighbour approves. |

## 8. MCP (Lab 6)

| Symptom | Fix |
|---|---|
| `/mcp` does not list the server just added | It was added while Claude was open. New servers load in a new session: `/exit`, `claude`. `/clear` is not enough. |
| `claude mcp list` shows nothing, but it worked in another folder | Local scope (the default) is per project. Run `claude mcp add` again from the repository root. |
| `microsoft-learn` shows failed | Check the URL was typed exactly, with `--transport http`: `https://learn.microsoft.com/api/mcp`. If it was, `learn.microsoft.com` is blocked: watch the podium demo and do Part B. |
| Claude answers the .NET question without calling a `mcp__microsoft-learn__…` tool | It answered from memory or used web search. "Use the microsoft-learn tools and cite the page." |
| `playwright` shows failed (Windows) | The `cmd /c` is missing. `claude mcp remove playwright`, then `claude mcp add playwright -- cmd /c npx @playwright/mcp@latest`. |
| `playwright` shows failed (any OS) | `node --version` is missing or below 18. Install Node.js LTS, open a new terminal, relaunch Claude. |
| `npx` hangs, `ETIMEDOUT`, `E403`, or a proxy error | `registry.npmjs.org` is blocked. That attendee does Part A and watches the podium's Part B. Fixing the proxy is an IT ticket, not a lab task. |
| `playwright` shows failed on its first start | `npx` downloads the package the first time, and it took longer than Claude waits. In a plain shell run `npx @playwright/mcp@latest --help` (Windows: `cmd /c npx @playwright/mcp@latest --help`) until it prints its help, then `/mcp` → `playwright` → **Reconnect**. |
| Claude reads the source code instead of using the browser | "Use the Playwright tools. Check the running app, not the code." |
| Claude retired or assigned something while checking | The data changed. Stop the app and reset the database by name (§3). |
| "Browser not found" / Chrome is not installed | Use Edge: `claude mcp remove playwright`, then add it again with `--browser msedge` at the end of the command. |
| The browser shows `ERR_CONNECTION_REFUSED` | The app is not running on 5198. T2: `dotnet run --project AssetDesk --urls http://localhost:5198`. |
| Claude edits code during the Part B check | Esc: "Do not change any code. Report only." `git restore .` if it already edited. |
| Claude reports "pass" without saying what it saw | "For each criterion, say what you saw on the page." Evidence, not a verdict. |
| `.playwright-mcp/` appears in `git status` | Playwright's output folder (snapshots, screenshots). The repo's `.gitignore` lists it; if it shows, they are on an old clone or edited `.gitignore`. Don't commit it. |
| Claude asks permission for every browser action | Expected in default mode. Read the first request for each tool, then pick the option that stops asking for that tool. |
| Someone added a server with `--scope project` | It wrote `.mcp.json`; the next session asks them to approve it. Fine for the lab; don't commit it to the workshop branches. |
| They rejected a project-server approval prompt | `claude mcp reset-project-choices`, then relaunch and approve. |
| A page tells Claude to do something ("ignore your instructions…") | Page content is data, not orders. Claude should report it, not act on it; permission prompts and deny rules still apply. |

## 9. Room-level failures

| Situation | Response |
|---|---|
| No internet or API unreachable | Use the screenshot backups (`05-Screenshot-Shot-List.md`), narrate each demo, run labs on the podium with the room directing. |
| Proxy blocks the API for attendees only | Podium on a phone hotspot; pair the room around any working machines. |
| Half the room behind at 12:15 | Everyone switches to `build/<name>-ref` at lunch. The afternoon is unaffected. |
| Actions blocked or the secret broken on the day | Local review fallback for everyone (§7, last rows). |
| Model output differs from the slides | Expected. Say it once at 10:00: the shape is what is being taught. |
