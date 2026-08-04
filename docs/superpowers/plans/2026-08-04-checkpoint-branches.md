# Checkpoint Branches Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn this repo into six branches that let a class walk four checkpoints in one sitting, watching a code review's finding count fall from ~10 to ~0 as skills are layered onto an unchanged spec.

**Architecture:** One spec, four skill configurations. `SPEC.md` stays byte-identical on every checkpoint branch; `.claude/` is the only variable. Branches are strictly additive — each is created from its parent and adds one layer. Three reference apps are curated from `dotnet-demo`'s working app so the review surfaces the same findings every time the session is taught.

**Tech Stack:** git branches, Markdown, Claude Code skills (`.claude/skills/*/SKILL.md`) and agents (`.claude/agents/*.md`), .NET 10 Blazor Web App with Dapper + Microsoft.Data.Sqlite.

**Source spec:** [`docs/superpowers/specs/2026-08-04-checkpoint-branches-design.md`](../specs/2026-08-04-checkpoint-branches-design.md)

## Global Constraints

- **`dotnet` is not on this shell's PATH.** It lives at `/usr/local/share/dotnet/dotnet`. Every task that builds must first run `export PATH="/usr/local/share/dotnet:$PATH"`. Verify with `dotnet --version` → `10.0.302`.
- **No test project, ever.** `SPEC.md` §2 line 30 reads `No tests, no CI, no Docker`. The red-green cycle in this plan is `grep`/`git diff`/`dotnet build`/`curl` assertions. Adding xunit would violate the spec the students are being taught to follow.
- **`SPEC.md` must end up byte-identical** across `main`, `checkpoint-1`, `checkpoint-2`, `checkpoint-3`, `checkpoint-4`, `checkpoint-4-result`. Verify with `git diff <a> <b> -- SPEC.md` returning empty.
- **Never modify `dotnet-demo` or `nodejs-demo`.** Record their SHAs in Task 0 and re-check in Task 10.
- **`docs/superpowers/` must never reach a student-facing branch.** It lives only on `checkpoints-setup`.
- **Never push.** Not one branch, not one tag, until the user explicitly authorizes it.
- Target `net10.0`. The only NuGet packages are `Dapper` and `Microsoft.Data.Sqlite`.
- Every `dotnet` command takes an explicit project path (`dotnet build AssetDesk`). A bare `dotnet build` now fails with `MSB1011` because checkpoint-3 holds two projects.
- Always pass `--urls http://localhost:5198` when running. The scaffold writes a random port into `launchSettings.json`.
- Every build must report `0 Warning(s), 0 Error(s)`.

## File Structure

**On `main` / `checkpoint-1` (checkpoint 1 content):**

| File | Responsibility |
|---|---|
| `SPEC.md` | Modified. The contract, thinned so it no longer rules on the UI↔data boundary or on warning suppression |
| `CHECKPOINTS.md` | New. The student runbook: what each checkpoint is, the prompt, the switch ritual |
| `README.md` | Modified. Re-leads with the four checkpoints |
| `INSTRUCTIONS-MACOS.md` | Modified. §6 becomes the checkpoint table; three new troubleshooting rows |
| `INSTRUCTIONS-WINDOWS.md` | Modified. Same, in PowerShell |
| `.claude/agents/assetdesk-reviewer.md` | New. Cold-context reviewer, read-only tools |
| `.claude/skills/review-build/SKILL.md` | New. Dispatches the reviewer; refuses to review inline |

**Added on `checkpoint-2`:** `.claude/skills/api-boundary/SKILL.md`

**Added on `checkpoint-3`:** `AssetDesk/` (API-boundary shape, 5 quality issues) and `reference/AssetDesk-noskills/` (repository-direct shape, same 5 quality issues + 5 boundary issues)

**Added on `checkpoint-4`:** `.claude/skills/csharp-quality/SKILL.md`, `.claude/skills/blazor-component-hygiene/SKILL.md`

**Added on `checkpoint-4-result`:** `AssetDesk/` satisfying all three skills

**New files inside the API-boundary apps:** `Data/Dtos.cs` (every boundary record, declared once), `Data/AssetDeskClient.cs` (the typed HTTP client and the only place a 400 becomes an exception)

---

### Task 0: Prepare the workspace

**Files:**
- Move away: `AssetDesk/` (untracked build residue — `obj/`, `bin/`, and a stale `assetdesk.db`; no hand-written source)
- Create: branch `checkpoint-1` from `main`

**Interfaces:**
- Produces: a clean tree on branch `checkpoint-1`; a record of the two frozen branch SHAs for Task 10 to verify against.

- [ ] **Step 1: Record the SHAs that must not change**

```bash
cd /Users/mohammadjunaid/Documents/GitHub/spec-driven-demo
git rev-parse dotnet-demo nodejs-demo | tee /private/tmp/claude-502/-Users-mohammadjunaid-Documents-GitHub-spec-driven-demo/94fa47de-21a3-4d41-8849-c498bb43f87e/scratchpad/frozen-shas.txt
```

Expected: two SHAs, `e52c313…` and `a4e46f5…`.

- [ ] **Step 2: Confirm the untracked directory holds no hand-written source**

```bash
find AssetDesk -name '*.csproj' -o -name '*.razor' | grep -v obj/
```

Expected: no output. If anything prints, stop — someone's build is in there. Report it and ask before continuing.

- [ ] **Step 3: Move it aside rather than delete it**

```bash
mv AssetDesk /private/tmp/claude-502/-Users-mohammadjunaid-Documents-GitHub-spec-driven-demo/94fa47de-21a3-4d41-8849-c498bb43f87e/scratchpad/AssetDesk-residue
git status --short
```

Expected: no output from `git status` — a genuinely clean tree.

- [ ] **Step 4: Create the checkpoint-1 branch from main**

```bash
git switch main && git switch -c checkpoint-1 && git log --oneline -1
```

Expected: `20e53ca Split instructions per OS, add a student README, make SPEC.md OS-neutral`.

No commit in this task.

---

### Task 1: Thin SPEC.md

**Files:**
- Modify: `SPEC.md:70`, `SPEC.md:146`, `SPEC.md:151-152`, `SPEC.md:156`, `SPEC.md:158`, `SPEC.md:332`, `SPEC.md:618`

**Interfaces:**
- Produces: the `SPEC.md` every later branch inherits byte-identically. After this task the spec no longer rules on who may call the repository, who owns page state, or whether warnings may be suppressed — those become skill territory.

- [ ] **Step 1: Assert the strings that must disappear are currently present**

```bash
grep -c 'Components call the repository directly' SPEC.md
grep -c 'The UI does not use them' SPEC.md
grep -c 'No component contains SQL' SPEC.md
grep -c 'null-forgiving operator' SPEC.md
```

Expected: `1` four times. This is the RED state — the strings are there and the skills would contradict them.

- [ ] **Step 2: Line 70 — drop the state-ownership comment**

Replace:
```
      Home.razor                  # the only page — owns all state
```
with:
```
      Home.razor                  # the only page
```

- [ ] **Step 3: Line 146 — drop the ruling on who may call the repository**

Replace:
```
| Data access | `AssetRepository` only. Registered as a singleton. No component contains SQL |
```
with:
```
| Data access | `AssetRepository`, registered as a singleton |
```

- [ ] **Step 4: Lines 151-152 — remove the Page state and Refresh rows entirely**

Delete both of these lines:
```
| Page state | `Home.razor` holds `AppState` and the active tab in private fields, passes them down as parameters |
| Refresh | After any mutation, `Home.razor` re-reads `repo.GetState()` and reassigns the field |
```

- [ ] **Step 5: Lines 156 and 158 — remove both paragraphs**

Delete the paragraph beginning `**Components call the repository directly.**` and the paragraph beginning `**The six API endpoints exist anyway.**`, including the blank line that follows each.

Leave line 154 (`**Synchronous data access is deliberate.**`) exactly as it is. It is deliberately kept — no skill covers it and removing it invites `StateHasChanged`/`await` failures mid-session.

- [ ] **Step 6: Line 332 — neutralise §4.7's opening**

Replace:
```
Mapped in `Program.cs`. The UI does not use them. They exist for command-line verification during M0 and M1 and for the invariant tests in section 9.
```
with:
```
Mapped in `Program.cs`. They must work from the command line — M0 and M1 are verified with `curl` before any component exists, and section 9's invariant checks go through them.
```

- [ ] **Step 7: Line 618 — remove the suppression checklist item**

Delete this line from §9:
```
- [ ] No `!` null-forgiving operator and no suppressed warnings anywhere
```

- [ ] **Step 8: Verify GREEN — the contradictions are gone and the keepers survived**

```bash
# must all print 0
grep -c 'Components call the repository directly' SPEC.md
grep -c 'The six API endpoints exist anyway' SPEC.md
grep -c 'The UI does not use them' SPEC.md
grep -c 'No component contains SQL' SPEC.md
grep -c 'null-forgiving operator' SPEC.md
grep -c 'owns all state' SPEC.md

# must all print 1 — deliberately kept
grep -c 'Synchronous data access is deliberate' SPEC.md
grep -c 'every SQL statement in the app' SPEC.md
grep -c 'shows the message verbatim' SPEC.md
grep -c 'No tests, no CI, no Docker' SPEC.md

# must print 1 — the replacement landed
grep -c 'They must work from the command line' SPEC.md
```

Note `grep -c` exits 1 when the count is 0, which is expected here; read the printed numbers, not the exit code.

