# AssetDesk lab

Training repository for the Claude Code Day 2 workshop. The app in `AssetDesk/` is built from `SPEC.md` during the labs.

## The contract
- `SPEC.md` is the contract. Never edit it. Where it is ambiguous, its own §10 says what to do.
- Feature specs live in `specs/`, one file per feature, written from `lab/templates/feature-spec.md`. A feature spec amends `SPEC.md` only where it says so.

## Build, run, test
- Build: `cd AssetDesk && dotnet build` (0 warnings, 0 errors)
- Run: `cd AssetDesk && dotnet run --urls http://localhost:5198`
- Check the API: `curl -s http://localhost:5198/api/state`
- Test: `dotnet test AssetDesk.Tests`, once a feature spec has added the test project. There is no solution file, so always name the project.
- Reset the data: stop the app, delete `assetdesk.db` with its `-wal` and `-shm` files, restart.

## Layout
- `AssetDesk/` — the app. Everything the build writes goes here.
- `specs/` — feature specs.
- `lab/` — lab material (templates, skills to copy in). Not part of the AssetDesk build. Do not reference it from `AssetDesk/`.
- `.claude/` — the reviewer agent, skills, and permissions for this repo.

## Working here
- Commit after each milestone, one commit per milestone, with the milestone in the message (`M3: assets table`).
- Never push. The person at the keyboard pushes.
