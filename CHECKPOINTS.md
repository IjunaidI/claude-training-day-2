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

Each checkpoint ships its deck under `slides/`, and later branches carry the earlier ones too.
The decks cover what is happening while the build runs rather than repeating this runbook:
context rot, spec-driven development, writing a spec with AI, and the review kit at checkpoint 1,
then one new layer per checkpoint.

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
