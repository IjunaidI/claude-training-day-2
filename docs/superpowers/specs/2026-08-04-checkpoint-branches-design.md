# Checkpoint branches for the spec-driven-development session

**Date:** 2026-08-04
**Status:** approved, ready for planning

## Goal

Turn the single-exercise repo into four checkpoints a class can walk through in one sitting, each
demonstrating a higher level of agent workflow. Students see the same spec and the same one-line
prompt produce progressively better code as skills are layered on, and they watch a code review
confirm it — the finding count falls at every rung.

The teaching claim being demonstrated: **a spec alone under-determines the build; skills are what
close the gap, and a review makes the gap measurable.**

## Design principle

**One spec, four skill configurations.** `SPEC.md` is byte-identical on every checkpoint branch.
`.claude/` is the only variable. That is what makes the four results comparable rather than merely
different — students cannot attribute the improvement to a better spec, because the spec never
changed.

## Branch topology

```
main ═══ Checkpoint 1        SPEC.md (thinned)  CHECKPOINTS.md  README.md
  │                          INSTRUCTIONS-MACOS.md  INSTRUCTIONS-WINDOWS.md
  │                          .claude/ (review kit only)
  │
  ├── checkpoint-1           same content as main — exists so `git checkout checkpoint-N`
  │                          works uniformly for all four
  ├── checkpoint-2           + .claude/skills/api-boundary/
  ├── checkpoint-3           = checkpoint-2 + two pre-built reference apps
  ├── checkpoint-4           = checkpoint-2 + .claude/skills/csharp-quality/
  │                                        + .claude/skills/blazor-component-hygiene/
  │                          NO pre-built app — see checkpoint-4-result
  ├── checkpoint-4-result    = checkpoint-4 + the clean reference app. Kept off checkpoint-4
  │                            on purpose; see "Checkpoint 4" below
  │
  ├── dotnet-demo            UNCHANGED — full 600-line spec + finished app. Fallback and the
  │                          canonical reference copy of the unthinned spec.
  ├── nodejs-demo            UNCHANGED — archive of the pre-.NET attempt.
  └── checkpoints-setup      this design doc and the work that produces the above. Never
                             merged into a student-facing branch.
```

`docs/superpowers/specs/` must not appear on any checkpoint branch. Students clone those; internal
design docs would be noise at best and spoilers at worst.

## What each checkpoint teaches

| # | Student does | Sees |
|---|---|---|
| 1 | Read the spec, run the prompt, review the result | A spec-only build. Architecture improvised, quality unconstrained. Review reports ~10 findings |
| 2 | Add the `api-boundary` skill, same prompt, review | Boundary findings gone. Quality findings remain. ~5 findings |
| 3 | Review two pre-built apps instead of building | The rung-1 vs rung-2 contrast, side by side, with no build to go wrong |
| 4 | Add the two quality skills, same prompt, review | ~0 findings. The skill stack closed both gaps. `checkpoint-4-result` holds the reference version for anyone whose build ran long |

## Spec thinning

Eight surgical edits to `SPEC.md`. Nothing in the data model, screens, visual design, or seed data is
touched — those are what hold the four builds comparable. Line numbers are against the current
`main` (`SPEC.md` as of e52c313).

| Line(s) | Current | Change | Rationale |
|---|---|---|---|
| 70 | `Home.razor    # the only page — owns all state` | Trim comment to `# the only page` | Consistency with removing the Page state row below |
| 146 | `Data access \| AssetRepository only. Registered as a singleton. No component contains SQL` | Trim to `Data access \| AssetRepository, registered as a singleton` | Drops the ruling on who may call the repository; keeps the repository itself, so the app stays structurally comparable |
| 151 | `Page state \| Home.razor holds AppState and the active tab in private fields…` | Remove row | State ownership is a skill concern |
| 152 | `Refresh \| After any mutation, Home.razor re-reads repo.GetState()…` | Remove row | Names the repository as the UI's data source |
| 156 | `**Components call the repository directly.** There is no HTTP between the UI and the data.…` | Remove paragraph | States the exact opposite of the `api-boundary` skill |
| 158 | `**The six API endpoints exist anyway.** … and the UI never calls them.…` | Remove paragraph | Same conflict |
| 332 | `Mapped in Program.cs. The UI does not use them.…` | Replace with: `Mapped in Program.cs. They must work from the command line — M0 and M1 are verified with curl before any component exists, and section 9's invariant checks go through them.` | Keeps the endpoints mandatory and curl-verifiable without ruling on whether the UI uses them |
| 618 | Checklist item: no `!` null-forgiving operator and no suppressed warnings anywhere | Remove checklist item | Owned by `csharp-quality`; leaving it means checkpoint 1 already passes and the skill demonstrates nothing |

