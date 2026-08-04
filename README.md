# spec-driven-demo — AssetDesk

One session on spec-driven development with Claude Code. You start with a specification and an
otherwise empty repository, and finish with a running .NET 10 Blazor app that nobody typed by hand.

## What is checked in

| File | What it is |
|---|---|
| [SPEC.md](SPEC.md) | The only source of truth. describing **AssetDesk**, an IT asset-management tool: data model, invariants, screens, visual design, build order, definition of done. |
| [CHECKPOINTS.md](CHECKPOINTS.md) | The four checkpoints, the one prompt they all share, and how to switch between them. **Read this first.** |
| [INSTRUCTIONS-MACOS.md](INSTRUCTIONS-MACOS.md) | Install, build, run, verify, reset — macOS. |
| [INSTRUCTIONS-WINDOWS.md](INSTRUCTIONS-WINDOWS.md) | The same, in PowerShell. |

Nothing else. No scaffold, no project file, no lockfile — the agent runs `dotnet new` itself.

## Before the session

Open the instructions file for your machine and work through **§1 Install**. Budget 10 minutes, and
do it beforehand — the prerequisites include a .NET SDK and a first NuGet restore, both of which want
network. `claude --version` and `dotnet --version` both answering is the bar.

Claude Code needs a **Pro, Max, Team, or Enterprise** account. The free Claude.ai plan does not
include it.

## How the session runs

The session runs as four checkpoints. Same spec, same one-line prompt, a different set of skills
loaded each time — so any difference in the result is the skills' doing and nothing else.

| # | Branch | Adds | You see |
|---|---|---|---|
| 1 | `main` | nothing but the reviewer | What a detailed spec still leaves undecided |
| 2 | `checkpoint-2` | `api-boundary` | One skill settling the architecture |
| 3 | `checkpoint-3` | two finished apps | The two reviews side by side, no build needed |
| 4 | `checkpoint-4` | two quality skills | A review with almost nothing left to say |

[CHECKPOINTS.md](CHECKPOINTS.md) is the runbook. Work through it in order.

The app is not the point. The point is which of your own specs would have survived being executed
literally, and how much of the gap a few pages of skills can close.

## Your OS is not something you manage

Everything inside the build belongs to the agent: scaffolding, package restore, file paths, the
`.gitignore`. It reads the platform from its own environment and issues the matching commands, which
is why nothing in SPEC.md is macOS- or Windows-specific and why you never translate for it. The only
OS-specific commands are the ones you type yourself, and those are the two instructions files.

## If your build goes sideways

Each checkpoint has a finished reference build to fall back on — `checkpoint-3` for checkpoints 1 and
2, `checkpoint-4-result` for checkpoint 4. Commit your own work first (see
[CHECKPOINTS.md](CHECKPOINTS.md#switching-checkpoints)) so nothing is lost and the switch is clean.

`dotnet-demo` still holds the original single-exercise build against the unthinned spec.
