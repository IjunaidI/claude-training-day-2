# AssetDesk — Claude Code Day 2 lab repository

The repository for every lab of the Claude Code Day 2 workshop. It holds a spec, not an app: during
the day, Claude Code builds `AssetDesk/` (a .NET 10 Blazor IT-asset tracker) from `SPEC.md`, you
write a feature spec for it, review it, extend it test-first, have Claude review your pull request
in CI, and give Claude new tools over MCP: Microsoft Learn docs, and a browser that checks the
running app.

## Before the workshop

1. Install Git, the .NET 10 SDK, Node.js LTS, and Claude Code (see the pre-work email, or §1 of
   the instructions file for your OS).
2. Clone this repository and open a terminal in its root folder.
3. Sign in to Claude Code: type `claude`, complete the browser sign-in, then type `/exit`.
4. Run the setup check:
   - Windows: `powershell -ExecutionPolicy Bypass -File scripts/check-setup.ps1`
   - macOS / Linux: `bash scripts/check-setup.sh`

All six checks must show `[ OK ]`. The last one proves your network reaches NuGet (packages for the
build) and npm (the Lab 6 browser server).

## During the workshop

Follow `lab/LAB-GUIDE.md`. Each lab tells you exactly what to type and how to know you are done.
`INSTRUCTIONS-MACOS.md` and `INSTRUCTIONS-WINDOWS.md` cover running, checking, and resetting the app.

Do not edit `SPEC.md`. Claude cannot: `.claude/settings.json` denies it.

## Branches

| Branch | What it is |
|---|---|
| `main` | The starting point: spec, tooling, no app. Tagged `start`. |
| `reference-build` | `main` plus a finished `AssetDesk/`. The fallback if your build falls behind. Branch from it; never commit to it. |

Your own work goes on your own branches: `build/<name>` and `feature/<name>-asset-history`.

## What is where

| Path | What it is |
|---|---|
| `SPEC.md` | The contract for AssetDesk. The build is judged against it |
| `CLAUDE.md` | Project memory: loads into every Claude Code session in this repo |
| `specs/` | Feature specs you write (Lab 2) |
| `lab/templates/feature-spec.md` | The template for a feature spec |
| `lab/skills/` | Review rules you copy into `.claude/skills/` in Lab 3 |
| `.claude/agents/assetdesk-reviewer.md` | The cold-context reviewer subagent |
| `.claude/skills/review-build/` | The `/review-build` skill that dispatches it |
| `.claude/settings.json` | Permissions: what Claude may run, and what it may never edit |
| `.github/workflows/claude-review.yml` | Claude reviews every pull request (Lab 5) |
| `scripts/` | The setup check |
