# Instructor Runbook — Claude Code Day 2

Minute-by-minute. Each block lists the slides, what to say, what to demo (exact commands), the lab, how to check the room, and what to cut if you are late. The Say lines come from the speaker notes in the deck; the deck wins if they ever differ.

Claude Code version rehearsed at T-3: `________` (fill in from `claude --version`).

**Your setup at the podium:** two clones of the **training repository** (not this package), both signed in, font size 18 or larger.

| Clone | Branch | Terminals | Used for |
|---|---|---|---|
| `~/podium/assetdesk-live` | `build/instructor-live`, created at 10:58 from `main` | T1 (Claude), T2 (shell) | Your own live build during Lab 1, so the room sees a real T1 |
| `~/podium/assetdesk-ref` | `build/instructor` (reference build, pushed at T-1); `feature/instructor-asset-history` ready and pushed | T3 (Claude), T4 (shell) | The DoD demo, `/review-build`, the Lab 5 PR, the MCP demo |

On Windows use `C:\podium\...`. Prepare both at T-1 (checklist "T-1").

**For the MCP demo (Block 8):** Node.js 18 or later and Google Chrome on the podium machine. At T-1, run `npx @playwright/mcp@latest --help` once so the package is downloaded and cached; on the day the first start is then fast. Do **not** pre-add the two servers: you add them live at slide 43. If a dry run left them registered, remove them (`claude mcp remove microsoft-learn`, `claude mcp remove playwright`, run in `assetdesk-ref`).

Keep open in the browser: the training repository's **Actions** tab, its **Pull requests** tab, and `01-Instructor/03-Answer-Keys.md` on your own screen only.

**Timing discipline:** the clock on each block is the start time. If a block overruns by more than 5 minutes, use its **Cut** line. Never debug one attendee's laptop in front of the room: pair them with a neighbour and fix it at the next break. The two protected moments are the Lab 1 launch (by 11:00, or the build does not finish before lunch) and the Lab 5 release (by 15:45, or reviews are still queued at 16:05).

---

## 09:30 — Before the room arrives (30 minutes)

- [ ] Projector shows slide 1. Presenter view shows notes (`02-Slides/ClaudeCode-Day2-IndusMotor.pptx`).
- [ ] Printed Quiz Form A on every seat, face down. Quick Reference and Stretch Cards on every seat.
- [ ] Whiteboard corner: `Form A avg: ___   Form B avg: ___   Commitments:`. A second corner: `Lab 3 TOTAL: #1 / #2`.
- [ ] `assetdesk-live` is on `main`, clean: `git switch main; git status`. No `AssetDesk/` folder exists.
- [ ] `assetdesk-ref` is on `build/instructor`: `dotnet build AssetDesk` gives `0 Warning(s)` (the planted `NoWarn` hides one). Reset its database.
- [ ] Nothing listens on 5198: `Get-NetTCPConnection -LocalPort 5198 -State Listen` (macOS: `lsof -nP -iTCP:5198 -sTCP:LISTEN`) prints nothing.
- [ ] `claude -p "Reply with the single word ok"` returns `ok` on the podium machine.
- [ ] MCP demo ready: `node --version` is 18 or later; `npx @playwright/mcp@latest --help` prints its help within a few seconds (cached); in `assetdesk-ref`, `claude mcp list` shows neither `microsoft-learn` nor `playwright`.
- [ ] The Actions tab shows your T-3 dry-run of **Claude review** green, the `ANTHROPIC_API_KEY` secret is still set, and the Console workspace has budget left.
- [ ] The Lab 4 reference tests (`Lab-Solutions/lab4/AssetDesk.Tests.csproj`, `AssetHistoryTests.cs`) are on a USB stick or a shared folder the room can reach.
- [ ] Wi-Fi works for attendees: ask the first three arrivals to run `claude -p "say ok"`.
- [ ] Co-trainer (if any) has the Form A key and knows the lunch walk-round job.

---

## 10:00 · Block 1 — Kickoff, Day 1 bridge, Quiz A (15 min) · Slides 1–5

**Slide 1 (1 min):** "Hands up if you have used Claude Code since yesterday." Count. "Yesterday the repo got smart. Today the spec writes the app." Then: "By five o'clock each of you will have built a full .NET app from a document, reviewed it with an agent that never saw it being written, changed it with a second document, had Claude review your pull request in GitHub, and plugged Claude into the running app with MCP."

**Slide 2 (1 min):** "Morning is the spec and the build. Afternoon is everything that happens after the first build: review, change, CI, and MCP." "The build runs for about an hour on your machine. We don't watch it. We use that hour to learn how to write the next spec."