- [ ] **Step 9: Confirm only SPEC.md changed, and by roughly the expected size**

```bash
git diff --stat
```

Expected: `SPEC.md` only, about 5 insertions and 10 deletions. If any other file appears, revert it.

- [ ] **Step 10: Commit**

```bash
git add SPEC.md
git commit -m "$(cat <<'EOF'
SPEC.md: hand the architecture and quality rulings to the skills

Removes the six passages that decided the UI-to-data boundary, page-state
ownership, and warning suppression. Those are now what the checkpoint skills
supply, so the spec staying silent is what makes the four builds differ.

Keeps the synchronous-Dapper paragraph, the repository's SQL ownership, and
the verbatim-error-message contract - no skill covers those and removing them
would destabilise a live session.
EOF
)"
```

---

### Task 2: The review kit

**Files:**
- Create: `.claude/agents/assetdesk-reviewer.md`
- Create: `.claude/skills/review-build/SKILL.md`

**Interfaces:**
- Consumes: `SPEC.md` as thinned in Task 1.
- Produces: the `/review-build` command and the `assetdesk-reviewer` agent, inherited by every later branch. The agent's count block (`BOUNDARY/QUALITY/SPEC/TOTAL`) is the contract Tasks 6, 7, and 9 verify against.

- [ ] **Step 1: Assert the command does not exist yet**

```bash
ls .claude 2>&1
```

Expected: `No such file or directory`. RED state.

- [ ] **Step 2: Create the reviewer agent**

Create `.claude/agents/assetdesk-reviewer.md`:

```markdown
---
name: assetdesk-reviewer
description: Reviews an AssetDesk build against SPEC.md and whichever skills this branch ships. Use when asked to review AssetDesk code, or when the review-build skill dispatches it.
tools: Read, Grep, Glob
---

You review one AssetDesk build. You did not write it. Read it cold.

## Inputs, in this order

1. `SPEC.md` — the contract the build was supposed to meet.
2. Every `SKILL.md` under `.claude/skills/`, except `review-build` itself. These are additional
   binding rules on this branch.

A branch with fewer skills has fewer rules. That is the design, not a gap for you to fill. Rules you
were not given are not violations — do not import standards from elsewhere, and do not flag a
practice as wrong merely because you would have written it differently.

## What to examine

The app directory you were given. If none was named, review `AssetDesk/`.

## Report format

Group findings by the rule they violate. Head each group with the rule's source (`SPEC.md §4.6`, or a
skill name). Then one line per finding:

    path/to/File.razor:42 — what is wrong, in one clause

End with exactly this block, so counts are comparable across checkpoints:

    BOUNDARY: <n>
    QUALITY:  <n>
    SPEC:     <n>
    TOTAL:    <n>

- BOUNDARY — violations of an `api-boundary` rule.
- QUALITY — violations of `csharp-quality` or `blazor-component-hygiene`.
- SPEC — violations of `SPEC.md` itself.

Each finding belongs to exactly one bucket. When a finding could sit in two, pick the most specific
rule and count it once. TOTAL is the sum.

## Rules for you

- Report only what you can point at with a file and a line. No speculation, no "consider whether".
- Do not fix anything. Do not edit files. You have no write tools and you should not ask for any.
- Do not pad the count. If a bucket is zero, print zero and say plainly that the build satisfies
  those rules.
- Do not report anything `SPEC.md` §2 forbids as missing. Absent tests, absent repository
  interfaces, absent service layers, absent CSS frameworks, and absent CI are all correct here.
- Do not report the same defect once per occurrence when it is one decision. Five components
  injecting the repository is one finding with five locations.
```

- [ ] **Step 3: Create the review-build skill**

Create `.claude/skills/review-build/SKILL.md`:

```markdown
---
name: review-build
description: Use when reviewing an AssetDesk build on this branch - dispatches a cold-context reviewer that scores the code against SPEC.md and this branch's skills and prints comparable finding counts.
---

# Review this build

Dispatch the `assetdesk-reviewer` agent. Do not review the code yourself.

## Why you must not review it yourself

If you wrote this code earlier in the conversation, reviewing it means reviewing your own reasoning,
and you will under-report. The point of the checkpoint exercise is that the finding counts are
comparable between branches — a biased reviewer destroys the comparison and the lesson with it. A
fresh agent reads the code cold.

## How

Dispatch one `assetdesk-reviewer` agent with this prompt, substituting the directory:

    Review the AssetDesk build in <directory>. Read SPEC.md and every SKILL.md under
    .claude/skills/ except review-build, then report findings in the required format.

Default `<directory>` to `AssetDesk/` when the user names none.

If the user asks for more than one directory — on checkpoint 3, `AssetDesk/` and
`reference/AssetDesk-noskills/` — dispatch **one agent per directory** so neither review contaminates
the other's context. Then print both count blocks side by side.

## Then

Print the agent's report verbatim, count block included. Do not summarise it away, do not soften a
finding, and do not offer to fix anything unless the user asks.
```

- [ ] **Step 4: Verify the frontmatter parses and the files are where Claude Code looks**

```bash
head -6 .claude/agents/assetdesk-reviewer.md
head -4 .claude/skills/review-build/SKILL.md
```

Expected: each file opens with `---`, then `name:` and `description:` on their own lines, then `---`. The agent additionally carries `tools: Read, Grep, Glob` and no write tool.

- [ ] **Step 5: Confirm the reviewer has no write capability**

```bash
grep -E '^tools:' .claude/agents/assetdesk-reviewer.md
```

Expected: `tools: Read, Grep, Glob`. If `Edit`, `Write`, or `Bash` appear, remove them — a reviewer that can edit will start fixing things and the finding count stops meaning anything.

- [ ] **Step 6: Commit**

```bash
git add .claude
git commit -m "$(cat <<'EOF'
Add the review kit: a cold-context reviewer and /review-build

The reviewer scores a build against SPEC.md plus whichever skills the branch
ships, so the same command tightens as the branches do. It runs as a separate
agent with read-only tools because a reviewer sharing the conversation that
wrote the code reviews its own reasoning and under-reports, which would make
the checkpoint-to-checkpoint counts incomparable.
EOF
)"
```

---

### Task 3: CHECKPOINTS.md

**Files:**
- Create: `CHECKPOINTS.md`

**Interfaces:**
- Consumes: the `/review-build` command from Task 2.
- Produces: the student runbook. Tasks 4's edits to `README.md` and both instructions files link to it, so the section headings settled here (`## Switching checkpoints`, `## The four checkpoints`) must not be renamed later.

- [ ] **Step 1: Assert it does not exist**

```bash
ls CHECKPOINTS.md 2>&1
```

Expected: `No such file or directory`.

- [ ] **Step 2: Write the file**

Create `CHECKPOINTS.md`:

```markdown
# CHECKPOINTS — four levels of the same build

Every checkpoint uses **the same spec and the same one-line prompt**. `SPEC.md` is byte-identical on
all four branches. The only thing that changes is what is in `.claude/`.

That is the whole experiment: if the results differ, the skills did it, because nothing else moved.

## The prompt

One line, at every checkpoint, nothing added:

```
Read SPEC.md and build it.
```

## The four checkpoints

| # | Branch | What is in `.claude/` | You do |
|---|---|---|---|
| 1 | `main` or `checkpoint-1` | the reviewer only | Read the spec. Run the prompt. Review the result |
| 2 | `checkpoint-2` | + `api-boundary` | Read the new skill. Run the same prompt. Review |
| 3 | `checkpoint-3` | same as 2, plus two finished apps | Review code you did not write. No build |
| 4 | `checkpoint-4` | + `csharp-quality`, `blazor-component-hygiene` | Run the same prompt. Review |

### Checkpoint 1 — the spec alone

The spec is detailed and still does not say whether the UI should talk to the database directly or go
through the API. Watch what your agent picks, then review it:

```
/review-build
```

Expect around ten findings. Note which are *architecture* and which are *quality* — checkpoints 2
and 4 remove those two groups separately.

### Checkpoint 2 — one skill

Read `.claude/skills/api-boundary/SKILL.md` first. It is short, and it decides the one thing the spec
left open. Same prompt, then `/review-build` again.

The architecture findings should be gone. The quality findings will not be — nothing yet addresses
them. That gap is checkpoint 4's job.

### Checkpoint 3 — review, don't build

Two finished apps, no build required:

| Directory | Built with |
|---|---|
| `AssetDesk/` | the `api-boundary` skill |
| `reference/AssetDesk-noskills/` | no skills at all |

Review both and compare the count blocks:

```
/review-build AssetDesk
/review-build reference/AssetDesk-noskills
```

Same reviewer, same rules, one difference: which skills were loaded when the code was written.

Then go find the bug you can *see*. Assign an asset that is already in repair. The error banner comes
up blank, because a `catch` block threw the message away — and `SPEC.md` §4.6 requires the UI to show
that message verbatim. It is in both apps. It is what checkpoint 4 fixes.

### Checkpoint 4 — the full stack

Three skills now. Same prompt, then `/review-build`.

Near zero findings. If your build ran long, `checkpoint-4-result` holds a reference version — kept on
its own branch on purpose, because an agent that finds a finished app in the repo will copy it
instead of building from the spec.

## Switching checkpoints

Build on your own branch at each checkpoint, and **commit before switching**. That is what makes the
switch clean.

```bash
git switch -c work/cp1 checkpoint-1     # build here
git add -A && git commit -m "cp1 build" # commit before you leave
git switch -c work/cp2 checkpoint-2     # next checkpoint
```

Why committing is the trick: untracked files **block** `git checkout`, so an uncommitted build leaves
you fighting git instead of reading diffs. Committed, it is tracked on your own branch — the switch
is clean, no `rm -rf` needed, your build is recoverable with `git switch work/cp1`, and `git branch`
becomes a record of your session.

Two things to do after every switch:

**1. Delete the database.** It is gitignored, so it survives the switch, and the seed only fires on an
empty database. Skip this and your first verification reports 13 assets instead of 12.

| macOS / Linux | Windows PowerShell |
|---|---|
| `find . -name 'assetdesk.db*' -delete` | `Get-ChildItem -Recurse -Filter 'assetdesk.db*' \| Remove-Item -Force` |

**2. Restart Claude Code.** Skills and commands register at startup only:

```
/exit
claude
```

`/clear` is **not** enough. If `/review-build` is missing from the `/` list, this is why.

## Getting your bearings on any branch

```bash
git branch --show-current
ls .claude/skills                 # which rules are loaded here
```

The second command is the one that matters. It tells you which checkpoint you are actually on.
```