**Deliberately kept:**

- Line 154, the synchronous-data-access paragraph. No skill covers it and removing it invites a
  class of `StateHasChanged`/`await` failures that would derail a live session.
- Line 81, `AssetRepository.cs  # every SQL statement in the app`. The repository always exists;
  the boundary question is purely whether the UI calls it or the API. Keeping this narrows the
  variable and lowers the risk of a wildly divergent build.
- Line 312, "The UI shows the message verbatim and never writes its own copy." This makes the
  swallowed-exception issue a **spec** violation as well as a skill violation — the review should
  catch spec bugs, not only skill bugs.
- Section 2 (non-goals) in full. It is what keeps scope comparable across the four builds.

## The skills

All ship as `.claude/skills/<name>/SKILL.md` with `name` and `description` frontmatter, matching the
convention every skill in the local plugin cache uses.

### `api-boundary` — checkpoints 2, 3, 4

- No component injects or references `AssetRepository`. The repository is reachable only from
  `Program.cs` endpoint handlers.
- Every UI read and write goes through `/api/*`.
- One typed `HttpClient` registered in DI. Never `new HttpClient()` at a call site.
- Every call passes a `CancellationToken` and checks the response status before deserialising.
- DTOs at the boundary. Domain records from `Data/Models.cs` do not appear in component signatures.
- Error responses (`{ "error": … }`, 400) are translated once, in the client, into a message the UI
  renders verbatim.

### `csharp-quality` — checkpoint 4

- No swallowed exceptions. Every `catch` either surfaces the message verbatim per SPEC.md §4.6 or
  rethrows. No empty catch blocks.
- No `!` null-forgiving operator, no `#pragma warning disable`, no `<NoWarn>`. Fix the cause.
- Validation at the boundary: every endpoint validates its body server-side. UI-side validation is
  an addition, never the only line of defence.

### `blazor-component-hygiene` — checkpoint 4

- One component, one job. The components `SPEC.md` §3.2 names stay separate; collapsing two into one
  file is a violation even if the result is short. Numeric tripwire at 250 lines — `AssetsTable.razor`
  is legitimately ~246, so a 150-line cap would have made the *clean* app violate its own rule.
- No business logic in markup.
- DTOs declared once and shared, never re-declared inline per component.
- `Status`, `Category`, and `Condition` come from the enum, never string literals.

## The review kit — every branch

Two files, shipped on `main` and every `checkpoint-*` branch, so the finding count is measurable at
every rung.

```
.claude/
  agents/assetdesk-reviewer.md     read-only tools, isolated context
  skills/review-build/SKILL.md     dispatches the agent
```

**Why an agent and not skill frontmatter.** Every skill in the local plugin cache carries only
`name:` and `description:`; no `context:` or `fork:` key exists in any of them. Isolation, model, and
tool restrictions are agent-definition frontmatter. A skill obtains a forked context by dispatching
such an agent. If a future Claude Code exposes a skill-level switch, this becomes a one-line change.

**Why the context must be forked.** A reviewer that shares the conversation that wrote the code is
reviewing its own reasoning. The finding count would drop for the wrong reason, and the whole
demonstration would be invalid. The reviewer reads the code cold.

**What the reviewer checks:** `SPEC.md` plus whichever skills are present on the branch. So the same
command scores checkpoint 1's output against the spec alone, and checkpoint 4's against the spec plus
three skills — the rules tighten as the branch does.

