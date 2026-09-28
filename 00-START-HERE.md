# START HERE — Claude Code Day 2 · Indus Motor

Everything needed to deliver Day 2. Read this page, then follow the checklist. Day 1 is the prerequisite: attendees arrive knowing plan mode, subagents, CLAUDE.md, skills, hooks, headless `-p`, and MCP concepts.

**Day 2 in one line:** the spec writes the app. Attendees build a full .NET 10 Blazor app from a 625-line spec, review it with an agent that never saw it being written, change it with a second spec (test first), get a Claude review on their pull request in GitHub Actions, and connect two ready-made MCP servers: Microsoft Learn for docs and Playwright, which gives Claude a browser to check the app's screens.

Two threads run through every block: **independent review** (a subagent that never saw the work reviews it) and **give Claude a way to check its work** (a done-checklist, tests before code, a browser).

## What's in the folder

| Folder | Who uses it | What's inside |
|---|---|---|
| `01-Instructor/` | You only | Pre-delivery checklist, minute-by-minute runbook, answer keys, troubleshooting, screenshot shot list, reference solutions for every lab, a finished reference build |
| `02-Slides/` | You, on the projector | 48-slide deck in 10Pearls branding. `.pdf` to project, `.pptx` for presenter view (speaker notes on every slide), `source/` to edit and rebuild |
| `03-Send-Before-Workshop/` | Attendees, 5 days before | Pre-work email: GitHub access, install, clone, run the setup check |
| `04-Hand-To-Students/` | Attendees, on the day | Lab guide, quick reference, Quiz A, Quiz B, take-home exam, stretch cards, feedback form |
| `05-Lab-Repo/` | Push to GitHub | `AssetDesk-Lab/` (the student repository) and `make-lab-repo` scripts that turn it into a git repo with a `start` tag and a `reference-build` branch |
| `06-Follow-Up/` | You, 30 days later | Adoption check survey and report template |

## Do this, in order

| When | Do | File |
|---|---|---|
| T-10 days | Confirm licences (Max or Team recommended), network allow-list, a GitHub org you control, a Console API key with a spend limit | `01-Instructor/01-Pre-Delivery-Checklist.md` |
| T-7 days | Run `05-Lab-Repo/make-lab-repo.sh`, push to a private repo, install the Claude GitHub App, add the secret | Checklist "T-7", `01-Instructor/Lab-Solutions/lab5/SETUP.md` |
| T-5 days | Fill in the date, room and repo URL; send the email; collect GitHub usernames | `03-Send-Before-Workshop/Pre-Work-Email.md` |
| T-3 days | Full dry run of every lab, including one real PR review in Actions; take the screenshots | Checklist "T-3", `05-Screenshot-Shot-List.md` |
| T-1 day | Create the three forms, paste their links into `02-Slides/source/config.json`, rebuild the deck; print handouts | Checklist "T-1" |
| Day | Follow the runbook minute by minute | `01-Instructor/02-Instructor-Runbook.md` |
| Day, 17:00 | Hand out the take-home exam and feedback form | `04-Hand-To-Students/05-…`, `07-…` |
| Day +7 | Grade the take-home exam | `01-Instructor/03-Answer-Keys.md` |
| Day +30 | Send the adoption survey; report | `06-Follow-Up/Day-30-Adoption-Check.md` |

## What to print (one per attendee)
- `04-Hand-To-Students/02-Quick-Reference.md`
- `04-Hand-To-Students/03-Quiz-Form-A.md`
- `04-Hand-To-Students/04-Quiz-Form-B.md`
- `04-Hand-To-Students/06-Stretch-Cards.md`
- `04-Hand-To-Students/07-Feedback-Form.md`

The lab guide does not need printing: attendees open `lab/LAB-GUIDE.md` in the repo. It is identical to `04-Hand-To-Students/01-Lab-Guide.md`.

## Three things only you can supply
1. **The GitHub training repository** in an org you control, with the Claude GitHub App installed and attendees added as writers.
2. **An Anthropic API key** from a dedicated Console workspace with a spend limit, stored as the repo secret `ANTHROPIC_API_KEY`. Lab 5 does not work without it.
3. **Three form links** (Quiz A, Quiz B, feedback) in `02-Slides/source/config.json`. The QR codes regenerate on rebuild.

## The day at a glance

| Time | Block | Slides | Lab |
|---|---|---|---|
| 10:00 | Kickoff, Day 1 bridge, Quiz A | 1–5 | — |
| 10:15 | Why spec-driven | 6–11 | — |
| 10:40 | An executable spec, then launch the build | 12–17 | Lab 1 |
| 11:20 | While it builds: writing specs with Claude | 18–23 | Lab 2 |
| 12:15 | Definition of done, clause by clause | 24 | check |
| 12:30 | Lunch | 25 | — |
| 13:30 | Verify: a reviewer who wasn't in the room | 26–29 | Lab 3 |
| 14:10 | The principles in real teams | 30–34 | Lab 4 |
| 15:05 | Break | 35 | — |
| 15:15 | Claude reviews every PR | 36–41 | Lab 5 |
| 16:05 | MCP, hands-on | 42–45 | Lab 6 |
| 16:35 | Monday, Quiz B, commitments | 46–48 | — |

The build started in Lab 1 runs unattended for 60–80 minutes. Blocks 4 and the DoD check are designed to happen while it runs. Anyone whose build stalls switches to the `reference-build` branch and loses nothing.

## Facts checked against the Claude Code docs (September 2026)
If you rehearse on a much later version, re-check these:
- CI uses `anthropics/claude-code-action@v1`. It needs `id-token: write` and either `ANTHROPIC_API_KEY` or `CLAUDE_CODE_OAUTH_TOKEN` (from `claude setup-token`).
- `/install-github-app` needs repo admin and the `gh` CLI; it installs the app, stores the secret and opens a PR with the workflow.
- Fork PRs do not receive secrets. Never pair `pull_request_target` with a checkout of the PR head.
- Anthropic's managed **Code Review** (Team/Enterprise, research preview) is a separate, separately billed product; the labs use the Action.
- MCP: `claude mcp add --transport http microsoft-learn https://learn.microsoft.com/api/mcp` needs no install or login. `claude mcp add playwright -- npx @playwright/mcp@latest` needs Node.js 18+ (native Windows: `cmd /c npx …`) and uses the installed Chrome (`--browser msedge` otherwise). Both tested on 2026-09-28. Default scope is local; `--scope project` writes `.mcp.json`. Tools appear as `mcp__<server>__<tool>`; new servers load in a new session.
- A build-your-own C# MCP server (`ModelContextProtocol` NuGet package) is kept as an optional instructor stretch in `01-Instructor/Lab-Solutions/stretch-mcp-server/`.
- Skills and commands register at session start. After copying a skill in, `/exit` and relaunch; `/clear` is not enough.
- Day 1 conventions still hold: `/review` is an alias of `/code-review`; hooks block only on exit code 2; file permission rules use `Edit(...)`.

## Where the Day 2 prep work went
Earlier drafts of Day 2 used four checkpoint branches in this repository. Their content lives on here: the checkpoint-3 build is `01-Instructor/Lab-Solutions/reference-build/`, the three skills are `05-Lab-Repo/AssetDesk-Lab/lab/skills/`, and the lessons are slides 20 and 26–29. The old branches are preserved as `archive/*` tags.