- [ ] **Step 3: Verify every internal claim in the file matches what the branch will hold**

```bash
# every skill directory CHECKPOINTS.md names must be real by the end of Task 8
grep -oE '\.claude/skills/[a-z-]+' CHECKPOINTS.md | sort -u
```

Expected exactly: `.claude/skills/api-boundary`, `.claude/skills/review-build`. Any other name is a typo that will send students to a nonexistent file.

- [ ] **Step 4: Commit**

```bash
git add CHECKPOINTS.md
git commit -m "$(cat <<'EOF'
Add CHECKPOINTS.md: the four-level runbook

One spec, one prompt, four skill configurations. Documents the commit-before-
switching ritual, since untracked files block git checkout and an uncommitted
build turns a checkpoint switch into a fight with git.

Also documents the two things that bite after every switch: the gitignored
database survives and must be deleted, and skills only register at startup so
/clear is not enough.
EOF
)"
```

---

### Task 4: Rewire README and both instructions files

**Files:**
- Modify: `README.md` (the "What is checked in" table, "How the session runs", and the closing fallback note)
- Modify: `INSTRUCTIONS-MACOS.md:225-271` (§6 Branches) and `:307-323` (§8 troubleshooting table)
- Modify: `INSTRUCTIONS-WINDOWS.md` (the equivalent sections)

**Interfaces:**
- Consumes: `CHECKPOINTS.md` and its section headings from Task 3.
- Produces: the last of checkpoint 1's content. The commit at the end of this task is the one `main` fast-forwards to in Task 10, and the one `checkpoint-2` branches from in Task 5.

- [ ] **Step 1: Assert the stale claims are present**

```bash
grep -n '600 lines' README.md
grep -n 'git checkout dotnet-demo' README.md
grep -n 'nodejs-demo' INSTRUCTIONS-MACOS.md | head -3
```

Expected: each prints at least one line. These are the statements that are about to become wrong.

- [ ] **Step 2: README.md — add CHECKPOINTS.md to the checked-in table**

In the `## What is checked in` table, insert this row directly beneath the `SPEC.md` row:

```markdown
| [CHECKPOINTS.md](CHECKPOINTS.md) | The four checkpoints, the one prompt they all share, and how to switch between them. **Read this first.** |
```

- [ ] **Step 3: README.md — replace the "How the session runs" numbered list**

Replace the three numbered items under `## How the session runs` with:

```markdown
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
```

- [ ] **Step 4: README.md — fix the "600 lines" description and the closing fallback**

In the `SPEC.md` table row, replace `~600 lines describing` with `describing`. The spec is no longer ~600 lines and the number would drift again anyway.

Then replace the entire `## If your build goes sideways` section with:

```markdown
## If your build goes sideways

Each checkpoint has a finished reference build to fall back on — `checkpoint-3` for checkpoints 1 and
2, `checkpoint-4-result` for checkpoint 4. Commit your own work first (see
[CHECKPOINTS.md](CHECKPOINTS.md#switching-checkpoints)) so nothing is lost and the switch is clean.

`dotnet-demo` still holds the original single-exercise build against the unthinned spec.
```

- [ ] **Step 5: INSTRUCTIONS-MACOS.md — replace the §6 branch table**

Replace the three-row table under `## 6. Branches, and the one that saves you` (the `main` /
`dotnet-demo` / `nodejs-demo` table) with:

```markdown
| Branch | What it is | When |
|---|---|---|
| `main`, `checkpoint-1` | Checkpoint 1. Spec, runbook, instructions, and the reviewer | **Start here.** |
| `checkpoint-2` | + the `api-boundary` skill | Checkpoint 2 |
| `checkpoint-3` | + two finished apps to review. No build | Checkpoint 3 |
| `checkpoint-4` | + the two quality skills | Checkpoint 4 |
| `checkpoint-4-result` | Checkpoint 4's finished app | Only after your own checkpoint-4 build is done or abandoned |
| `dotnet-demo` | The original single-exercise build, against the unthinned spec | Historical reference |
| `nodejs-demo` | Archive of the pre-.NET attempt | Historical curiosity. **Not a working app.** |

The switching ritual — commit, switch, delete the database, restart Claude Code — is in
[CHECKPOINTS.md](CHECKPOINTS.md#switching-checkpoints). Follow it rather than the older
`rm -rf AssetDesk` advice: committing your build first makes the `rm -rf` unnecessary and keeps your
work.
```

Keep the existing `### Starting clean` and `### Falling back mid-build` subsections. They still
describe real failure modes for anyone who did not commit.

- [ ] **Step 6: INSTRUCTIONS-MACOS.md — add three troubleshooting rows**

Append these to the §8 table:

```markdown
| `/review-build` is missing from the `/` list after switching checkpoints | Skills register at startup only. `/exit`, then `claude`. `/clear` will not do it. |
| `git switch checkpoint-N` refuses over your build | You did not commit it. `git add -A && git commit -m wip`, then switch. Your build stays on the branch you left. |
| `MSBUILD : error MSB1011: more than one project` | `checkpoint-3` holds two apps. Name the one you mean: `dotnet build AssetDesk` or `dotnet build reference/AssetDesk-noskills`. |
```

- [ ] **Step 7: INSTRUCTIONS-WINDOWS.md — replace its branch table**

Replace the equivalent three-row branch table with:

```markdown
| Branch | What it is | When |
|---|---|---|
| `main`, `checkpoint-1` | Checkpoint 1. Spec, runbook, instructions, and the reviewer | **Start here.** |
| `checkpoint-2` | + the `api-boundary` skill | Checkpoint 2 |
| `checkpoint-3` | + two finished apps to review. No build | Checkpoint 3 |
| `checkpoint-4` | + the two quality skills | Checkpoint 4 |
| `checkpoint-4-result` | Checkpoint 4's finished app | Only after your own checkpoint-4 build is done or abandoned |
| `dotnet-demo` | The original single-exercise build, against the unthinned spec | Historical reference |
| `nodejs-demo` | Archive of the pre-.NET attempt | Historical curiosity. **Not a working app.** |

The switching ritual — commit, switch, delete the database, restart Claude Code — is in
[CHECKPOINTS.md](CHECKPOINTS.md#switching-checkpoints). Follow it rather than the older
`Remove-Item -Recurse -Force AssetDesk` advice: committing your build first makes the delete
unnecessary and keeps your work.
```

- [ ] **Step 8: INSTRUCTIONS-WINDOWS.md — add the three troubleshooting rows**

Append to its troubleshooting table:

```markdown
| `/review-build` is missing from the `/` list after switching checkpoints | Skills register at startup only. `/exit`, then `claude`. `/clear` will not do it. |
| `git switch checkpoint-N` refuses over your build | You did not commit it. `git add -A; git commit -m wip`, then switch. Your build stays on the branch you left. |
| `MSBUILD : error MSB1011: more than one project` | `checkpoint-3` holds two apps. Name the one you mean: `dotnet build AssetDesk` or `dotnet build reference\AssetDesk-noskills`. |
```

Note the PowerShell statement separator is `;` not `&&`, and the reference path uses a backslash.

- [ ] **Step 9: Verify no file still contradicts the new structure**

```bash
# no leftover claim that dotnet-demo is the only fallback
grep -rn 'only real fallback' README.md INSTRUCTIONS-MACOS.md INSTRUCTIONS-WINDOWS.md
# every branch named across the docs
grep -rhoE 'checkpoint-[0-9]+(-result)?' README.md INSTRUCTIONS-*.md CHECKPOINTS.md | sort -u
```

Expected: the first prints nothing (if it prints, rewrite that sentence — `checkpoint-3` and
`checkpoint-4-result` are fallbacks too). The second prints exactly `checkpoint-1`, `checkpoint-2`,
`checkpoint-3`, `checkpoint-4`, `checkpoint-4-result`.

- [ ] **Step 10: Confirm no app files snuck in**

```bash
git status --short && git diff --cached --stat
```

Expected: only `README.md`, `INSTRUCTIONS-MACOS.md`, `INSTRUCTIONS-WINDOWS.md`.

- [ ] **Step 11: Commit**