**Slide 3 (1 min):** "Nothing today is a new feature of the tool. It is yesterday's habits pointed at a bigger job." Point at MCP and `claude -p`: "Yesterday's last MCP slide promised you would connect one. That is Lab 6, and you connect two: official docs and a browser. Yesterday's headless slide promised a CI review. That is Lab 5."

**Slide 4 (1 min):** "Yesterday we launched in Manual mode all day so you saw every action. Today one session builds for an hour; you approve a plan once and review in git, milestone by milestone." "The fallback branch exists so nobody spends the afternoon watching a spinner. Using it is not failing." Tell them to open three terminals now, all in the repository root.

**Slide 5 (10 min):** "Paper or phone, your choice. It measures the room, not you. Ten minutes." Collect at **10:13**.

**While Block 2 runs:** a co-trainer scores Form A with `03-Answer-Keys.md` and writes the room average on the board. Alone? Score at lunch.

**Cut:** slide 3, read the six violet labels only.

---

## 10:15 · Block 2 — Why spec-driven (25 min) · Slides 6–11

**Slide 6 (4 min):** "Both of these can produce an app. Only one of them produces the same app twice, survives the author going on leave, and can be argued with before code exists." "The prompt at the bottom is the one you type in Lab 1. It is eight words. All the engineering is already done, in a file."

**Slide 7 (5 min):** "Imagine someone walks you through procurement, payroll, inventory and tax for four hours with no notes. By hour three you are not stupid. You are full." Walk the four failure modes. Stop on the fourth: "It doesn't feel wrong from the inside." Close: "Yesterday's fixes still apply: /context, /clear, subagents. Today adds the big one: put durable facts in a file."

**Slide 8 (3 min):** "Nobody's version drifted because their session went long, and nobody's is better because they explained it more patiently." "That is also why a spec can be reviewed: it exists before the code, as a diff someone can comment on."

**Slide 9 (4 min):** "Neither one is better. The mistake is staying in exploration mode for work that three other people will have to live with." **Ask:** "Which of your current tickets is in the wrong column?" Take one answer.

**Slide 10 (4 min):** "Four stages, four gates. The agent does most of the typing; a human owns every gate." "The arrow from Verify back to Specify is the one teams skip. When the review finds something the spec never said, the fix is a sentence in the spec, not just a patch in the code."

**Slide 11 (4 min):** "This slide keeps the rest of the day honest. Spec-driven is not a religion." Read all four. Stress number four: "If nobody can answer the interview questions, the spec is a confident guess, and the agent will build the guess perfectly."

**Cut:** slide 9, skip the question. Slide 7, read the four headings only.

---

## 10:40 · Block 3 — An executable spec, then launch the build (40 min) · Slides 12–17 · Lab 1

**Slide 12 (4 min). Do:** open `SPEC.md` on the projector next to the slide (from `assetdesk-live`) and scroll to §2. **Say:** "The longest sections are the data model and the screens. The most important one is twenty lines: the non-goals. Every item there is something a capable agent would add by default." Read the last line of §2 aloud: "If something is not described in this document, do not build it." "That one sentence removes a whole category of review findings."

**Slide 13 (3 min):** "Read the grey lines. Every one of them has been in a requirements document you have received this year." **Ask:** "What would two different developers build for 'handle errors gracefully'?" Take two answers. "Now two agents. Same problem, faster."

**Slide 14 (3 min):** "M0 and M1 finish before a single component exists. So a bug found later is either in the repository or in the UI, never ambiguously both." "At 12:15 we walk the §9 list together. 'The agent said it's done' is not on it."

**Slide 15 (3 min):** "An agent that stops to ask you twenty questions during an unattended build is useless. An agent that silently guesses is dangerous. §10 asks for the third option: guess the simplest thing, and say so in one line." "The right-hand side is illustrative. Your assumptions will differ. Write yours down during Lab 1; we use them at 12:15."

**Slide 16 (4 min). Do:** open the real `.claude/settings.json`. It also allows `Bash(git log *)`; the file wins over the slide. **Say:** "Yesterday you watched every action in Manual mode. That doesn't scale to an 80-minute build. So we move the review to two places: the plan before, and git after each milestone." "The deny on SPEC.md is the most important line. An agent under pressure to pass a check will happily move the check. If it wants to change the contract, that is a human pull request."

**Slide 17 — release Lab 1 at 10:58 (20 min).** "Open `lab/LAB-GUIDE.md`, Lab 1. Read before you run: five minutes on §1, §2, §8, §9, §10." "Push back once on purpose, even if the plan is good. Ask it to confirm the two-package rule, or to commit after each milestone. Watch the plan change."

**Live demo alongside the room (in `assetdesk-live`, T1 and T2):**

