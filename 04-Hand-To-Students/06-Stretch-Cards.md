# Stretch Cards

For anyone who finishes a lab early. Pick the card for the lab you just finished. Each card is 5–10 minutes. Don't commit stretch work to your workshop branches unless it passes the Check.

---

### After Lab 1 — Prove the contract is protected
The build is running in T1. In T2, try to change the spec with edits pre-approved, so the deny rule is the only thing in the way:
`claude -p "Append the line 'Draft' to the end of SPEC.md." --permission-mode acceptEdits`
**Check:** Claude reports it could not edit the file, and `git diff --stat SPEC.md` prints nothing.

### After Lab 2 — A `/spec-review` command for the team
In T3, create `.claude/commands/spec-review.md` with `description`, `argument-hint: "[path to spec]"` and `allowed-tools: Read, Grep, Glob`. Its body: use a subagent with fresh context to read SPEC.md and `$ARGUMENTS`, list every decision still open with the section it belongs in, and change nothing. Restart Claude (commands register at startup). Don't call it `/review`: that name is taken.
**Check:** `/spec-review specs/asset-history.md` lists open decisions and `git status` shows no edits.

### After Lab 3 — One more rulebook
Copy `lab/skills/api-boundary` into `.claude/skills/`, restart Claude, and run `/review-build`. Don't fix anything yet; this is a large refactor.
**Check:** the BOUNDARY count went from 0 to a real number while the code did not change, and you can say which rule produced the largest group.

### After Lab 4 — No spec? Extract one
Open your Day 1 `OrderService` clone. In plan mode: `Read src/OrderService/Payments. Write specs/payments-as-is.md describing what the code does today: behaviour, errors, and invariants. Mark every statement you inferred but could not confirm from code as ASSUMED.`
**Check:** the file has at least one ASSUMED line, and you confirmed or refuted one of them by reading the code yourself.

### After Lab 4 — Build after every edit
Stop the running app first (Windows locks the build output). Add a PostToolUse hook to `.claude/settings.local.json` (personal, not committed):
```json
{ "hooks": { "PostToolUse": [ { "matcher": "Edit|Write",
  "hooks": [ { "type": "command", "command": "dotnet build AssetDesk -nologo -v q 1>&2 || exit 2" } ] } ] } }
```
Restart Claude and check `/hooks`. Ask Claude to rename one property on a DTO in one file only.
**Check:** Claude's reply quotes the build error the hook sent back, and it fixes the other callers without being asked.

### After Lab 5 — The same review, headless, on your laptop
In T2, pipe your PR's diff into Claude in print mode. `<number>` is your PR number:
`gh pr diff <number> | claude -p "Review this pull request diff against SPEC.md, the specs/*.md files it adds, and every .claude/skills/*/SKILL.md except review-build. Every finding: the clause it breaks, file:line, one sentence. End with a count block: SPEC, QUALITY, OTHER, TOTAL." --allowedTools "Read,Grep,Glob" --max-turns 30`
Don't edit the workflow file to experiment: a PR that changes it fails the Claude GitHub App's check.
**Check:** the command prints findings that cite clauses, ends with a count block, and exits. Compare it with the CI summary on your PR: which findings match, and why might the others differ?

### After Lab 6 — Playwright for the whole team, and a screenshot of every tab
Share the browser server at project scope. In T2 (Claude closed): `claude mcp remove playwright`, then `claude mcp add --scope project playwright -- npx @playwright/mcp@latest` (Windows: `... -- cmd /c npx @playwright/mcp@latest`). Start `claude`, read the approval prompt for the project server, approve it. With the app running: `Use the Playwright browser to open http://localhost:5198 and take a full-page screenshot of each sidebar tab. Tell me where you saved them. Do not change any code.`
**Check:** `.mcp.json` exists at the repo root with a `playwright` entry, you were asked to approve it, and there is one screenshot per tab under `.playwright-mcp/`. Before a team commits that `.mcp.json`: the `cmd /c` form only works on Windows, so a mixed team has to choose. Don't commit it to your workshop branches.

### After Lab 6 — Build your own MCP server (outside the repo)
Your own API as tools, with no code of your own. In a new folder next to the repo, plan mode: `Scaffold a C# MCP server in this folder with the ModelContextProtocol NuGet package, stdio transport, and all logging on stderr. One tool, list_assets, that calls GET http://localhost:5198/api/state and filters by optional category and status. Write a description that says what it returns and when to use it. On a 400, return the API's error message, not a generic one.` Then, from the repo root: `claude mcp add assetdesk -- dotnet run --project <path to the new folder>`, a new session, `/mcp`.
**Check:** `/mcp` lists `assetdesk` with its tool, and `Which laptops are in stock?` calls `mcp__assetdesk__list_assets` and answers AST-1003. Nothing was added to the training repo.

### After Lab 6 — Package the rules as a plugin
Outside the repo, create `../assetdesk-plugin/` with `.claude-plugin/plugin.json` (`{"name": "assetdesk", "version": "0.1.0", "description": "AssetDesk review rules"}`), `skills/` holding copies of the two quality skills, and `agents/assetdesk-reviewer.md`. Start a session with `claude --plugin-dir ../assetdesk-plugin`.
**Check:** the skills appear with the plugin's prefix (`assetdesk:csharp-quality`). A team shares the same folder through a marketplace with `/plugin marketplace add`.
