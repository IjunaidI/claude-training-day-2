# Claude Code Day 2 — Lab Guide

Every lab in today's workshop is in this file, in order. Each lab tells you exactly what to type, where to type it, and how to know you are done.

---

## How to read this guide

**Three terminals today.** Open three terminal windows, all in the repository root: the folder you cloned, `assetdesk-lab` in this guide. If your clone has another name, use that.

| Label | What runs there |
|---|---|
| **T1** | Your Claude Code session: the build this morning, every lab after it |
| **T2** | A plain shell for `git`, `dotnet` and running the app |
| **T3** | A second Claude session in the same folder, for Lab 2 and the 12:15 check. After that it becomes a spare plain shell |

**Four kinds of instruction box:**

- **T2 >** (or **T1 >**, **T3 >**) means type this in that shell, not inside Claude.
- **Claude >** means type this at the Claude Code prompt in T1. **Claude (T3) >** is the Claude prompt in T3.
- **Check:** is what you should see. If you don't see it, go to "If stuck" at the end of the lab.

**Windows vs macOS.** Commands are written for Windows PowerShell. Where macOS or Linux differs, the alternative is shown in brackets or in a second block. `git`, `dotnet` and `claude` commands are the same everywhere.

**Output varies.** Claude's wording, plan, and finding counts will not match your neighbour's or the slides. That is normal. The shape of the result is what matters.

**Your name in branch names.** `<name>` is your first name, lowercase, no spaces: `build/aisha`, not `build/<name>`.

**Your build branch.** This guide says `build/<name>`. If you switch to the reference build at 12:15, your build branch becomes `build/<name>-ref`. From then on, use that name wherever you see `build/<name>`.

**The app always runs on port 5198:**

**T2 >** `dotnet run --project AssetDesk --urls http://localhost:5198`

Routes take an asset's `id` (a GUID from `/api/state`), never its tag. `AST-1003` is the only laptop in stock. `AST-1004` is in repair. Aisha Rahman is employee 1.

**Resetting the data.** Stop the app, then delete the three database files **by name**:

**T2 >** `Get-ChildItem AssetDesk -Recurse -Filter 'assetdesk.db*' | Remove-Item -Force`
(macOS / Linux: `find AssetDesk -name 'assetdesk.db*' -delete`)

**Never delete the `data` folder.** On Windows and macOS, `AssetDesk/data` and `AssetDesk/Data` are the same folder: deleting one deletes your whole data layer (SPEC.md §3.2).

---

## Lab 0 — Before the workshop (pre-work, 20 minutes)

Do this at your desk before the day. The workshop assumes it is done.