```
T2> git switch -c build/instructor-live
T1> claude --permission-mode plan
Claude> /permissions                       (show Deny: Edit(SPEC.md), then Esc)
Claude> Read SPEC.md and plan the build. Do not write code yet.
        (read step 1 of the plan aloud; ask the room "data first?")
        choose "No, keep planning", then:
Claude> Confirm the .csproj ends with exactly Dapper and Microsoft.Data.Sqlite, and commit after each milestone with the milestone in the message, for example "M0: models, schema, seed".
        (show the plan change, then choose "Yes, and auto-accept edits")
```

Leave podium T1 running all morning. Glance at it between slides; it is your early warning for the room.

**Walk the room from 11:05.** Look at plans: M0/M1 first, two packages, commits. Approve-button hesitation is normal; nudge.

**Check the room at 11:18:** "Hands up if M0 is running." Anyone not: pair them, or have them wait for the 12:15 fallback and join Lab 2 now.

**Common problems:**
- Status bar says auto mode or manual mode, not plan → they typed plain `claude`. `/exit`, relaunch with `--permission-mode plan`.
- Claude wrote files during "planning" → plan mode was off. Lab 1 "If stuck".
- A permission prompt for `mkdir`, `kill`, `sqlite3` → expected; approve if it stays inside the repo.
- A prompt to delete a `data` folder → **No**. Case-insensitive filesystem: it would delete `Data/` too. See Troubleshooting §2.
- "Trust this folder?" → Yes. Project settings apply only in trusted folders.

**Cut:** drop the reading to §2 and §8 only (slide 17 Cut). Skip slide 13's question.

---

## 11:20 · Block 4 — While it builds: writing specs with Claude (55 min) · Slides 18–23 · Lab 2

**Slide 18 (3 min):** "These are the spec's own estimates. Yours will run faster or slower. What repeats is the order." "Glance at T1 between slides. One line per milestone. If it goes quiet for more than ten minutes, or starts working on UI before M1 is done, raise a hand." **Do:** point at podium T1.

**Slide 19 (4 min):** "The interview prompt is straight out of Anthropic's Claude Code best-practices guide. Claude asks with multiple-choice options; you pick or type your own." "Answer the way the business would: 'IT wants to know who had this laptop before it broke'. Let Claude translate that into a table and an endpoint. Then check the translation."

**Slide 20 (4 min):** "This is the real gap in today's spec. When we ran this build repeatedly, about half the review findings traced back to one missing sentence: how the UI reaches its data. One skill decides it; you'll see it in Lab 3's stretch." "Why a subagent? You wrote the spec, so you read what you meant, not what you wrote. The subagent only has what you wrote." The right-hand list is illustrative; the first line is the real observed gap.

**Slide 21 (3 min):** "Yesterday's Block 8 table, extended by one column. The spec is the new column." "The last row is the one that matters for review: a rule you only ever said out loud is invisible to every audit, human or agent."

**Slide 22 (3 min):** "Today's SPEC.md says 'No tests'. The feature needs tests. So the feature spec amends §2, for this feature only, in writing. That is how a contract changes: visibly." **Do:** open `lab/templates/feature-spec.md` for ten seconds. Point at the nine headings.

**Slide 23 — release Lab 2 at 11:40 (30 min).** "Two Claude sessions, one folder. That's safe here because this session only writes one markdown file, and the build never touches specs/." "Don't commit from T3. The build commits after each milestone and may pick the file up; that's fine, it belongs on your build branch anyway. If it's still untracked at 12:15, commit it then." "Answer at least one question with 'Other' and your own words. The interview is only as good as your answers." "The last two steps are an independent review of a document: a subagent that never saw your conversation reads only what you wrote." Four steps in all: get interviewed, read the draft, the one-line ambiguity hunt, fix two of what it finds.

**Optional 2-minute demo** if the room is slow to start: show `01-Instructor/Lab-Solutions/lab2/interview-transcript-sample.md` on the projector, Q1 and Q2 only.

**Walk the room.** Watch for: three questions in one message ("one at a time"), no recommendation in the options, drift into code ("behaviour only"). Use `lab2/interview-transcript-sample.md` and `lab2/ambiguity-hunt-sample.md` as your reference. The reference answer is `lab2/asset-history.md`.

**Check at 12:10:** "Hands up if you have a spec file." Anyone whose spec is still untracked commits it on the build branch at 12:15 (T2: `git add specs/asset-history.md`, `git commit -m "Asset history spec"`). Then collect the most common 'still open' item and read it out. It is usually one of: ordering inside one second, seeded assets' history, tag or GUID in the route. Point at the last one: "That one breaks the Lab 4 curl check if you leave it open."

