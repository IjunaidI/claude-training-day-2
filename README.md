# spec-driven-demo — AssetDesk

One session on spec-driven development with Claude Code. You start with a specification and an
otherwise empty repository, and finish with a running .NET 10 Blazor app that nobody typed by hand.

## What is checked in

| File | What it is |
|---|---|
| [SPEC.md](SPEC.md) | The only source of truth. ~600 lines describing **AssetDesk**, an IT asset-management tool: data model, invariants, screens, visual design, build order, definition of done. |
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

1. `claude`, then `/model sonnet`, then one prompt: **Read SPEC.md and build it.**
2. It works through eight milestones (§8 of the spec), data layer first, UI last. Read the diffs as
   they land — that is the actual exercise.
3. Run it, click through the acceptance criteria in §5, then go looking for where the spec let it
   improvise.

The app is not the point. The point is what a specification has to pin down before an agent can
execute it without you in the loop, and which of your own specs would have survived that.

## Your OS is not something you manage

Everything inside the build belongs to the agent: scaffolding, package restore, file paths, the
`.gitignore`. It reads the platform from its own environment and issues the matching commands, which
is why nothing in SPEC.md is macOS- or Windows-specific and why you never translate for it. The only
OS-specific commands are the ones you type yourself, and those are the two instructions files.

## If your build goes sideways

`git checkout dotnet-demo` has the finished app. Read the fallback notes in your instructions file
first — a partly-built `AssetDesk/` will block the checkout, and there is one command that fixes it.