```bash
git add README.md INSTRUCTIONS-MACOS.md INSTRUCTIONS-WINDOWS.md
git commit -m "$(cat <<'EOF'
Point README and both instructions files at the checkpoints

The branch tables described a single exercise with one fallback; there are now
four checkpoints and three reference builds. Adds the three failure modes that
only appear once students switch branches: skills needing a restart to
register, checkout refusing over an uncommitted build, and MSB1011 from the
two projects on checkpoint-3.
EOF
)"
```

---

### Task 5: checkpoint-2 — the api-boundary skill

**Files:**
- Create: branch `checkpoint-2` from `checkpoint-1`
- Create: `.claude/skills/api-boundary/SKILL.md`

**Interfaces:**
- Consumes: `checkpoint-1` at Task 4's commit.
- Produces: the rules Tasks 7 and 9 implement and Task 6 deliberately violates. The client type name `AssetDeskClient`, the DTO file `Data/Dtos.cs`, and the six rule numbers are referenced by all three app tasks — do not rename them.

- [ ] **Step 1: Branch**

```bash
git switch -c checkpoint-2 checkpoint-1 && git log --oneline -1
```

Expected: Task 4's commit.

- [ ] **Step 2: Write the skill**

Create `.claude/skills/api-boundary/SKILL.md`:

```markdown
---
name: api-boundary
description: Use when building or changing AssetDesk UI or data access - requires the UI to reach data only through the HTTP API and never through AssetRepository directly.
---

# The UI talks to the API, never to the repository

AssetDesk keeps one boundary: components speak HTTP, the repository speaks SQL, and nothing crosses.

## Rules

1. **No component references `AssetRepository`.** Not `@inject`, not `[Inject]`, not a parameter
   type, not a `using`. The repository is constructed in `Program.cs` and reachable only from the
   endpoint handlers there.
2. **Every read and write goes through `/api/*`** — the six endpoints in `SPEC.md` §4.7. If the UI
   needs data, an endpoint serves it.
3. **One typed client, registered in DI.** `AssetDeskClient` wraps `HttpClient`. Never
   `new HttpClient()` at a call site: it bypasses your configuration and exhausts sockets under
   reconnects.
4. **Every call takes a `CancellationToken`** and checks the status code before deserialising. A
   Blazor circuit that drops mid-request must not leave the call running.
5. **DTOs at the boundary.** The records in `Data/Models.cs` are the database's shape. Components
   take DTOs from `Data/Dtos.cs`. A domain record must never appear in a `[Parameter]` property or a
   component's method signature.
6. **Translate errors once, in the client.** A 400 carries `{ "error": "..." }`. The client turns it
   into an `AssetDeskException` carrying that exact message. Components render the message verbatim
   and never write their own copy — `SPEC.md` §4.6 requires it.

## Event handlers become async

HTTP is asynchronous and there is no synchronous escape that does not risk deadlock. Handlers become
`async Task`; await the client call, then reassign state.

`SPEC.md` §3.4 still requires synchronous Dapper **inside the repository** — that rule is about the
data layer's own calls, not about the UI's HTTP hop. Both are correct at the same time.

## Registering the client

The app calls its own endpoints, so the base address is not known until the server is listening.
Resolve it from the server instead of hardcoding a port:

```csharp
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

builder.Services.AddHttpClient<AssetDeskClient>((sp, c) =>
{
    var addresses = sp.GetRequiredService<IServer>()
        .Features.Get<IServerAddressesFeature>()?.Addresses;
    var baseUrl = addresses?.FirstOrDefault() ?? "http://localhost:5198";
    c.BaseAddress = new Uri(baseUrl
        .Replace("[::]", "localhost")
        .Replace("0.0.0.0", "localhost"));
});
```

Hardcoding `http://localhost:5198` breaks the moment anyone passes a different `--urls`, and the
scaffold writes a random port into `launchSettings.json`.

**One trap.** `app.UseHttpsRedirection()` sits in front of your own endpoints. When only an http
address is bound — which is what `--urls http://localhost:5198` does — ASP.NET logs
`Failed to determine the https port for redirect` and skips redirecting, so the self-call works. That
log line is expected and is not a build warning. If you bind https as well, the self-call must target
the http address or it will chase a redirect to a port with no dev certificate.

## What this does not change

Everything else in `SPEC.md` §3.4 stands: one connection per repository method, a transaction per
mutation, schema init once in `Program.cs`. This skill moves the UI's data path and nothing else.
```

- [ ] **Step 3: Verify the skill names match what the app tasks will build**

```bash
grep -oE 'AssetDeskClient|Data/Dtos\.cs|AssetDeskException' .claude/skills/api-boundary/SKILL.md | sort -u
```

Expected: all three. These exact names are what Tasks 7 and 9 implement.

- [ ] **Step 4: Verify SPEC.md is still untouched on this branch**

```bash
git diff checkpoint-1 checkpoint-2 --stat
```

Expected: nothing yet (the skill is uncommitted). After the commit in Step 5 this must show only
`.claude/skills/api-boundary/SKILL.md`.

- [ ] **Step 5: Commit**

```bash
git add .claude/skills/api-boundary
git commit -m "$(cat <<'EOF'
checkpoint-2: the api-boundary skill

Settles the one architectural question the thinned spec leaves open. Six rules,
plus the two details that actually cost time: the base address has to be
resolved from IServerAddressesFeature because a self-calling app does not know
its own port until Kestrel is listening, and UseHttpsRedirection sits in front
of those endpoints.
EOF
)"
git diff checkpoint-1 checkpoint-2 --stat
```

Expected from the final command: one file, `.claude/skills/api-boundary/SKILL.md`.

---

### Task 6: checkpoint-3 — the no-skills reference app

**Files:**
- Create: branch `checkpoint-3` from `checkpoint-2`
- Create: `reference/AssetDesk-noskills/` — the full app, copied from `dotnet-demo`
- Modify, inside that copy: `AssetDesk.csproj`, `Components/Pages/Home.razor`, `Components/Shared/AssetsTable.razor`, `Components/Shared/AddAssetDrawer.razor`, `Data/AssetRepository.cs`

**Interfaces:**
- Consumes: `checkpoint-2`; `dotnet-demo`'s app as the source tree.
- Produces: the ~10-finding baseline. Its five quality issues (Q1–Q5) must be reproduced byte-for-byte in Task 7's app, so the exact code below is the contract between the two tasks.

This app keeps `dotnet-demo`'s shape — components inject `AssetRepository`, no HTTP layer — which is
already five boundary findings (B1–B5) without editing anything. Then it gains the five quality
issues.

- [ ] **Step 1: Branch and copy the app**

```bash
git switch -c checkpoint-3 checkpoint-2
mkdir -p reference
git archive dotnet-demo AssetDesk | tar -x -C reference
mv reference/AssetDesk reference/AssetDesk-noskills
ls reference/AssetDesk-noskills
```

Expected: `AssetDesk.csproj  Components  Data  Program.cs  Properties  appsettings.Development.json  appsettings.json  wwwroot`. `git archive` copies only tracked files, so no `bin/`, `obj/`, or database comes along.

- [ ] **Step 2: Verify the boundary issues are already present — this is the RED state that stays red**

```bash
grep -rn 'AssetRepository' reference/AssetDesk-noskills/Components/ | head
grep -rc 'HttpClient' reference/AssetDesk-noskills/ 2>/dev/null | grep -v ':0' || echo "no HttpClient anywhere — B2 confirmed"
```

Expected: the first prints `[Inject] private AssetRepository Repo` hits in the components (B1, B3, B4, B5). The second prints the "no HttpClient" message (B2). Leave all of it exactly as it is.

- [ ] **Step 3: Confirm it builds before you break anything**

```bash
export PATH="/usr/local/share/dotnet:$PATH"
dotnet build reference/AssetDesk-noskills
```

Expected: `0 Warning(s)`, `0 Error(s)`. If it does not build straight from `dotnet-demo`, stop and report — nothing below is worth doing on a broken baseline.

- [ ] **Step 4: Plant Q1 — the swallowed exception**

`reference/AssetDesk-noskills/Components/Shared/AssignDialog.razor` currently reads, at lines 58-60:

```csharp
        catch (AssetDeskException ex)
        {
            _error = ex.Message;
        }
```

Replace that with:

```csharp
        // Q1: swallowed exception. _error is set to "" rather than null, so the
        // banner's @if (_error is not null) guard still passes and renders an
        // empty red box with no reason in it.
        catch (AssetDeskException)
        {
            _error = "";
        }
```

The empty string rather than `null` is the point: line 6 of the same file guards the banner with
`@if (_error is not null)`, so `""` renders the box and discards the reason. This is the issue students
can *see* — assign an asset that is in repair and the banner comes up blank. It also violates
`SPEC.md` §4.6, so it belongs in the SPEC bucket as well as QUALITY.

- [ ] **Step 5: Plant Q2 — a real warning and the suppression hiding it**

In `reference/AssetDesk-noskills/AssetDesk.csproj`, inside the first `<PropertyGroup>`, add:

```xml
<NoWarn>$(NoWarn);CS8602</NoWarn>
```

Then in `Components/Shared/AssetsTable.razor`, introduce the dereference that warning was hiding —
inside the `@code` block, resolve the assignee name without a null check:

```csharp
// Q2: an unguarded dereference whose CS8602 is hidden by the NoWarn above.
private string AssigneeName(Asset a) =>
    Employees.FirstOrDefault(e => e.Id == a.AssignedTo).Name;
```

`FirstOrDefault` returns `Employee?`, so `.Name` raises `CS8602: Dereference of a possibly null
reference`, and the `<NoWarn>` is what keeps the build at zero warnings. **Do not write `!` here.** The
null-forgiving operator suppresses CS8602 by itself, which would leave the `<NoWarn>` dead and the
"suppressed warning" nonexistent — the two halves of this issue have to be a real warning and a real
suppression, or the reviewer has nothing to find.

Call it from the `Assigned to` cell, guarded so the app still runs:
`@(a.AssignedTo is null ? "—" : AssigneeName(a))`. The defect stays real for any caller that skips the
guard, which is the point.

Confirm the warning is genuinely being suppressed rather than absent — temporarily remove the
`<NoWarn>` line, run `dotnet build reference/AssetDesk-noskills`, and check that `CS8602` appears. Put
the line back and confirm the build returns to `0 Warning(s)`. If CS8602 never appears, the dereference
is not actually unguarded and Q2 is not planted.

- [ ] **Step 6: Plant Q3 — remove the server-side guard**

In `reference/AssetDesk-noskills/Data/AssetRepository.cs`, `CreateAssets` opens at line 36 and its
first statement is `ValidateNewAsset(input);`. Comment that call out:

```csharp
    public List<Asset> CreateAssets(NewAssetInput input)
    {
        // Q3: validation now happens only in the drawer UI, so curl bypasses it entirely.
        // ValidateNewAsset(input);

        var tags = ExpandTags(input.Tag, input.Quantity);
```

Leave the `ValidateNewAsset` method itself (line 179) in place and unused — an orphaned guard is more
realistic than a deleted one, and the compiler will not warn about a private static method that is
never called. `SPEC.md` §9 explicitly tests this path with `curl`, so this is a SPEC finding too.

- [ ] **Step 7: Plant Q4 — inline the table into Home.razor**

Move the entire contents of `Components/Shared/AssetsTable.razor` into
`Components/Pages/Home.razor`, delete `AssetsTable.razor`, and re-declare the row shape inside
`Home.razor`'s `@code` block rather than using the `Asset` record:

```csharp
// Q4: a second source of truth for a shape that already exists in Data/Models.cs.
private record Row(string Tag, string Category, string Status, string AssignedTo, double Cost);
```

`Home.razor` must end up over 250 lines — roughly 290, being its own 50 plus the table's 246 — and be
doing fetch, state, filtering, and rendering at once. It also collapses a component `SPEC.md` §3.2
names, which is the violation even before the line count. Verify with `wc -l`.

- [ ] **Step 8: Plant Q5 — compare status as a string**

In `Home.razor`, replace the enum comparisons in the filter and count logic with string literals:

```csharp
// Q5: magic strings where Status exists as an enum in Data/Models.cs.
private int InStockCount => _state.Assets.Count(a => a.Status.ToString() == "InStock");
private bool IsRepair(Asset a) => a.Status.ToString() == "Repair";
```

- [ ] **Step 9: Verify GREEN — all five issues present, and it still builds and runs**

```bash
export PATH="/usr/local/share/dotnet:$PATH"
grep -c 'NoWarn' reference/AssetDesk-noskills/AssetDesk.csproj            # 1  (Q2)
grep -rc '_error = ""' reference/AssetDesk-noskills/Components/           # 1  (Q1)
grep -rc '// ValidateNewAsset(input);' reference/AssetDesk-noskills/Data/         # 1  (Q3)
wc -l reference/AssetDesk-noskills/Components/Pages/Home.razor            # >250 (Q4)
grep -rc 'ToString() == "InStock"' reference/AssetDesk-noskills/          # 1  (Q5)
ls reference/AssetDesk-noskills/Components/Shared/AssetsTable.razor       # must not exist (Q4)
dotnet build reference/AssetDesk-noskills
```

Expected: the counts above, `AssetsTable.razor` absent, and `0 Warning(s), 0 Error(s)`. The build must
stay clean — `CS8602` is suppressed by design, which is the finding.

- [ ] **Step 10: Verify it runs and the data layer is intact**

```bash
export PATH="/usr/local/share/dotnet:$PATH"
dotnet run --project reference/AssetDesk-noskills --urls http://localhost:5198 &
sleep 12
curl -s http://localhost:5198/api/health
curl -s http://localhost:5198/api/state | jq '{employees:(.employees|length), assets:(.assets|length)}'
kill %1
```

Expected: `{"ok":true}` and `{ "employees": 5, "assets": 12 }`. If assets is 13, a database survived —
`find reference -name 'assetdesk.db*' -delete` and rerun.

- [ ] **Step 11: Commit**

```bash
git add reference
git commit -m "$(cat <<'EOF'
checkpoint-3: the no-skills reference app

dotnet-demo's shape unchanged - components inject AssetRepository and there is
no HTTP layer, which is the five boundary findings on its own - plus the five
quality issues that both checkpoint-3 apps share.

Builds with 0 warnings and runs correctly. Every planted issue is a quality or
architecture defect rather than breakage, except the swallowed exception, whose
blank error banner is deliberately visible so students can click it.
EOF
)"
```

---

### Task 7: checkpoint-3 — the api-boundary reference app

**Files:**
- Create: `AssetDesk/` on `checkpoint-3` — copied from `dotnet-demo`, then restructured
- Create: `AssetDesk/Data/Dtos.cs`, `AssetDesk/Data/AssetDeskClient.cs`
- Modify: `AssetDesk/Program.cs`, every file under `AssetDesk/Components/`

**Interfaces:**
- Consumes: the six rules and the client-registration code from Task 5's skill; the exact Q1–Q5 code from Task 6.
- Produces: `AssetDeskClient` with methods `GetStateAsync`, `CreateAssetsAsync`, `AssignAsync`, `ReturnAsync`, `SetStatusAsync` — all taking a trailing `CancellationToken ct`. Task 9 starts from this file and keeps these signatures.

- [ ] **Step 1: Copy a fresh tree**

```bash
git archive dotnet-demo AssetDesk | tar -x
ls AssetDesk
```

Expected: the same file list as Task 6 Step 1, now at the repo root.

- [ ] **Step 2: Assert the boundary is currently violated — the RED state this task fixes**

```bash
grep -rn 'AssetRepository' AssetDesk/Components/ | wc -l
```

Expected: a non-zero count. By Step 9 this must be `0`.

- [ ] **Step 3: Create the DTOs**

Create `AssetDesk/Data/Dtos.cs`:

```csharp
namespace AssetDesk.Data;

// The boundary's shape, declared once. Components never see the records in Models.cs.
public record EmployeeDto(string Id, string Name, string Email, string Department, string Title);

public record AssetDto(
    string Id,
    string Tag,
    Category Category,
    string Make,
    string Model,
    string Serial,
    Status Status,
    Condition Condition,
    string PurchaseDate,
    double Cost,
    string Location,
    string Notes,
    string? AssignedTo,
    string? AssignedDate);

public record AppStateDto(List<EmployeeDto> Employees, List<AssetDto> Assets);

public record NewAssetInputDto(
    string Tag,
    Category Category,
    string Make,
    string Model,
    string Serial,
    Condition Condition,
    string PurchaseDate,
    double Cost,
    string Location,
    string Notes,
    int Quantity);

public record AssignRequest(string EmployeeId, string AssignedDate);
public record StatusRequest(Status Status);
public record ApiError(string Error);
```

Then delete the `AssignRequest` and `StatusRequest` declarations from the bottom of `Program.cs` —
they now live here, declared once.

- [ ] **Step 4: Create the typed client**

Create `AssetDesk/Data/AssetDeskClient.cs`:

```csharp
using System.Net.Http.Json;

namespace AssetDesk.Data;

// The only place an HTTP failure becomes an AssetDeskException. Components never
// see a status code and never write their own error copy (SPEC.md 4.6).
public class AssetDeskClient(HttpClient http)
{
    public Task<AppStateDto> GetStateAsync(CancellationToken ct) =>
        GetAsync<AppStateDto>("/api/state", ct);

    public Task<List<AssetDto>> CreateAssetsAsync(NewAssetInputDto input, CancellationToken ct) =>
        PostAsync<NewAssetInputDto, List<AssetDto>>("/api/assets", input, ct);

    public Task<AssetDto> AssignAsync(string assetId, string employeeId, string assignedDate, CancellationToken ct) =>
        PostAsync<AssignRequest, AssetDto>(
            $"/api/assets/{assetId}/assign", new AssignRequest(employeeId, assignedDate), ct);

    public Task<AssetDto> ReturnAsync(string assetId, CancellationToken ct) =>
        PostAsync<object?, AssetDto>($"/api/assets/{assetId}/return", null, ct);

    public Task<AssetDto> SetStatusAsync(string assetId, Status status, CancellationToken ct) =>
        PostAsync<StatusRequest, AssetDto>(
            $"/api/assets/{assetId}/status", new StatusRequest(status), ct);

    private async Task<TOut> GetAsync<TOut>(string path, CancellationToken ct)
    {
        using var response = await http.GetAsync(path, ct);
        return await ReadAsync<TOut>(response, ct);
    }

    private async Task<TOut> PostAsync<TIn, TOut>(string path, TIn body, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync(path, body, ct);
        return await ReadAsync<TOut>(response, ct);
    }

    private static async Task<TOut> ReadAsync<TOut>(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            var problem = await response.Content.ReadFromJsonAsync<ApiError>(ct);
            throw new AssetDeskException(
                problem?.Error ?? "The server could not complete that request. Refresh and try again.");
        }

        return await response.Content.ReadFromJsonAsync<TOut>(ct)
               ?? throw new AssetDeskException("The server returned no data. Refresh to try again.");
    }
}
```

- [ ] **Step 5: Register the client and map DTOs at the endpoints**

In `AssetDesk/Program.cs`, add the usings and the registration from the `api-boundary` skill:

```csharp
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

builder.Services.AddHttpClient<AssetDeskClient>((sp, c) =>
{
    var addresses = sp.GetRequiredService<IServer>()
        .Features.Get<IServerAddressesFeature>()?.Addresses;
    var baseUrl = addresses?.FirstOrDefault() ?? "http://localhost:5198";
    c.BaseAddress = new Uri(baseUrl
        .Replace("[::]", "localhost")
        .Replace("0.0.0.0", "localhost"));
});
```

Add a private mapping helper at the bottom of `Program.cs` and use it in the endpoint handlers so the
API returns DTOs rather than domain records:

```csharp
static AssetDto ToDto(Asset a) => new(
    a.Id, a.Tag, a.Category, a.Make, a.Model, a.Serial, a.Status, a.Condition,
    a.PurchaseDate, a.Cost, a.Location, a.Notes, a.AssignedTo, a.AssignedDate);

static EmployeeDto ToDto(Employee e) => new(e.Id, e.Name, e.Email, e.Department, e.Title);
```

`/api/state` returns `new AppStateDto(state.Employees.Select(ToDto).ToList(), state.Assets.Select(ToDto).ToList())`. The four mutation endpoints return `ToDto(...)`. `/api/assets` maps its
`NewAssetInputDto` body to `NewAssetInput` before calling the repository.

Keep `builder.Services.AddSingleton(new AssetRepository(connectionString))` — the endpoints still need
it. Rule 1 forbids *components* from reaching it, not `Program.cs`.

- [ ] **Step 6: Convert the components**

For every file under `AssetDesk/Components/`:

- Replace `[Inject] private AssetRepository Repo { get; set; } = default!;` with
  `[Inject] private AssetDeskClient Client { get; set; } = default!;`
- Change every `[Parameter]` and local type from `Asset`/`Employee`/`AppState` to
  `AssetDto`/`EmployeeDto`/`AppStateDto`
- Make every event handler `async Task` and await the client
- Thread a `CancellationToken` through. `Home.razor` owns it:

```csharp
private readonly CancellationTokenSource _cts = new();

protected override async Task OnInitializedAsync() => await Refresh();

private async Task Refresh()
{
    try
    {
        _state = await Client.GetStateAsync(_cts.Token);
        _error = null;
    }
    catch (AssetDeskException ex)
    {
        _error = ex.Message;
    }
}

public void Dispose()
{
    _cts.Cancel();
    _cts.Dispose();
}
```

`Home.razor` declares `@implements IDisposable`.

- [ ] **Step 7: Plant the same five quality issues**

Apply Q1 through Q5 exactly as written in Task 6, Steps 4–8, adapted to the async call sites:

- **Q1** in `AssignDialog.razor`: `catch (AssetDeskException) { _error = ""; }`
- **Q2** in `AssetDesk.csproj`: `<NoWarn>$(NoWarn);CS8602</NoWarn>`, plus
  `Employees.FirstOrDefault(e => e.Id == a.AssignedTo).Name` in the assignee cell — no `!`, for the
  reason given in Task 6 Step 5 — typed on `AssetDto`/`EmployeeDto` here
- **Q3** in `Data/AssetRepository.cs`: `// ValidateNewAsset(input);`
- **Q4**: inline the table into `Home.razor`, delete `AssetsTable.razor`, re-declare
  `private record Row(string Tag, string Category, string Status, string AssignedTo, double Cost);`
- **Q5** in `Home.razor`: `a.Status.ToString() == "InStock"` and `a.Status.ToString() == "Repair"`

These must match Task 6's app. Identical quality issues in both apps is what proves the
`api-boundary` skill changed the architecture and nothing else.

- [ ] **Step 8: Build**

```bash
export PATH="/usr/local/share/dotnet:$PATH"
dotnet build AssetDesk
```

Expected: `0 Warning(s)`, `0 Error(s)`.

- [ ] **Step 9: Verify GREEN — the boundary holds and the five issues are present**

```bash
export PATH="/usr/local/share/dotnet:$PATH"
grep -rc 'AssetRepository' AssetDesk/Components/ 2>/dev/null | grep -v ':0' || echo "0 — boundary holds"
grep -c 'AddHttpClient<AssetDeskClient>' AssetDesk/Program.cs      # 1
grep -rc 'new HttpClient' AssetDesk/ 2>/dev/null | grep -v ':0' || echo "0 — no ad-hoc clients"
grep -rc 'CancellationToken' AssetDesk/Data/AssetDeskClient.cs     # >=6
grep -rn 'Asset \|Employee \|AppState ' AssetDesk/Components/ | grep -v Dto || echo "0 — no domain records in components"
grep -c 'NoWarn' AssetDesk/AssetDesk.csproj                        # 1  (Q2)
grep -rc '_error = ""' AssetDesk/Components/                       # 1  (Q1)
```

Expected: boundary clean on the first, third and fifth checks; the client registered; a token on
every method; and Q1/Q2 present.

- [ ] **Step 10: Verify it runs, including the self-call**

```bash
export PATH="/usr/local/share/dotnet:$PATH"
find AssetDesk -name 'assetdesk.db*' -delete
dotnet run --project AssetDesk --urls http://localhost:5198 &
sleep 12
curl -s http://localhost:5198/api/health
curl -s http://localhost:5198/api/state | jq '{employees:(.employees|length), assets:(.assets|length)}'
kill %1
```

Expected: `{"ok":true}` and `{ "employees": 5, "assets": 12 }`.

Then open <http://localhost:5198> by hand and click a tab. The self-calling `HttpClient` is the one
thing `curl` cannot prove — if the UI renders an empty table while `/api/state` returns 12 assets, the
client's base address is wrong. `Failed to determine the https port for redirect` in the log is
expected and harmless.

- [ ] **Step 11: Commit**

```bash
git add AssetDesk
git commit -m "$(cat <<'EOF'
checkpoint-3: the api-boundary reference app

Same app as reference/AssetDesk-noskills with the UI moved behind /api/*: a
typed AssetDeskClient resolved from IServerAddressesFeature, DTOs declared once
in Data/Dtos.cs, cancellation threaded from Home.razor, and 400 responses
translated to AssetDeskException in exactly one place.

Carries the identical five quality issues, which is what makes the two reviews
on this branch differ by architecture alone.
EOF
)"
```

---

### Task 8: checkpoint-4 — the two quality skills

**Files:**
- Create: branch `checkpoint-4` from `checkpoint-2` (**not** from `checkpoint-3` — no app may come along)
- Create: `.claude/skills/csharp-quality/SKILL.md`
- Create: `.claude/skills/blazor-component-hygiene/SKILL.md`

**Interfaces:**
- Consumes: `checkpoint-2`.
- Produces: the rules that turn Q1–Q5 into findings. Task 9's app must satisfy every rule here.

- [ ] **Step 1: Branch from checkpoint-2 and prove no app came with it**

```bash
git switch -c checkpoint-4 checkpoint-2
ls AssetDesk reference 2>&1
```

Expected: `No such file or directory` for both. Checkpoint 4 is a live build; an agent that finds a
finished app in the tree will copy it instead of building from the spec, and the exercise would be
measuring copying rather than skills.

- [ ] **Step 2: Write the csharp-quality skill**

Create `.claude/skills/csharp-quality/SKILL.md`:

```markdown
---
name: csharp-quality
description: Use when writing or changing C# in AssetDesk - forbids swallowed exceptions, warning suppressions, and endpoints that trust their input.
---

# Three things that must never ship

## 1. No swallowed exceptions

An empty or message-discarding `catch` turns a failure into silence. `SPEC.md` §4.6 requires the UI to
show the repository's message verbatim, so swallowing one breaks the spec as well as the user.

```csharp
// NEVER
try { await Client.AssignAsync(id, employeeId, date, ct); }
catch (AssetDeskException) { }

// NEVER — discards the message the UI is required to show
catch (AssetDeskException) { _error = ""; }

// NEVER — invents copy the spec says not to write
catch (AssetDeskException) { _error = "Something went wrong."; }

// CORRECT
catch (AssetDeskException ex) { _error = ex.Message; }
```

Catch the specific type. If you cannot handle it, let it propagate.

## 2. No suppressions

No `!` null-forgiving operator. No `#pragma warning disable`. No `<NoWarn>` in the `.csproj`. A
warning is information; suppressing it deletes the information and keeps the bug.

```csharp
// NEVER — the .csproj carries <NoWarn>CS8602</NoWarn>, so this compiles clean
// and keeps the NullReferenceException
private string AssigneeName(AssetDto a) =>
    Employees.FirstOrDefault(e => e.Id == a.AssignedTo).Name;

// ALSO NEVER — same bug, suppressed inline instead of in the .csproj
private string AssigneeName(AssetDto a) =>
    Employees.FirstOrDefault(e => e.Id == a.AssignedTo)!.Name;

// CORRECT
private string AssigneeName(AssetDto a) =>
    Employees.FirstOrDefault(e => e.Id == a.AssignedTo)?.Name ?? "—";
```

```csharp
// NEVER
var state = await response.Content.ReadFromJsonAsync<AppStateDto>(ct)!;

// CORRECT
var state = await response.Content.ReadFromJsonAsync<AppStateDto>(ct)
            ?? throw new AssetDeskException("The server returned no data. Refresh to try again.");
```

One documented exception exists: `NU1903` for `SQLitePCLRaw.lib.e_sqlite3`, pinned transitively by
`Microsoft.Data.Sqlite` with no newer patch. If you suppress that one, the `.csproj` carries a comment
saying which package and why.

## 3. Validate at the boundary, not only in the UI

Every endpoint validates its own body. UI validation is a convenience for the user, never a defence —
`curl` bypasses it entirely, and `SPEC.md` §9 explicitly tests that path.

`NewAssetInput` goes through the repository's hand-written guard before any SQL runs, per `SPEC.md`
§4.6. Drawer validation does not replace it, and commenting the guard out because the drawer already
checks is the exact mistake this rule exists to prevent.
```

- [ ] **Step 3: Write the blazor-component-hygiene skill**

Create `.claude/skills/blazor-component-hygiene/SKILL.md`:

```markdown
---
name: blazor-component-hygiene
description: Use when writing or changing .razor components in AssetDesk - caps component size, keeps logic out of markup, forbids duplicate DTO declarations, and requires enum values over string literals.
---

# Components stay small and dumb

## 1. One component, one job

A component that fetches data, holds page state, filters it, and renders three views is four
responsibilities in one file. Extract the inner pieces into components under `Components/Shared/` and
pass them parameters.

`SPEC.md` §3.2 already names the components this app has — `Dashboard`, `AssetsTable`,
`AddAssetDrawer`, `AssignDialog`, `People`, `StatusPill`. Keeping them is not optional; collapsing two
of them into one file is a violation even if the result is short.

**Numeric tripwire: 250 lines.** Past that, a `.razor` file is doing too much and needs splitting. A
dense table component in the low 200s is fine — the line count is a signal, the responsibility count
is the rule.

## 2. No business logic in markup

`@if` on a field is fine. Filtering, sorting, totalling, and formatting belong behind a named member
in `@code`, or in `Data/Format.cs` where `SPEC.md` put them.

```razor
@* NEVER *@
@foreach (var a in Assets.Where(x => x.Status == Status.InStock && x.Cost > 500).OrderBy(x => x.Tag))

@* CORRECT *@
@foreach (var a in VisibleAssets)

@code {
    private IEnumerable<AssetDto> VisibleAssets =>
        Assets.Where(a => a.Status == Status.InStock && a.Cost > 500).OrderBy(a => a.Tag);
}
```

## 3. Declare each DTO once

One definition, in `Data/Dtos.cs`, shared by every component that needs it.

```csharp
// NEVER — a second source of truth for a shape Data/Dtos.cs already defines
@code {
    private record Row(string Tag, string Category, string Status, string AssignedTo, double Cost);
}
```

It will drift from the real shape, and the drift shows up as a rendering bug nobody can trace.

## 4. Enum values come from the enum

`Status`, `Category`, and `Condition` are enums in `Data/Models.cs`. Never compare them as strings.

```csharp
// NEVER
private int InStockCount => Assets.Count(a => a.Status.ToString() == "InStock");

// CORRECT
private int InStockCount => Assets.Count(a => a.Status == Status.InStock);
```

A string literal is unchecked: rename a member and the compiler stays silent while the count silently
goes to zero. `SPEC.md` §4.3 covers how these are stored and §4.1 names every member — use them.
```

- [ ] **Step 4: Verify each planted issue has a rule that names it**

```bash
grep -l 'NoWarn' .claude/skills/csharp-quality/SKILL.md                        # Q2
grep -l '_error = ""' .claude/skills/csharp-quality/SKILL.md                   # Q1
grep -l 'guard' .claude/skills/csharp-quality/SKILL.md                         # Q3
grep -l 'private record Row' .claude/skills/blazor-component-hygiene/SKILL.md   # Q4
grep -l 'ToString() == "InStock"' .claude/skills/blazor-component-hygiene/SKILL.md  # Q5
```

Expected: all five print their filename. Every planted issue must be catchable by a rule that names
it concretely — a rule the reviewer has to infer will report inconsistently.

- [ ] **Step 5: Verify the branch holds skills and nothing else new**

```bash
git diff checkpoint-2 --stat
ls .claude/skills
```

Expected: two new `SKILL.md` files and nothing else. `ls` shows `api-boundary`,
`blazor-component-hygiene`, `csharp-quality`, `review-build`.

- [ ] **Step 6: Commit**

```bash
git add .claude/skills/csharp-quality .claude/skills/blazor-component-hygiene
git commit -m "$(cat <<'EOF'
checkpoint-4: the two quality skills

csharp-quality covers the three defects that survive a correct architecture -
swallowed exceptions, suppressed warnings, and endpoints that trust their body.
blazor-component-hygiene covers component size, logic in markup, duplicated DTO
declarations, and string literals standing in for enum members.

Each rule names a concrete violation rather than stating a principle, so the
reviewer reports the same finding every time instead of inferring a standard.
EOF
)"
```

---

### Task 9: checkpoint-4-result — the clean app

**Files:**
- Create: branch `checkpoint-4-result` from `checkpoint-4`
- Create: `AssetDesk/` — Task 7's app with all five quality issues repaired

**Interfaces:**
- Consumes: `checkpoint-3`'s `AssetDesk/` as the starting tree; the rules from Task 8.
- Produces: the ~0-finding endpoint of the demo.

- [ ] **Step 1: Branch and take checkpoint-3's app**

```bash
git switch -c checkpoint-4-result checkpoint-4
git checkout checkpoint-3 -- AssetDesk
export PATH="/usr/local/share/dotnet:$PATH"
dotnet build AssetDesk
```

Expected: `0 Warning(s), 0 Error(s)` — the api-boundary work carries over intact.

- [ ] **Step 2: Assert the five issues are present — the RED state this task clears**

```bash
grep -c 'NoWarn' AssetDesk/AssetDesk.csproj                    # 1
grep -rc '_error = ""' AssetDesk/Components/                   # 1
grep -rc '// ValidateNewAsset(input);' AssetDesk/Data/                 # 1
grep -rc 'private record Row' AssetDesk/Components/            # 1
grep -rc 'ToString() == "InStock"' AssetDesk/Components/       # 1
```

Expected: `1` five times.

- [ ] **Step 3: Fix Q1 — surface the message**

In `Components/Shared/AssignDialog.razor`:

```csharp
catch (AssetDeskException ex)
{
    _error = ex.Message;
}
```

- [ ] **Step 4: Fix Q2 — remove the suppression and the bug it was hiding**

Delete the `<NoWarn>$(NoWarn);CS8602</NoWarn>` line from `AssetDesk.csproj`. Build once with the
dereference still unguarded and confirm `CS8602` now appears — that proves the suppression was load
bearing and you are fixing a real warning, not deleting a no-op line. Then fix it:

```csharp
private string AssigneeName(AssetDto a) =>
    Employees.FirstOrDefault(e => e.Id == a.AssignedTo)?.Name ?? "—";
```

The build must return to 0 warnings *without* any suppression. That is the proof.

Also drop the now-unnecessary guard at the call site — `@AssigneeName(a)` is enough, since the method
handles null itself.

- [ ] **Step 5: Fix Q3 — restore the server-side guard**

In `Data/AssetRepository.cs`, restore `CreateAssets`'s first statement and delete the Q3 comment:

```csharp
    public List<Asset> CreateAssets(NewAssetInput input)
    {
        ValidateNewAsset(input);

        var tags = ExpandTags(input.Tag, input.Quantity);
```

- [ ] **Step 6: Fix Q4 — split the table back out and drop the duplicate record**

Take the original markup as your starting point rather than rewriting it:

```bash
git show dotnet-demo:AssetDesk/Components/Shared/AssetsTable.razor > AssetDesk/Components/Shared/AssetsTable.razor
```

Then adapt that file to this branch: its `[Parameter]` types become DTOs
(`List<AssetDto> Rows`, `List<EmployeeDto> Employees`), its mutation calls go through
`Client` with a `CancellationToken` instead of `Repo`, and its handlers become `async Task`. Delete the
inlined copy and the `private record Row(...)` declaration from `Home.razor`, which goes back to owning
state and refresh only at around 50 lines.

`AssetsTable.razor` lands around 246 lines, under the skill's 250-line tripwire and correct on
responsibility — it renders one table and nothing else.

- [ ] **Step 7: Fix Q5 — compare the enum**

```csharp
private int InStockCount => _state.Assets.Count(a => a.Status == Status.InStock);
private bool IsRepair(AssetDto a) => a.Status == Status.Repair;
```

- [ ] **Step 8: Verify GREEN — every issue gone, build still clean**

```bash
export PATH="/usr/local/share/dotnet:$PATH"
# all five must print 0 or nothing
grep -c 'NoWarn' AssetDesk/AssetDesk.csproj
grep -rc '_error = ""' AssetDesk/Components/ 2>/dev/null | grep -v ':0' || echo "0 — Q1 fixed"
grep -rn '// ValidateNewAsset(input);' AssetDesk/Data/ || echo "0 — Q3 fixed"
grep -rn 'private record Row' AssetDesk/Components/ || echo "0 — Q4 fixed"
grep -rn 'ToString() == "' AssetDesk/Components/ || echo "0 — Q5 fixed"
grep -rn '!\.' AssetDesk/ --include=*.cs --include=*.razor || echo "0 — no null-forgiving operator"
wc -l AssetDesk/Components/Pages/Home.razor                     # ~50, well under 250
wc -l AssetDesk/Components/Shared/AssetsTable.razor             # ~246, under 250
ls AssetDesk/Components/Shared/AssetsTable.razor                # exists again
dotnet build AssetDesk
```

Expected: every check clears, both components under the 250-line tripwire, `AssetsTable.razor` back, and
`0 Warning(s), 0 Error(s)` **with no suppression in the csproj**.

- [ ] **Step 9: Verify it runs and behaves**

```bash
export PATH="/usr/local/share/dotnet:$PATH"
find AssetDesk -name 'assetdesk.db*' -delete
dotnet run --project AssetDesk --urls http://localhost:5198 &
sleep 12
curl -s http://localhost:5198/api/state | jq '{employees:(.employees|length), assets:(.assets|length)}'

# Q3's fix: the server rejects a bad body that the drawer would have caught
curl -s -X POST http://localhost:5198/api/assets -H 'Content-Type: application/json' -d '{
  "tag":"AST-9999","category":"Headset","make":"Jabra","model":"E2","serial":"X1",
  "condition":"New","purchaseDate":"2026-08-04","cost":-5,"location":"HQ",
  "notes":"","quantity":1}'
kill %1
```

Expected: `{ "employees": 5, "assets": 12 }`, then `{"error":"Cost must be a number of 0 or more."}`.
A `201` instead means the guard is still bypassed and Q3 is not actually fixed.

Then open <http://localhost:5198>, assign an asset already in repair, and confirm the banner now shows
`AST-1003 is in repair and cannot be assigned.` — Q1's fix, visible.

- [ ] **Step 10: Commit**

```bash
git add AssetDesk AssetDesk/AssetDesk.csproj
git commit -m "$(cat <<'EOF'
checkpoint-4-result: the app the full skill stack produces

Task 7's app with all five quality issues repaired: the error message reaches
the banner, the suppression is gone and the build is still clean without it,
the server-side guard runs again, the table is its own component with no
duplicate row record, and status comparisons use the enum.

On its own branch so nothing on checkpoint-4 gives a live build something to
copy.
EOF
)"
```

---

### Task 10: Land main and verify the whole set

**Files:**
- Modify: branch pointer `main` → `checkpoint-1`
- Modify: `docs/superpowers/specs/2026-08-04-checkpoint-branches-design.md` on `checkpoints-setup` (the Q5 correction)

**Interfaces:**
- Consumes: every branch built in Tasks 1–9.
- Produces: the finished branch set, verified against the spec's definition of done.

- [ ] **Step 1: Fast-forward main to checkpoint-1**

```bash
git switch main && git merge --ff-only checkpoint-1 && git log --oneline -1
git rev-parse main checkpoint-1
```

Expected: the merge succeeds as a fast-forward and both SHAs are identical. If git refuses the
fast-forward, `main` gained a commit during the work — stop and report rather than forcing it.

- [ ] **Step 2: Verify SPEC.md is byte-identical across all six branches**

```bash
for b in checkpoint-1 checkpoint-2 checkpoint-3 checkpoint-4 checkpoint-4-result; do
  printf '%-22s ' "$b"
  if [ -z "$(git diff main $b -- SPEC.md)" ]; then echo IDENTICAL; else echo "DIFFERS — FIX"; fi
done
```

Expected: `IDENTICAL` five times.

- [ ] **Step 3: Verify the frozen branches did not move**

```bash
diff <(git rev-parse dotnet-demo nodejs-demo) \
     /private/tmp/claude-502/-Users-mohammadjunaid-Documents-GitHub-spec-driven-demo/94fa47de-21a3-4d41-8849-c498bb43f87e/scratchpad/frozen-shas.txt \
  && echo "dotnet-demo and nodejs-demo unchanged"
```

Expected: no diff output, then the confirmation line.

- [ ] **Step 4: Verify no design docs leaked and checkpoint-4 has no app**

```bash
for b in main checkpoint-1 checkpoint-2 checkpoint-3 checkpoint-4 checkpoint-4-result; do
  printf '%-22s docs:%s\n' "$b" "$(git ls-tree -r --name-only $b | grep -c '^docs/superpowers' || true)"
done
git ls-tree -r --name-only checkpoint-4 | grep -E '^(AssetDesk|reference)/' || echo "checkpoint-4 holds no app — correct"
```

Expected: `docs:0` on all six, and the confirmation that checkpoint-4 holds no app.

- [ ] **Step 5: Verify the skill layering is exactly additive**

```bash
for b in main checkpoint-2 checkpoint-3 checkpoint-4 checkpoint-4-result; do
  printf '%-22s %s\n' "$b" "$(git ls-tree -r --name-only $b | grep -oE '\.claude/skills/[a-z-]+' | sort -u | tr '\n' ' ')"
done
```

Expected:
- `main`, `checkpoint-1` → `review-build`
- `checkpoint-2`, `checkpoint-3` → `api-boundary review-build`
- `checkpoint-4`, `checkpoint-4-result` → `api-boundary blazor-component-hygiene csharp-quality review-build`

- [ ] **Step 6: Verify the switch ritual actually works**

```bash
git switch -c work/verify checkpoint-1
mkdir -p AssetDesk && echo 'scratch' > AssetDesk/Fake.cs
git add -A && git commit -q -m "verify: simulated build"
git switch checkpoint-2 && echo "switch clean, no rm -rf needed"
ls AssetDesk 2>&1                       # expect: No such file or directory
git switch work/verify && ls AssetDesk  # expect: Fake.cs — the build is recoverable
git switch checkpoint-2 && git branch -D work/verify
```

Expected: the switch succeeds with an uncommitted-then-committed build present, `AssetDesk/`
disappears on `checkpoint-2`, and the work is recoverable on `work/verify`. This is the exact claim
`CHECKPOINTS.md` makes to students.

- [ ] **Step 7: Confirm the design doc already carries its two corrections**

Both were applied while this plan was being written — no edit needed here, only a check that they are
present, since the reviewer in Step 8 will be scoring against them.

```bash
git switch checkpoints-setup
grep -c 'compared as a string literal' docs/superpowers/specs/2026-08-04-checkpoint-branches-design.md   # 1
grep -c '250 lines' docs/superpowers/specs/2026-08-04-checkpoint-branches-design.md                      # 1
git switch main
```

Expected: `1` and `1`. For the record, the two corrections were:

- **Q5 narrowed to `Status` only.** The design doc originally said "status **and tab**", but no `Tab`
  enum exists in `SPEC.md` and introducing one would violate §10 item 7 (`If you want to add something
  not in this spec, don't`).
- **The component-size tripwire moved from 150 to 250 lines.** `AssetsTable.razor` is legitimately
  ~246 lines, so a 150-line cap would have made `checkpoint-4-result` — the app that is supposed to
  score zero — violate its own skill.

- [ ] **Step 8: Run the three reviews and record the counts**

This is the one check that proves the demo works. It needs an interactive session — run it by hand,
restarting Claude Code on each branch so the skills register:

```bash
git switch checkpoint-3
# /exit, then: claude
#   /review-build reference/AssetDesk-noskills   → expect BOUNDARY 5, QUALITY 5, TOTAL ~10
#   /review-build AssetDesk                      → expect BOUNDARY 0, QUALITY 5, TOTAL ~5
git switch checkpoint-4-result
# /exit, then: claude
#   /review-build AssetDesk                      → expect BOUNDARY 0, QUALITY 0, TOTAL 0-1
```

Record the three count blocks. If the boundary count on `checkpoint-3/AssetDesk` is not zero, Task 7
missed a rule. If the no-skills quality count differs from the api-boundary app's, the two apps'
planted issues have drifted apart and rung 2 stops being a controlled comparison — reconcile them
before teaching.

- [ ] **Step 9: Report, and do not push**

Summarise for the user: the six branches, the three recorded count blocks, and the two open items from
the spec — pushing (needs their authorization) and the optional live checkpoint-4 build to confirm the
skills produce the clean result under the real prompt.

```bash
git branch -v
git status --short
```

Expected: all branches listed, working tree clean. **No `git push` in this task or any other.**

---

## Notes for the implementer

**The two things most likely to go wrong.**

1. *The self-calling HttpClient in Task 7.* `curl` proving `/api/state` works does **not** prove the UI
   can reach it — they are different code paths. The base address comes from
   `IServerAddressesFeature`, which is only populated once Kestrel is listening, so a mistake here
   shows up as a UI that renders empty tables while `curl` looks perfect. Always open the browser.
2. *Q1–Q5 drifting between Task 6's app and Task 7's.* The whole rung-2 comparison rests on the two
   apps differing by architecture alone. Task 7 Step 7 is not a paraphrase of Task 6 — copy the code.

**Why there are no unit tests.** `SPEC.md` §2 line 30 forbids them, and these apps are the artefacts
students review against that spec. A test project in the tree would itself be a finding.

**Branch parents, since three tasks get this wrong easily.** `checkpoint-2` from `checkpoint-1`;
`checkpoint-3` from `checkpoint-2`; `checkpoint-4` from **`checkpoint-2`, not `checkpoint-3`**;
`checkpoint-4-result` from `checkpoint-4`.