**Common problems:**
- Pro plans hit usage limits running two sessions → pause T3, finish the spec by hand from the template (slide 23 note).
- T3 edits anything other than `specs/asset-history.md` → Esc: "Only write specs/asset-history.md. Touch nothing else." `git status` in T2 shows what it changed.
- T3 opened in the wrong folder → its banner path must be the repository root, the same as T1.
- Someone made a worktree out of Day 1 habit → fine, as long as the spec ends up committed on the build branch. Troubleshooting §4.
- Someone's build in T1 stopped to ask a question → answer it with "pick the simplest reading, state it, continue" (§10.6).

**Cut:** skip the ambiguity hunt; do it at the start of Lab 4 instead (slide 23 Cut). Skip slide 21.

---

## 12:15 · Let it prove done. Then check one yourself (15 min) · Slide 24

**Say:** "Give Claude a way to check its work. That is Anthropic's own best-practice advice for Claude Code. Without it, 'done' just means the agent stopped." Point at the ladder: "It goes from weakest to strongest. A build proves it compiles. Tests and the API prove behaviour. The screen proves what the user sees, and this afternoon Claude gets a browser for that." "Claude runs the checks and shows the output. You read the evidence, and you do one check yourself, because trusting evidence you never looked at is the same as trusting 'done'." "A repair row has no Assign button, so force the error with two tabs: open Assign on AST-1003 in one, Mark repair in the other, then confirm the assignment. The banner must say 'AST-1003 is in repair and cannot be assigned.' If it is empty, you have found the most common defect in generated Blazor code. Hold that thought until 13:30."

**Demo on the projector (in `assetdesk-ref`, T3).** Reset the database first, and make sure nothing listens on 5198:

```
T4> Get-ChildItem AssetDesk -Recurse -Filter 'assetdesk.db*' | Remove-Item -Force
    (macOS: find AssetDesk -name 'assetdesk.db*' -delete)
T3> claude --permission-mode default
Claude> Walk SPEC.md §9, item by item. For each item run a command that proves it and show the evidence. Do not fix anything. If you start the app, use port 5198 and stop it when you are done.
```

Approve its commands as they come (`dotnet build`, `dotnet run`, `curl`). Point at the evidence as it lands: `0 Warning(s)`; `/api/state` with 5 employees and 12 assets after the reset; the invariant 5 `curl` with a made-up employee id coming back 400 with a §4.6 message. Point at the items it marks as not proven, typically "clicking a sidebar tab" and "every §5 acceptance criterion clicked through by hand": "It has no browser. Honest answer. At four o'clock we give it one."

**Then the human spot-check (2 min).** In the browser, on `http://localhost:5198`: open Assign on AST-1003 in one tab, Mark repair on AST-1003 in a second tab, then confirm the assign in the first. A row in repair has no Assign button (SPEC.md §5.2), which is why it takes two tabs. The reference build shows an **empty** red box. That is planted defect 1 (`reference-build/PLANTED-DEFECTS.md`) and §9 "every repository error surfaces verbatim" just failed, whatever Claude's walk said about it. Say only: "Write it down. Lab 3 finds it." Put AST-1003 back in stock. Ask Claude to stop the app it started (or stop it yourself: Troubleshooting §3).

**Do:** "Once your build has finished, type `/clear` in T3 and run the same prompt there. T3 didn't write the app, so it has no stake in saying it's done. Then do the two-tab check yourself. The lab guide has both."

**Do at 12:25:** anyone whose build is not at M6 commits, then runs `git switch -c build/<name>-ref origin/reference-build` before lunch. Their committed spec does not come with them: `git checkout build/<name> -- specs/asset-history.md`, then commit it on `build/<name>-ref`. Watch for anyone typing `git switch reference-build`: that puts them on the shared branch. Never let the room commit to `reference-build` itself.

**Common problems:**
- Build still running in T1 → they don't run the walk yet: it builds and starts the app, which clashes with the build (file locks, port 5198). They run it at 13:25.
- Claude starts fixing what it finds → Esc: "Do not fix anything. Report only."
- Claude says an item passes without showing output → "Show the command and its output for that item."
- `address already in use` on 5198 → the build agent, or the §9 walk, left its own `dotnet run` alive. Troubleshooting §3.
- `/api/state` shows 13+ assets → old database. Reset by name.
- `NU1903` breaks zero warnings → known transitive advisory; Troubleshooting §2.

**Cut:** show Claude's walk only; everyone does the two-tab spot-check after lunch.

---

## 12:30 — Lunch · Slide 25

**Say:** "If your build is still running, let it. Lock your screen, don't close the terminal."