**Output shape:** findings grouped by rule, with a count per category and a total. Live builds are
nondeterministic, so the visible signal must be which *categories* survive, not a fragile exact
number.

## Checkpoint 3 — two reference apps

Checkpoint 3 requires no build, which is what makes it the reliable centrepiece of the session.

```
checkpoint-3/
  AssetDesk/                       built WITH api-boundary, quality skills off
  reference/AssetDesk-noskills/    built with NO skills
```

Both are curated from `dotnet-demo`'s working app rather than generated live, so the review surfaces
the same findings every time the session is taught.

`reference/AssetDesk-noskills/` is close to `dotnet-demo` as it already stands — verified: its
components carry `[Inject] private AssetRepository Repo`, and `HttpClient` appears nowhere in the
app. It needs the five quality issues added and little else.

`AssetDesk/` is the real work: the same app restructured so the UI talks to `/api/*` through a typed
client, with the same five quality issues left in.

Every documented command targets a project path explicitly (`dotnet build AssetDesk`), so the sibling
project causes no conflict on the happy path. A bare `dotnet build` at the root will fail with
`MSB1011` because two projects are now in the tree — hence the new troubleshooting row. Both apps must
be independently buildable and runnable when given their own path.

### The planted issues

Five quality issues, **identical in both apps**. That is the load-bearing detail: it proves the
`api-boundary` skill fixed the architecture and nothing else, which is what makes rung 2
unambiguous.

| # | Issue | Caught by |
|---|---|---|
| Q1 | `catch (Exception) { }` around a mutation — the error banner renders blank | `csharp-quality` (and SPEC.md §4.6) |
| Q2 | `!` on a deserialize result, plus `<NoWarn>` hiding a genuine warning | `csharp-quality` |
| Q3 | `POST /api/assets` trusts its body; tag/cost/quantity validated UI-side only | `csharp-quality` (and SPEC.md §4.6 guard method) |
| Q4 | An oversized component doing fetch + state + render, with DTOs re-declared inline | `blazor-component-hygiene` |
| Q5 | `Status` compared as a string literal instead of the `Status` enum | `blazor-component-hygiene` |

Q1 is deliberately **visible**: students click through it and watch the banner come up empty rather
than only reading about it in a report.

Five boundary issues, present **only** in `reference/AssetDesk-noskills/`:

| # | Issue | Caught by |
|---|---|---|
| B1 | Components inject `AssetRepository` — the UI is bound to the data layer | `api-boundary` |
| B2 | No HTTP layer at all, so the six `/api/*` endpoints are dead code that can drift from UI behaviour | `api-boundary` |
| B3 | Domain records from `Data/Models.cs` leak straight into component parameters | `api-boundary` |
| B4 | Mutation-then-refresh logic copy-pasted into each component instead of one client | `api-boundary` |
| B5 | Repository exceptions surface as raw .NET types with no translation layer | `api-boundary` |

Both apps must build with **0 warnings, 0 errors** and run correctly. Every planted issue is a
quality or architecture defect, not breakage — except Q1's visible-but-contained banner bug.

### Expected review results

| Reviewed | Skills at build time | Boundary | Quality | Total |
|---|---|---|---|---|
| `reference/AssetDesk-noskills/` | none | 5 | 5 | ~10 |
| `AssetDesk/` on checkpoint-3 | `api-boundary` | 0 | 5 | ~5 |
| `AssetDesk/` on checkpoint-4-result | all three | 0 | 0–1 | ~0 |

## Checkpoint 4 — clean reference app on its own branch

The clean app is curated to satisfy all three skills. It serves as the fallback for a student whose
build ran long, and as the "~0 findings" proof if nobody's live build finishes in time.

**It must not ship on `checkpoint-4` itself.** Checkpoint 4 is a live build into `AssetDesk/`, and an
agent that finds a finished implementation anywhere in the repo will read it and reproduce it. The
exercise would be measuring copying, not skills. Hence a separate `checkpoint-4-result` branch,
checked out only after the student's own build is done or abandoned.