1. Accept the GitHub invitation to the training repository (it arrives by email after you send your GitHub username).
2. Install Git for Windows: https://git-scm.com/downloads/win
3. Install the .NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0
4. Install Node.js LTS. Lab 6 uses it to give Claude a browser:
   **T2 >** `winget install --id OpenJS.NodeJS.LTS -e`
   (macOS: `brew install node`, or the LTS installer from https://nodejs.org)
5. Install Claude Code:
   **T2 >** `irm https://claude.ai/install.ps1 | iex`
   (macOS / Linux: `curl -fsSL https://claude.ai/install.sh | bash`)
6. Close the terminal and open a new one.
7. Set your Git identity (skip if already set):
   **T2 >** `git config --global user.name "Your Name"`
   **T2 >** `git config --global user.email "you@indusmotor.com"`
8. Clone the training repository (your instructor sends the URL) and enter it. Windows: clone somewhere short and not synced by OneDrive, such as `C:\src`.
   **T2 >** `git clone <repository-url>`
   **T2 >** `cd assetdesk-lab`
9. Sign in to Claude Code:
   **T2 >** `claude`
   Follow the browser sign-in. When you see the Claude prompt, type `/exit`.
10. Run the setup check:
    **T2 >** `powershell -ExecutionPolicy Bypass -File scripts/check-setup.ps1`
    (macOS / Linux: `bash scripts/check-setup.sh`)
    **Check:** all six lines show `[ OK ]` and the last line says `All checks passed. You are ready for Day 2.`
11. Prove you can push:
    **T2 >** `git push --dry-run origin main`
    **Check:** `Everything up-to-date`. Windows may open a browser to sign in to GitHub the first time. On macOS, if Git asks for a password, install the GitHub CLI (https://cli.github.com), run `gh auth login`, and answer **Yes** to "Authenticate Git with your GitHub credentials".

**Optional:** with the GitHub CLI installed and signed in, you can open the Lab 5 pull request from the terminal. The GitHub website works just as well.

If any line shows `[FAIL]`, follow the Fix text printed under it. If it still fails, send the output to your instructor before the workshop.

---

## Lab 1 — Plan it, then let it build (Block 3 · 20 minutes)

**Goal:** approve a plan you have argued with, then let Claude build the whole app from `SPEC.md`, one milestone and one commit at a time.

### Part A — Read the contract (5 minutes)

1. Create your build branch:
   **T2 >** `git status`
   **Check:** `On branch main` and `nothing to commit, working tree clean`.
   **T2 >** `git switch -c build/<name>`

2. Open `SPEC.md` in your editor and read §1, §2, §8, §9 and §10. About 80 lines. You are about to approve a plan, and you can only argue with a plan if you know the contract.

### Part B — Plan, push back, approve (10 minutes)

3. Start Claude in plan mode:
   **T1 >** `claude --permission-mode plan`
   If it asks whether you trust this folder, choose **Yes**: project settings only apply in trusted folders.
   **Check:** the status bar shows `⏸ plan mode on`.

4. See the guardrail before you start:
   **Claude >** `/permissions`
   **Check:** the deny list includes `Edit(SPEC.md)`, and the allow list includes `Bash(dotnet *)`, `Bash(curl *)`, `Bash(git add *)` and `Bash(git commit *)`. Press Esc.

5. **Claude >** `Read SPEC.md and plan the build. Do not write code yet.`
   **Check:** Claude reads files and returns a plan. No files change:
   **T2 >** `git status` → clean.

6. Read the plan against the spec. Look for:
   - M0 and M1 (data layer, then mutations, both checked with `curl`) before **any** component
   - The exact names from §4: `AssetRepository`, `AppState`, `AssetDeskException`, `Foreign Keys=True`
   - Exactly two packages: `Dapper` and `Microsoft.Data.Sqlite`. No EF Core, no interfaces, no test project
   - A commit after each milestone

7. Push back once, even if the plan is good. When asked how to proceed, choose **No, keep planning**, then type:
   **Claude >** `Confirm the .csproj ends with exactly Dapper and Microsoft.Data.Sqlite, and commit after each milestone with the milestone in the message, for example "M0: models, schema, seed".`
   (Or push back on something you found in step 6.)
   **Check:** the new plan says so.

8. Approve. Choose **Yes, and auto-accept edits**. (The wording varies a little between versions: pick the option that auto-accepts edits.)
   **Check:** the status bar shows `accept edits on`, and M0 starts: `dotnet new blazor ...` runs without asking you.

### Part C — Let it run (5 minutes, then all morning)

9. Watch the first milestone. Claude runs `dotnet`, `curl`, `git add` and `git commit` without asking. It may ask before other commands (for example `kill`, `lsof`, `sqlite3`). Read each request: approve it if it stays inside this folder. Choose **No** for anything that deletes a folder, and say:
   **Claude >** `Delete the three sqlite files by name. Data/ and data/ are the same folder on this machine.`

10. When M0 finishes:
    **T2 >** `git log --oneline`
    **Check:** a commit whose message starts with `M0`.

11. Write down the first line that starts with `Assumption:`. We use it at 12:15.

12. Leave T1 alone. Glance at it between slides. Raise a hand if it goes quiet for more than ten minutes, or starts on the UI (`.razor` files, `app.css`) before M1 is committed.

### Done when
- [ ] The plan follows §8: M0 and M1 before any UI
- [ ] You pushed back once and the plan changed
- [ ] M0 is running or committed on `build/<name>`
- [ ] You wrote down its first assumption

### If stuck
- **Claude wrote files while planning:** plan mode was not on. In T1 press Esc, `/exit`. In T2 run `git restore .` and `git clean -fd AssetDesk`, then repeat from step 3.
- **No commit after M0:** type into T1 while it works: `Commit after each milestone from now on.` It reads the message at its next step.
- **It works on components before M1 is committed:** press Esc and type `Stop. SPEC.md §8: finish and commit M1, verified with curl, before any component.`
- **Claude says it cannot edit `SPEC.md`:** working as intended. `.claude/settings.json` denies it. Nothing to fix.
- **"You've reached your usage limit":** raise a hand. You will switch to the reference build at 12:15 and lose nothing.

---

## Lab 2 — Get interviewed for a feature (Block 4 · 30 minutes)

**Goal:** a feature spec for "asset history", written by Claude from your answers, checked by a reader that was not in the interview.

Your build keeps running in T1. This lab happens next to it, in T3: a second Claude session in the **same folder**. It writes one new file, `specs/asset-history.md`, and nothing else, so it never touches the build's files. No new branch, no second folder.

### Part A — Start the second session (2 minutes)

1. **T3 >** `claude --permission-mode default`
   Trust the folder if asked.
   **Check:** the status bar shows neither `plan mode on` nor `accept edits on`. This session asks before every edit.

### Part B — The interview (15 minutes)

2. Open `lab/templates/feature-spec.md` in your editor and skim its nine sections. Claude fills in this shape.

3. **Claude (T3) >**
   ```
   I want to add asset history to AssetDesk: every create, assign, return and status change is recorded and viewable per asset. Interview me in detail using the AskUserQuestion tool, one question at a time: data, API, UI, edge cases, errors, and what we will NOT build. Then write specs/asset-history.md from lab/templates/feature-spec.md. Write only that file, and do not commit.
   ```
   **Check:** one question at a time, each with a few options you can pick with the arrow keys.

4. Answer the way the business would. Answer at least one question with **Other** and your own words, for example `IT wants to know who had this laptop before it broke.`

5. When Claude asks to write `specs/asset-history.md`, read the diff and choose **Yes**.
   Do not commit it. The build in T1 may include it in its next milestone commit: that is fine, because Lab 4 needs it on your build branch anyway.

6. Review the draft against this list. Ask Claude to fix anything missing:
   - An **Amends** line naming the SPEC.md sections it changes, including §2 (tests)
   - **Non-goals**: at least three
   - **Contract**: the table, the model, the repository method, and `GET /api/assets/{id}/history`, where `{id}` is the asset's GUID
   - **Errors** that reuse the §4.6 messages verbatim, for example `That asset no longer exists. Refresh to see current data.`
   - **Acceptance criteria** numbered AC-1, AC-2 …, each one a test someone could write without asking you
   - **Done when**: a checklist, including at least one `curl`

### Part C — The ambiguity hunt (10 minutes)

7. **Claude (T3) >**
   ```
   Use a subagent that has not seen this conversation. It reads SPEC.md and specs/asset-history.md and lists every decision an implementer would have to make that the spec does not make. One line each, with the section. Do not propose answers.
   ```
   **Check:** a subagent row appears while it works, then a numbered list comes back. Typical finds: what "newest first" means inside one second, whether seeded assets get history, whether `{id}` is a tag or a GUID.

8. Fix two of them:
   **Claude (T3) >** `Fix items <n> and <m> in specs/asset-history.md. For each: one sentence in the spec, plus an acceptance criterion that would catch it.`
   Read the diff, approve.

9. Tell the room the most interesting item you did **not** fix.

Leave T3 as it is. You use it again at 12:15.

### Done when
- [ ] The spec names what it amends
- [ ] It has non-goals and an API contract
- [ ] Errors reuse §4.6 messages verbatim
- [ ] Every acceptance criterion could be a test
- [ ] `specs/asset-history.md` is in your repository folder (committed or not)

### If stuck
- **Claude asks three questions in one message:** say `One question at a time.`
- **The interview drifts into code:** say `Behaviour only. What does the admin see?`
- **Claude (T3) asks to edit any file other than `specs/asset-history.md`:** choose **No** and say `Only specs/asset-history.md. The other session owns everything else.`
- **Claude (T3) committed the spec anyway:** no harm. It is on your build branch, where Lab 4 needs it.
- **Usage limit on T3:** type `/exit` in T3. Copy the template and finish it by hand in your editor:
  **T3 >** `Copy-Item lab/templates/feature-spec.md specs/asset-history.md` (macOS / Linux: `cp lab/templates/feature-spec.md specs/asset-history.md`)

---

## 12:15 — Definition of done, clause by clause (15 minutes)

**Goal:** Claude proves your build meets SPEC.md §9, item by item, with evidence. Then you check one thing by hand. "Claude says it's finished" is not a check. "Claude ran this command, and here is the output" is.

**Is your build still running?** If T1 has not reported M6, don't run anything here: a check that builds and runs the app fights the build in T1 for the same files. Watch the projector and run this after lunch. If it is not at M6 by 12:25, go straight to "Behind?" below.

1. Commit your spec, if the build has not already:
   **T2 >** `git status --short specs`
   **Check:** no output means it is already committed. Go to step 2.
   If you see `?? specs/asset-history.md` or ` M specs/asset-history.md`:
   **T2 >** `git add specs/asset-history.md`
   **T2 >** `git commit -m "Spec: asset history"`

2. Give T3 a clean slate. It did not write the app, so it has no stake in saying the app is done:
   **Claude (T3) >** `/clear`

3. **Claude (T3) >**
   ```
   Walk SPEC.md §9, item by item. For each item run a command that proves it and show the evidence. Do not fix anything. If you start the app, use port 5198 and stop it when you are done.
   ```
   Claude asks before commands outside the allow list, for example deleting the database to prove the seed state. Approve deleting `assetdesk.db` and its `-wal` and `-shm` files by name. Choose **No** for anything that deletes a folder.
   **Check:** one entry per §9 item (eleven), each with the command Claude ran, its output, and pass or fail. Some items cannot be proved from a terminal (the browser console, clicking a tab, clicking through §5 by hand): Claude should say so, not claim a pass. If an entry says "pass" with no output, ask `Show me the output for that item.`

4. Now your one check by hand: an error must reach the UI word for word. Start the app:
   **T2 >** `dotnet run --project AssetDesk --urls http://localhost:5198`
   **Check:** `Now listening on: http://localhost:5198`.

   A row in repair has no Assign button, so make the error happen with two browser tabs:
   - Open http://localhost:5198 in two tabs, Assets tab in both.
   - Tab 1: click **Assign** on AST-1003 and pick an employee. Do not confirm yet.
   - Tab 2: click **Mark repair** on AST-1003.
   - Tab 1: click **Assign asset**.
   **Check:** a red message reads `AST-1003 is in repair and cannot be assigned.` An **empty** red box is the most common defect in generated Blazor code. Write it down; Lab 3 finds it.
   Then in tab 2, click **Mark in stock** on AST-1003 to put it back.

   Short of time? Do a smaller check instead: click **People** in the sidebar. **Check:** the view changes.

5. Close the T3 session. T3 is a spare plain shell from now on:
   **Claude (T3) >** `/exit`

6. Optional, for the curious: the raw data Claude read, with the app still running.
   **T3 >** `curl.exe -s http://localhost:5198/api/state`
   (macOS / Linux: `curl -s http://localhost:5198/api/state`)

7. Read your `Assumption:` line from Lab 1 to your neighbour. Is it a hole in SPEC.md?

8. Stop the app: Ctrl+C in T2.

### Behind? Switch to the reference build

If your build is not at M6 by 12:25, or Claude's §9 walk shows the build fails and there is no time to fix it:

1. In T1 press Esc, then `/exit`.
2. Commit your work, so nothing is lost. This also commits your spec:
   **T2 >** `git add -A`
   **T2 >** `git commit -m "wip: my build"`
3. Start a new branch from the finished app:
   **T2 >** `git switch -c build/<name>-ref origin/reference-build`
4. Bring your spec across (skip this if you have no spec yet):
   **T2 >** `git checkout build/<name> -- specs/asset-history.md`
   **T2 >** `git commit -m "Spec: asset history"`
5. Reset the data: the old database survives a branch switch (see "How to read this guide").
6. Run steps 2–4 above on it.

Your own build stays on `build/<name>`. From now on your build branch is `build/<name>-ref`. **Never commit to `reference-build` itself**: the whole room shares it.

---

## Lab 3 — Review, add the rules, review again (Block 5 · 25 minutes)

**Goal:** review your build with an agent that never saw it being written, then watch its findings change when you give it more rules.

Keep a scorecard. You will fill in two rows:

| Review | BOUNDARY | QUALITY | SPEC | TOTAL |
|---|---|---|---|---|
| #1 spec only | | | | |
| #2 + two quality skills | | | | |

### Part A — Review with the spec only (8 minutes)

1. Make sure the build is committed:
   **T2 >** `git status`
   **Check:** clean. If not: `git add -A` and `git commit -m "Build: remaining changes"`.

2. Start a fresh session. The build session is full of its own reasoning.
   **Claude >** `/exit` (skip this if Claude is not running in T1)
   **T1 >** `claude --permission-mode default`

3. **Claude >** `/review-build`
   **Check:** a subagent row for `assetdesk-reviewer` appears, then a report grouped by rule, one `file:line` per finding, ending in a count block:
   ```
   BOUNDARY: 0
   QUALITY:  0
   SPEC:     <n>
   TOTAL:    <n>
   ```
   Write it in row #1. QUALITY is 0 because this branch has no quality rules yet, not because the code is clean.

### Part B — Add the rules (7 minutes)

4. Read both skills before you copy them. They are short, and each rule shows the wrong code next to the right code:
   `lab/skills/csharp-quality/SKILL.md` and `lab/skills/blazor-component-hygiene/SKILL.md`

5. Copy them into `.claude/skills/`. Claude cannot do this for you: `.claude/settings.json` denies edits under `lab/`.
   **T2 >** `Copy-Item -Recurse lab/skills/csharp-quality .claude/skills/`
   **T2 >** `Copy-Item -Recurse lab/skills/blazor-component-hygiene .claude/skills/`
   (macOS / Linux: `cp -r lab/skills/csharp-quality .claude/skills/` and `cp -r lab/skills/blazor-component-hygiene .claude/skills/`)
   **T2 >** `Get-ChildItem .claude/skills` (macOS / Linux: `ls .claude/skills`)
   **Check:** three folders: `blazor-component-hygiene`, `csharp-quality`, `review-build`.

6. Commit them to your build branch. Lab 5's CI review reads its rules from this branch:
   **T2 >** `git add .claude/skills`
   **T2 >** `git commit -m "Lab 3: add the quality skills"`

7. Skills register when a session starts. `/clear` is not enough:
   **Claude >** `/exit`
   **T1 >** `claude --permission-mode default`
   **Claude >** type `/csharp` and look at the menu.
   **Check:** `/csharp-quality` is offered. Press Esc.

### Part C — Review again and compare (10 minutes)

8. **Claude >** `/review-build`
   **Check:** QUALITY is now above 0, for code that did not change:
   **T2 >** `git status` → clean.
   Write it in row #2.

9. Compare the two rows. Same code, same reviewer, more written rules: more findings. The reviewer only checks what someone wrote down. Pick one QUALITY finding and read the rule it cites in the skill file.

### If you have time: fix the findings (optional)

10. **Claude >** `Fix every QUALITY finding from that review. Change nothing else. Run dotnet build AssetDesk, then commit once with the message "Lab 3: fix quality findings".`
    Read each diff before you choose **Yes**.
11. **T2 >** `git show --stat HEAD`
    **Check:** the commit touches only the files the findings named.
    **T2 >** `dotnet build AssetDesk`
    **Check:** `0 Warning(s)`. (Removing a warning suppression can surface a real warning. If it does, ask Claude to fix the cause, not re-suppress it.)
12. **Claude >** `/review-build`
    **Check:** QUALITY dropped.
13. If you saw an empty red box at 12:15, repeat the two-tab check (12:15 step 4). **Check:** the red message now says why.

### Done when
- [ ] Two count blocks, written down
- [ ] QUALITY rose with no code change
- [ ] The two skills are committed on your build branch

### Stretch
Copy `lab/skills/api-boundary` into `.claude/skills/` too, `/exit`, relaunch, and run `/review-build`. Ask for BOUNDARY findings only. Don't fix them today: it is a large refactor.

### If stuck
- **QUALITY is still 0 in review #2:** you did not restart, or the skills are in the wrong folder. The file must be `.claude/skills/csharp-quality/SKILL.md`, not `.claude/skills/csharp-quality/csharp-quality/SKILL.md`. Fix the path, then `/exit` and relaunch.
- **Claude reviews the code itself instead of dispatching the agent:** say `Use the review-build skill. Do not review it yourself.`
- **Your counts differ from your neighbour's:** expected. A live review is not deterministic. What repeats is the direction: QUALITY rises in #2.
- **A finding says tests or interfaces are missing:** ignore it. SPEC.md §2 forbids them, and the reviewer's own rules say so.

---

## Lab 4 — Build the feature, test first (Block 6 · 35 minutes)

**Goal:** your Lab 2 spec on top of your build, with the acceptance criteria turned into failing tests before any feature code exists.

### Part A — Branch (3 minutes)

1. Stop anything running in T1 and T2:
   **Claude >** `/exit`
   Ctrl+C in T2 if the app is running.

2. Make sure you are on your build branch and everything is committed:
   **T2 >** `git switch build/<name>`
   **T2 >** `git status`
   **Check:** clean. If `specs/asset-history.md` is listed, commit it first: `git add specs/asset-history.md` and `git commit -m "Spec: asset history"`. If anything else is listed: `git add -A` and `git commit -m "Build: remaining changes"`.
   **T2 >** `git log --oneline -1 -- specs/asset-history.md`
   **Check:** one commit. The spec is on your build branch.

3. Branch for the feature:
   **T2 >** `git switch -c feature/<name>-asset-history`

### Part B — Plan, then tests first (22 minutes)

4. **T1 >** `claude --permission-mode plan`

5. **Claude >**
   ```
   Read specs/asset-history.md. Write the failing tests from its acceptance criteria first, run them to see them fail, and commit them. Then implement until they pass. SPEC.md stays unchanged; the feature spec amends §2 for tests only. There is no solution file: use dotnet build AssetDesk and dotnet test AssetDesk.Tests.
   ```

6. Read the plan. Look for:
   - A separate test project, `AssetDesk.Tests/`, next to `AssetDesk/`. `AssetDesk.csproj` keeps exactly its two packages
   - Tests that use their own temporary database, never `AssetDesk/data/assetdesk.db`
   - A commit of failing tests **before** any change to `AssetRepository`
   - Every event written with the mutation's own connection and transaction

   If one is missing, choose **No, keep planning** and ask for it.

7. Approve with **Yes, and auto-accept edits**.

8. Watch for the red step. **Check:** Claude runs `dotnet test AssetDesk.Tests` and it fails before any feature code exists. A compile error such as `'AssetRepository' does not contain a definition for 'GetHistory'` counts as red.
   **T2 >** `git log --oneline -3`
   **Check:** a commit with only the tests in it.

9. When Claude says it is done:
   **T2 >** `dotnet test AssetDesk.Tests`
   **Check:** `Passed!` with 0 failed. The reference spec has ten tests; yours may have a different number.
   **T2 >** `dotnet build AssetDesk`
   **Check:** `0 Warning(s)`.

### Part C — See it work, then push (10 minutes)

10. Run the app:
    **T2 >** `dotnet run --project AssetDesk --urls http://localhost:5198`

11. In the browser, open http://localhost:5198, **Assets** tab:
    - Click **Return** on AST-1001.
    - Click **Assign** on AST-1001, pick **Mei Tanaka**, click **Assign asset**.
    - Click **History** on AST-1001.
    **Check:** two events, newest first: `Assigned` above `Returned`. Seeded assets start with no history, so an asset you have not touched shows none.

    Curious about the API? **Claude >** `Show me the history of AST-1001 from the running app's API with curl.` It looks up the GUID for you.

12. Prove the contract did not move:
    **T2 >** Ctrl+C to stop the app, then `git diff --stat main -- SPEC.md`
    **Check:** no output.

13. Push the branch. No pull request yet: that is Lab 5.
    **T2 >** `git status`
    **Check:** clean. If not, commit what is left.
    **T2 >** `git push -u origin feature/<name>-asset-history`
    **Check:** `branch 'feature/<name>-asset-history' set up to track 'origin/feature/<name>-asset-history'`.

### Done when
- [ ] A commit of failing tests comes before the code
- [ ] `dotnet test AssetDesk.Tests` is green
- [ ] `git diff --stat main -- SPEC.md` is empty
- [ ] The branch is pushed. No PR yet

### If stuck
- **`MSB1003: Specify a project or solution file`:** there is no solution file. Name the project: `dotnet test AssetDesk.Tests`, `dotnet build AssetDesk`.
- **`git log` in step 2 shows nothing:** the spec is not committed. Run `git status`: if it shows `?? specs/asset-history.md`, commit it (step 2). If the file does not exist, raise a hand: your instructor has a reference spec.
- **Claude wrote tests and code in one commit:** you have lost the proof that the tests can fail. If there is time, say `Split the last commit into two: the tests first, then the implementation.` If not, move on and say so in your PR description.
- **Claude tries to add "tests are allowed" to SPEC.md and is refused:** working as intended. Say `The amendment lives in specs/asset-history.md §7. Do not edit SPEC.md.`
- **No History button:** your spec may put history somewhere else. Ask Claude: `Where does the UI show an asset's history?`
- **Tests take minutes and fail with `database is locked`:** the event is written on a second connection. Say `Every event insert uses the mutation's own conn and tx.`
- **No tests after ten minutes, or one stays red:** raise a hand. Your instructor has the reference tests.

---

## Lab 5 — Open the PR, read the review (Block 7 · 20 minutes)

**Goal:** Claude reviews your pull request in GitHub Actions against the written rules. You answer one finding, fix another, and a human approves.

The workflow (`.github/workflows/claude-review.yml`) and the API key secret are already on the training repository. You only open a pull request.

### Part A — Open the PR (5 minutes)

1. Push your build branch. It carries the Lab 3 skills, and the review reads its rules from the PR's **base** branch:
   **T2 >** `git push -u origin build/<name>`

2. Open the pull request. Base is your build branch, not `main`: the diff is only the feature.

   On the web: open the repository on GitHub → **Pull requests** → **New pull request**. Set **base:** `build/<name>` and **compare:** `feature/<name>-asset-history`. Title `Asset history`. Click **Create pull request**. Do not create a draft: drafts are skipped.

   Or with the GitHub CLI:
   **T2 >** `gh pr create --base build/<name> --head feature/<name>-asset-history --fill`

   **Check:** the PR page shows `wants to merge ... into build/<name> from feature/<name>-asset-history`, and **Files changed** shows only the feature.

3. Open the **Actions** tab. **Check:** a **Claude review** run for your PR, yellow (running). The first run of the day takes longest; expect two to five minutes.

### Part B — Read, answer, fix (15 minutes)

4. When the run is green, go to your PR. **Check:**
   - Inline comments on changed lines, each citing a clause (`SPEC.md §4.6`, `specs/asset-history.md AC-3`, `csharp-quality §1`)
   - One summary comment headed `Claude review` with the commit SHA, ending in a block:
     ```
     SPEC:    <n>
     QUALITY: <n>
     OTHER:   <n>
     TOTAL:   <n>
     ```

5. Read every finding. For each, decide: agree, or not.

6. Reply to one finding you disagree with, on the comment itself. Cite the clause that makes it wrong, for example `AC-3 says newest first by id, not by occurred_at.` (The review does not answer back. The reply is for the human who approves.)

7. Fix one finding you agree with:
   **Claude >** `This review finding is on my PR: "<paste the finding>". Fix it so the clause it cites is satisfied. Change nothing else, run dotnet test AssetDesk.Tests, and commit.`
   Read the diff. Then:
   **T2 >** `git push`
   **Check:** a new **Claude review** run starts in Actions for the new commit. A run still going for the old commit is cancelled: that is on purpose.

8. Ask your neighbour to review. They open your PR → **Files changed** → **Review changes** → **Approve**.
   **Check:** the PR shows their approval. Claude commented; a human approved.

9. Optional: merge it into your build branch. Only a person ever merges.

### Done when
- [ ] A Claude review cites a clause and a line
- [ ] You replied to one finding you disagree with
- [ ] You pushed a fix and the review ran again
- [ ] A human, not Claude, approved

### If stuck
- **No run in Actions after a minute:** the PR is a draft (click **Ready for review**), or its base is `main`. Check the top of the PR page.
- **The review found nothing:** make one deliberate violation so there is something to answer. In the Assign dialog's `catch`, replace the exception message with `"Something went wrong."`, commit, push, and read the review. Revert it afterwards.
- **QUALITY findings are missing although you added the skills:** the skills commit is not on the remote base branch. Push `build/<name>` (step 1) and push one more commit to the PR.
- **The run failed:** tell your instructor. Don't edit the workflow file: a PR that changes it fails the Claude GitHub App's check.
- **Actions is blocked or slow for the whole room:** run the same review on your laptop. `<number>` is your PR number:
  **T2 >** `gh pr diff <number> | claude -p "Review this pull request diff for AssetDesk. Read SPEC.md, every specs/*.md file in the repo, and every .claude/skills/*/SKILL.md except review-build. Report only problems on lines the diff adds or changes. Every finding: the clause it breaks, file:line, and the problem in one sentence. End with a count block: SPEC, QUALITY, OTHER, TOTAL. Do not edit anything." --allowedTools "Read,Grep,Glob" --max-turns 30`
  No GitHub CLI? Replace `gh pr diff <number>` with `git diff build/<name>...feature/<name>-asset-history`.

---

## Lab 6 — Give Claude new tools (Block 8 · 15 minutes)

**Goal:** connect two MCP servers. One lets Claude read Microsoft's documentation. The other gives Claude a browser, so it can check your running app against the spec by clicking through it, the way it checked §9 from the terminal at 12:15.

An MCP server gives Claude new tools. `claude mcp add` registers one, from a plain shell. The default scope is **local**: only you, only this folder, nothing written into the repository. (With `--scope project` it would write `.mcp.json` into the repository instead, and each teammate approves the server the first time they start Claude.)

### Part A — Microsoft Learn: docs on demand (5 minutes, no install, no login)

1. Leave Claude, then add the server:
   **Claude >** `/exit`
   **T1 >** `claude mcp add --transport http microsoft-learn https://learn.microsoft.com/api/mcp`
   **Check:** `Added HTTP MCP server microsoft-learn with URL: https://learn.microsoft.com/api/mcp to local config`.

2. Servers load when a session starts:
   **T1 >** `claude --permission-mode default`
   **Claude >** `/mcp`
   **Check:** `microsoft-learn` shows as connected. Press Esc.

3. **Claude >**
   ```
   Use the Microsoft Learn tools to find the current recommended way to serialize enums as strings in a .NET 10 minimal API, and cite the page.
   ```
   Approve the tool call when asked. The approval names the server and the tool; the tool's full name is `mcp__microsoft-learn__microsoft_docs_search`.
   **Check:** the answer names `JsonStringEnumConverter` and links a `learn.microsoft.com` page. SPEC.md §4.7 asks for the same converter.

### Part B — Playwright: Claude gets a browser (10 minutes)

4. Start the app:
   **T2 >** `dotnet run --project AssetDesk --urls http://localhost:5198`
   **Check:** `Now listening on: http://localhost:5198`.

5. Leave Claude, then add the browser server:
   **Claude >** `/exit`
   **T1 >** `claude mcp add playwright -- cmd /c npx @playwright/mcp@latest`
   (macOS / Linux: `claude mcp add playwright -- npx @playwright/mcp@latest`)
   It drives Google Chrome. No Chrome on this machine? Add `--browser msedge` to the end of the line.
   **Check:** `Added stdio MCP server playwright with command: ... to local config`.

6. **T1 >** `claude --permission-mode default`
   **Claude >** `/mcp`
   **Check:** `playwright` shows as connected, next to `microsoft-learn`. The first start downloads the server, so it can take up to a minute. Press Esc.

7. **Claude >**
   ```
   Use the Playwright browser to open http://localhost:5198. Click each sidebar tab, then check the acceptance criteria in SPEC.md §5.2. Report pass or fail for each with what you saw. Do not change any code.
   ```
   A browser window opens and moves by itself. Don't click in it while Claude works. Approve the tool calls (`browser_navigate`, `browser_click`, `browser_snapshot` …); to stop being asked for the same tool, pick the option that does not ask again.
   **Check:** the three tabs visited, then the eight §5.2 criteria, each with pass or fail and what Claude saw, for example the result count it read after typing in the search box. Which of these could the 12:15 terminal walk not prove?

8. Look at what Claude reads when it chooses a tool:
   **Claude >** `/mcp`
   Choose `playwright`, then **View tools**, then `browser_click`.
   **Check:** a description and the parameters. That text is how Claude decides which tool to use, and with what arguments. A tool with a vague description gets used badly, or not at all. Press Esc.

9. Stop the app: Ctrl+C in T2. If Claude changed data along the way (retired or assigned something), reset it (see "How to read this guide").

### Done when
- [ ] Claude answered from Microsoft Learn with a citation
- [ ] Claude drove the browser and reported pass or fail for the §5.2 criteria
- [ ] You can name one tool by its `mcp__server__tool` name

### Clean up (optional)
Local servers live in your own Claude Code config, not in the repository, so there is nothing to commit. To remove them, from a plain shell:
**T1 >** `claude mcp remove playwright`
**T1 >** `claude mcp remove microsoft-learn`
The `.playwright-mcp/` folder the browser server writes is ignored by git.

### If stuck
- **`/mcp` does not list the server:** you added it while Claude was running, or from another folder. `/exit`, then run `claude mcp list` in the repository root, then `claude`.
- **`npx` is not recognized (macOS: `command not found`):** Node.js is missing. Install it (Lab 0 step 4), open a new terminal, and start again at step 5. Part A needs no Node.
- **Windows: `playwright` shows failed, or `Connection closed`:** the `cmd /c` is missing. **T1 >** `claude mcp remove playwright`, then add it again exactly as in step 5.
- **`Chromium distribution 'chrome' is not found`:** no Chrome. **T1 >** `claude mcp remove playwright`, then add it again with `--browser msedge` at the end. Every Windows machine has Edge.
- **`playwright` shows failed on its first start:** the download took longer than Claude waits. In T3 run `npx @playwright/mcp@latest --help` (Windows: `cmd /c npx @playwright/mcp@latest --help`) until it prints its help, then in Claude `/mcp` → `playwright` → **Reconnect**.
- **npx hangs, or fails with `ETIMEDOUT`, `E403` or `SELF_SIGNED_CERT_IN_CHAIN`:** a proxy blocks registry.npmjs.org. Raise a hand and watch the instructor run Part B. The npm proxy settings are in §7 of the INSTRUCTIONS file for your OS.
- **The browser shows `This site can't be reached`:** the app is not running on 5198. Start it in T2.
- **Claude reads the source code instead of using the browser:** say `Use the Playwright tools. Check the running app, not the code.`
- **`microsoft-learn` shows failed:** your network blocks learn.microsoft.com. Skip Part A and go on to Part B.

---

## After the workshop

- Keep your clone and your branches. Every lab can be repeated from the `start` tag.
- The take-home exam is due in seven days. It uses the same loop on a feature you choose: spec, ambiguity hunt, tests first, pull request.
- The Quick Reference card has every command and prompt from today.
- Try one thing on Monday: before your next multi-file change, write one page with non-goals and a done-when list, and ask Claude to interview you first.