**Do during lunch:** walk the room with the co-trainer. Anyone without a working build: help them switch to `build/<name>-ref` so Lab 3 works for them. Score Form A if not done. Note how many builds reached done without the fallback (for `06-Follow-Up`).

---

## 13:30 · Block 5 — Verify: a reviewer who wasn't in the room (40 min) · Slides 26–29 · Lab 3

**Slide 26 (3 min):** Point at the kicker, "Independent review". **Say:** "Never let the author grade the exam. Independent review means the reviewer never saw the code being written." "Yesterday: a subagent inherits nothing from your conversation. That was a limitation in Block 6. Here it is the whole point." "The skill is short and it says why: if the same context reviews its own code, it reviews its own reasoning, and the counts stop being comparable." "You have already seen this idea once today, in the ambiguity hunt. You will see it again at 15:15, in CI. Same idea, three places."

**Start the podium review now** so its result is ready by slide 29 (in `assetdesk-ref`, T3; `/exit` the 12:15 session first so the reviewer's parent session is clean):

```
T3> claude --permission-mode default
Claude> /review-build
```

**Slide 27 (3 min). Do:** open `.claude/agents/assetdesk-reviewer.md` on the projector. Point at "Report only what you can point at with a file and a line." **Say:** "Rule 3 is the one humans break most. Flagging something the spec forbids means you are reviewing the spec, not the build."

**Slide 28 (4 min):** "We ran this build many times with one variable: which skills were in .claude/. Numbers are the typical shape from those dry runs. A live build is not deterministic; what repeats is which bucket moves." "The twist is the lesson. An audit that finds nothing has told you about its rulebook, not about your code."

**Show the podium count block** (typical on the reference build: `QUALITY: 0`, `SPEC: 3`). Write it on the board as row #1.

**Slide 29 — release Lab 3 at 13:40 (25 min).** "This is an independent review. /review-build hands the job to a subagent with read-only tools and no memory of the build." "Review, add the rules, review again, then stop and compare the two count blocks. Fixing the findings is a stretch, not the lab." "Read both skills before you copy them. They are short, and each rule shows the wrong code next to the right code. That's why they work on a model and on a new hire." "/clear is not enough after adding a skill. Skills register at startup. /exit, then claude." "Commit the skills: Lab 5's CI review reads them from your build branch."

**Podium, alongside the room (T4 then T3):**

```
T4> Copy-Item -Recurse lab/skills/csharp-quality .claude/skills/
T4> Copy-Item -Recurse lab/skills/blazor-component-hygiene .claude/skills/
    (macOS: cp -r lab/skills/csharp-quality .claude/skills/ ; cp -r lab/skills/blazor-component-hygiene .claude/skills/)
T4> git add .claude/skills
T4> git commit -m "Lab 3: add the quality skills"
T4> git push
Claude> /exit
T3> claude --permission-mode default
Claude> /review-build
```

Do **not** fix the podium findings: the Lab 5 demo PR needs a base with its defects intact.

**Check at 14:05:** ask three people to read their two TOTAL lines aloud. Write them on the board. Point at the column that moved: QUALITY.

**Common problems:**
- QUALITY still 0 in review #2 → no restart, or a nested folder (`.claude/skills/csharp-quality/csharp-quality`). Troubleshooting §5.
- Main session reviews the code itself → "Use the review-build skill. Do not review it yourself."
- Counts differ between neighbours → expected; compare direction, not numbers.
- Claude tries to copy the skills itself and is refused → `Edit(lab/**)` deny; the human copies in T2.

**Stretch:** the lab guide's optional "fix the findings" steps (QUALITY falls in a third review, and the empty red box now says why); or copy `lab/skills/api-boundary` too and ask the reviewer for BOUNDARY findings only.

**Cut:** slide 27, skip opening the agent file and read rule 3 from the slide. Release the lab by 13:45 at the latest.

---

## 14:10 · Block 6 — The principles in real teams (55 min) · Slides 30–34 · Lab 4

**Slide 30 (4 min):** "AssetDesk was a green field so the lesson was clean. Your Monday is not. Everything you learned still applies, but at the size of a ticket." "Rule of thumb from teams that do this: if the delta spec runs past three pages, it's two changes. Split it."

**Slide 31 (4 min):** "The first spec of a legacy system describes what it does, including its bugs. Humans then decide which behaviours are rules and which are accidents. Only then does it become a contract." "'Cite file:line' and 'mark INFERRED' are what make it reviewable. Without them it is a confident essay."

**Slide 32 (4 min):** "Two pull requests per feature. The first one is a document, and the right reviewers are different people: a BA or product owner for 'is this what we want?', a tech lead for 'is it buildable?'." "The second PR is reviewed against the first. That's the job CI does in Block 7, and a human confirms." **Ask:** "Who in your team would review the spec PR?" If nobody: "That's the gap to close first."

**Slide 33 (3 min):** "Every row here is something we hit while preparing today. The fixes are cheap if you decide them on day one and expensive if you discover them in month three." "Spec drift is the one to watch. A spec that is wrong is worse than no spec, because the next agent will obey it."

**Slide 34 — release Lab 4 at 14:25 (35 min).** "Your spec is already on your build branch; `git status` should show it committed. Branch the feature from there: `git switch -c feature/<name>-asset-history`. No merge." "Tests first is the other half of 'give Claude a way to check its work'. The tests exist before the code, so Claude can't write tests that merely agree with what it built." "Because SPEC.md fixed the repository's method names, a test written against the spec compiles against your build. The contract made the tests portable." "Ask for the failing-tests commit explicitly. If Claude writes code and tests together, you have lost the proof that the tests can fail." "There is no solution file: `dotnet test AssetDesk.Tests`, not bare `dotnet test`."

**Walk the room with `Lab-Solutions/lab4/NOTES.md`.** Expect 15–25 minutes to green: plan 3–5, tests 3–5, implementation 5–10, UI 3–5.

**Getting a stuck attendee unstuck** (from NOTES, stop at the first that works):
1. Red for the wrong reason: read the first compile error aloud; if it is a name, "use the exact names in §4 of the spec".
2. No tests after 10 minutes: copy in the reference tests, then prompt Claude to implement until they pass without editing them:
   ```
   T2> mkdir AssetDesk.Tests
   T2> (copy lab4/AssetDesk.Tests.csproj and lab4/AssetHistoryTests.cs into it from a USB stick or a shared folder)
   Claude> The tests in AssetDesk.Tests are the acceptance criteria. Implement until dotnet test AssetDesk.Tests passes. Do not edit the tests.
   ```
   The tests assume `new AssetRepository(string connectionString)`. If the build's constructor differs, ask Claude to adapt the test fixture's one constructor line, not the repository.
3. One test red: paste its name and message with "This is AC-n in specs/asset-history.md. Fix the implementation, not the test."
4. The build is too broken to extend: commit, `git switch -c build/<name>-ref origin/reference-build`, bring the spec across (`git checkout build/<name> -- specs/asset-history.md`, commit), then branch the feature. `lab4/reference-implementation.patch` applies **only** to the reference build (it touches `Dtos.cs` and `AssetDeskClient.cs`); use it only with under 10 minutes left. Attendees on their own builds get the spec and the tests, never the patch.
5. Out of time: push whatever passes. Lab 5 reviews a partial feature just as well.

**Check at 14:55:** "Hands up if `dotnet test` is green." Stuck: see above.

**Do at 15:00:** "Push the feature branch before the break: `git push -u origin feature/<name>-asset-history`. Lab 5 needs it."

**Common problems:** `MSB1003` (bare `dotnet test`), history route called with the tag, `database is locked`, test packages added to `AssetDesk.csproj`, tests opening the real database. All in Troubleshooting §6.

**Cut:** slide 31 (point at the stretch card instead). Drop Lab 4's browser check.

---

## 15:05 — Break · Slide 35

**Do:** fix broken laptops now, never in front of the room. Check that everyone has pushed a feature branch: the repository's branch list on GitHub should show one `feature/*-asset-history` per attendee. Anyone missing: push it now, partial or not.

**Do:** in `assetdesk-ref`, confirm `feature/instructor-asset-history` is pushed and `build/instructor` has the skills commit on GitHub.

---

## 15:15 · Block 7 — Claude reviews every PR (50 min) · Slides 36–41 · Lab 5

**Open the podium PR first (1 min)** so the first run of the day starts while you talk:

```
T4> gh pr create --base build/instructor --head feature/instructor-asset-history --title "Asset history" --body "Demo PR for Lab 5."
```

(Or the web UI: base `build/instructor`, compare `feature/instructor-asset-history`.) Put the Actions tab on the projector.

**Slide 36 (4 min):** "Nothing new under the hood. It's Claude Code with -p, inside a GitHub runner, started by an Action Anthropic maintains." "Notice the last box. The review is a check on the PR. It never approves and never merges."

**Slide 37 (5 min). Do:** open the real `.github/workflows/claude-review.yml`; the file wins over the slide. **Say:** "Day 1's rule: a tool list is enforcement. --allowedTools is that rule in CI. The reviewer can read the PR and comment on it. It can't push, can't merge, can't run your build." Point at two lines in the file: `if: github.event.pull_request.draft == false` (drafts are skipped) and the comment that the action restores `.claude/` from the base branch: "a PR cannot rewrite the rules it is reviewed by. That's why you committed the skills to your build branch."

**Slide 38 (5 min):** "Same five rules as the reviewer this afternoon: a line, a clause, no taste. The only difference is where it runs." "The comment on the right is illustrative. The clause citation is what makes it arguable: you can reply 'AC-4 doesn't say that' and the discussion is about the contract, not opinions." **Do:** if the podium run has finished, switch to the PR and show one real inline comment and the summary count block.

**Slide 39 (5 min):** "For a team repo, prefer an API key from a dedicated Console workspace with a spend limit. You can see exactly what CI costs and cap it." "The managed Code Review is a separate product for Team and Enterprise: it runs on Anthropic's infrastructure and is billed separately. The Action runs on your runners with your key. Today we use the Action because you can read every line of it."

**Slide 40 (5 min):** "Yesterday's MCP rule two, again: output is data, not orders. In CI the 'output' is the whole pull request." "pull_request_target runs with secrets in the context of the base branch. Checking out the PR head there hands an attacker your key. If you see it in a workflow, stop and ask."

**Slide 41 — release Lab 5 at 15:40 (20 min).** "Base is your build branch, not main. That way the diff is only the feature and the review takes a minute, not ten." "Push your build branch first: it carries your Lab 3 skills, and the review reads its rules from the base."

**Watch the Actions tab on the projector.** 20 attendees means 20 queued runs; they start within a minute or two and finish in two to five.

**Check at 16:00:** "Hands up if you have a Claude comment on your PR."

**Common problems:**
- No run → PR is a draft, or base is `main`. Look at the PR header.
- Run failed with 401 or "credit balance" → the secret or the Console workspace budget. Fix once for the room; then everyone pushes an empty commit (`git commit --allow-empty -m "rerun review"`, `git push`).
- "Resource not accessible by integration" → Claude GitHub App not installed on the repository.
- "Workflow validation failed" → the attendee edited the workflow in their PR. Revert that file.
- Zero findings → have them plant the swallowed-message violation from the lab guide's "If stuck".
- No QUALITY findings → skills commit not pushed to the base branch.
- Full list: Troubleshooting §7.

**Fallback:** if Actions is slow or blocked for the room, everyone runs the local review from the lab guide (Lab 5 "If stuck", the `gh pr diff <number> | claude -p ...` line). Demo it once on the podium first.

**Cut:** slide 39 to one sentence: "an API key from a Console workspace with a spend limit". Drop the "reply to a finding" step if runs are still queued at 15:58.

---

## 16:05 · Block 8 — MCP, hands-on (30 min) · Slides 42–45 · Lab 6

**Before slide 42, in `assetdesk-ref` T4:** `dotnet run --project AssetDesk --urls http://localhost:5198`. Make sure the podium live build in `assetdesk-live` is not holding 5198. Chrome is closed or idle (Playwright opens its own window).

**Slide 42 (4 min):** "Yesterday: MCP is an open standard; Claude Code is the client, each system runs a server. Today you connect two real ones, and neither needs code or an account." Walk the diagram: "The first is remote: Microsoft runs it, Claude reaches it over HTTP, it searches official .NET docs. The second is local: a small program npx starts on your laptop, over stdio, which drives a real browser pointed at your app." If asked why not a Google search server: those need API keys and billing, and Claude Code already has web search built in, so it would teach nothing new.

**Slide 43 (4 min). "Two commands. No code." Do, live (T3, in `assetdesk-ref`):**

```
Claude> /exit
T3> claude mcp add --transport http microsoft-learn https://learn.microsoft.com/api/mcp
T3> claude mcp add playwright -- npx @playwright/mcp@latest
    (Windows: claude mcp add playwright -- cmd /c npx @playwright/mcp@latest)
T3> claude mcp list
T3> claude --permission-mode default
Claude> /mcp
```

**Say:** "That's the whole integration. The server tells Claude what tools it has and what each one is for; Claude decides when to call them." Open `/mcp` and read one tool description out loud: "That sentence is all the model knows about the tool. When you build your own server one day, the description is the part to get right." "Local scope keeps it to you. Project scope commits a `.mcp.json` so the whole team gets the server, and each person approves it the first time. Secrets never go in that file: use `${VARIABLES}`." "A new server loads in a new session: `/exit`, then `claude`. And Windows needs `cmd /c` in front of `npx`."

**Slide 44 (4 min). "Give Claude eyes." Do (same T3 session):**

```
Claude> Using the Microsoft Learn tools, how do I set the render mode for a whole Blazor Web App in .NET 10? Cite the page.
Claude> Use the Playwright browser to open http://localhost:5198. Click each sidebar tab, then check the acceptance criteria in SPEC.md §5.2. Report pass or fail for each with what you saw. Do not change any code.
```

Approve the tool calls as they come. Point at the `mcp__microsoft-learn__microsoft_docs_search` call and the learn.microsoft.com link in the answer. Then let the browser window open and click through the tabs on its own; stop talking while it does.

**Say:** "This is the same habit as 12:15, one rung higher. Claude checked §9 with commands; now it checks §5 by looking at the screen, and it reports what it saw so you can verify it." "Yesterday's MCP rule two, again: what a server returns is data, not orders. A browser server returns whole web pages, so that rule matters more here. A page can say 'ignore previous instructions'; it's text Claude read, not an order." "And local servers run code: npx runs a package on your laptop. Add the ones you'd install anyway." Point at the Learn call: "It picked that tool because of its description. Descriptions drive tool choice."

**Slide 45 — release Lab 6 at 16:17 (15 min).** "Part A takes two minutes and works on every laptop. Part B needs Node.js from the pre-work. Windows: put cmd /c before npx." "Check the Learn answer against SPEC.md §4.7: it should land on JsonStringEnumConverter. That is you verifying Claude's evidence." "Part B is a check, not a fix: the prompt says do not change any code."

**Check at 16:32:** "Hands up if Claude clicked through your app." Then: "Anyone get a fail? That's a finding. Where does the fix go: the code, or a sentence in the spec?"

**Common problems:**
- `/mcp` does not list the server → it was added while Claude was open. `/exit`, `claude`.
- `playwright` shows failed → `node --version` is missing or below 18; or on Windows the `cmd /c` is missing (`claude mcp remove playwright`, add it again with `cmd /c`).
- `npx` hangs, or `ETIMEDOUT` / `E403` → `registry.npmjs.org` is blocked. That attendee does Part A only and watches the podium's Part B.
- Browser not found → Chrome is not installed. Remove and re-add with `--browser msedge` at the end of the command.
- `ERR_CONNECTION_REFUSED` on localhost:5198 → the app is not running. T2: `dotnet run --project AssetDesk --urls http://localhost:5198`.
- Claude starts editing code → Esc: "Do not change any code. Report only."
- Claude answers the .NET question from memory or web search → "Use the microsoft-learn tools and cite the page."
- Full list: Troubleshooting §8.

**Fallback if npm is blocked for the room:** Part B becomes a podium-only demo. Everyone does Part A; you run the Part B prompt on the podium (on a phone hotspot if the podium is blocked too) and read the §5.2 report aloud. If `learn.microsoft.com` is blocked as well, use the screenshot backups (`05-Screenshot-Shot-List.md`, shots 13–16).

**Early finishers:** Stretch Cards, "After Lab 6".

**Optional, instructor only:** `Lab-Solutions/stretch-mcp-server/` is a tested C# MCP server over the AssetDesk API, with its own `NOTES.md`. It is not part of the lab and not in the student repository. Show it only if the room asks "how would we write our own?" and there is time, or save it for a follow-up session.

**Cut:** at slide 44 show the Learn question only; the room sees Playwright in their own Part B. Release the lab at 16:13.

---

## 16:35 · Block 9 — Monday, Quiz B, commitments (25 min) · Slides 46–48

**Slide 46 (4 min):** "If you remember one thing: the spec is the part of the work that survives. Code gets regenerated; the contract is what you keep." Point at habits 03 and 04: "These two ran through the whole day. Give Claude a way to check its work. And an independent review before a human one." "What's next is Day 3 material if you want it: plugins turn today's .claude folder into something the whole company installs in one command."

**Slide 47 (16:40, 10 min):** "Ten minutes. Same concepts as this morning, new questions." Collect Form B at **16:50**.

1. **16:50** While the co-trainer scores Form B: go round the room for one commitment each: "The spec I will write this month is ___". Write each on the board.
2. **16:56** Write the Form A and Form B room averages side by side. "This morning the room averaged X out of 12. Now it's Y." Never individual scores.

**Slide 48 (16:57, 3 min):** "Feedback takes three minutes and it changes the next cohort's day. The take-home exam is due in seven days." Hand out the take-home exam (`04-Hand-To-Students/05-Take-Home-Exam.md`, printed or emailed now) and the feedback form. The exam is not in the training repository; attendees need the handout.

**After they leave:**
- Photograph the commitments board. File it with both averages and the "builds done without fallback" count in `06-Follow-Up/Day-30-Adoption-Check.md`.
- Stop the podium apps and close the podium PR.
- Check the API spend in the Console workspace and note it for the next cohort's budget.
- Decide with the client when the training repository is archived. Attendees need it for seven days for the exam; delete the secret and revoke the key after that (`Lab-Solutions/lab5/SETUP.md`).