The same hazard does not apply to `checkpoint-3`, which involves no build at all, so its two
reference apps can sit in the tree safely. And because `AssetDesk/` on checkpoint-3 *is* the
checkpoint-2 output, checkpoint-3 doubles as checkpoint 2's fallback — no separate
`checkpoint-2-result` branch is needed.

## Switching checkpoints

Students build on a work branch per checkpoint, so checkpoint branches stay pristine and no build is
ever lost:

```bash
git switch -c work/cp1 checkpoint-1     # build here
git add -A && git commit -m "cp1 build" # makes the next switch clean
git switch -c work/cp2 checkpoint-2     # on to the next
```

**Why committing is the mechanism.** Untracked, non-ignored files block `git checkout` — already
documented at INSTRUCTIONS-MACOS.md §8. Once committed they are tracked on the student's own branch,
so the switch is clean, no `rm -rf` is needed, the build stays recoverable, and `git branch` becomes
a record of the session.

Two follow-ups the runbook must carry, both of which will otherwise bite live:

1. The gitignored database survives a branch switch. It must be deleted between checkpoints or the
   seed skips and the first verification prints the wrong count. This is the one OS-specific command
   in the runbook.
2. **`/exit` and relaunch `claude` after every switch.** Skills register at session startup only;
   `/clear` is not enough. Already documented at INSTRUCTIONS-MACOS.md §8, and it becomes critical
   here because every checkpoint changes `.claude/`.

## Documentation changes

| File | Change |
|---|---|
| `CHECKPOINTS.md` | **New**, on every branch. What each checkpoint is, the exact prompt, the switch ritual, what to look for in each review. OS-neutral git; the one OS-specific delete shown as a two-row table |
| `INSTRUCTIONS-MACOS.md` | §6's branch table becomes the checkpoint table, pointing at `CHECKPOINTS.md`. New troubleshooting rows: skills not registering after a switch; checkout blocked by an uncommitted build; `MSB1011` from a bare `dotnet build` on checkpoint-3, which now holds two projects and needs an explicit project path |
| `INSTRUCTIONS-WINDOWS.md` | Same, in PowerShell |
| `README.md` | Re-leads with the four checkpoints instead of the single exercise. The "~600 lines" description of `SPEC.md` and the `dotnet-demo` fallback note both need updating |

## Definition of done

- [ ] `SPEC.md` byte-identical across `main`, `checkpoint-1`, `checkpoint-2`, `checkpoint-3`,
      `checkpoint-4`, `checkpoint-4-result`
- [ ] `git rev-parse main` equals `git rev-parse checkpoint-1` — they are the same content, so any
      drift between them is a bug
- [ ] `dotnet-demo` and `nodejs-demo` unchanged — same commit SHAs as before this work
- [ ] No checkpoint branch contains `docs/superpowers/`
- [ ] `checkpoint-4` contains no `AssetDesk/` and no `reference/` app — nothing for a live build to copy
- [ ] All three reference apps build with 0 warnings, 0 errors and run
- [ ] `/api/health` and `/api/state` answer correctly on all three reference apps (5 employees, 12 assets)
- [ ] The review reports ~10 / ~5 / ~0 findings on the three reference apps, and every planted issue
      in the tables above appears in the right bucket
- [ ] The `checkpoint-4-result` app violates none of the three skills
- [ ] The switch ritual works end to end: build on `work/cp1`, commit, switch to `checkpoint-2`
      without `rm -rf`, confirm the new skill registers after relaunch
- [ ] Nothing pushed to GitHub without explicit authorization

## Open items

- **Pushing.** All six student-facing branches — `main`, `checkpoint-1` through `checkpoint-4`, and
  `checkpoint-4-result` — must reach GitHub before the session. Deferred until explicitly authorized.
- **Live validation of checkpoint 4.** Running a real checkpoint-4 build to confirm the three skills
  actually produce the clean result is the only way to know the skills work under the real prompt.
  Recommended before teaching; not a blocker for building the branches.
