# Pre-Delivery Checklist

Work down this list in order. Each line has an owner and a deadline relative to the workshop day (T).

Day 2 has three dependencies Day 1 did not: a GitHub repository attendees can push to, a Claude GitHub App with an API key secret for the CI review, and seats that survive an 80-minute unattended build plus a second session in parallel. Start them at T-10.

## T-10 days — Access and environment

- [ ] **Licences:** every attendee has a Claude Pro, Max, Team, or Enterprise seat; the free plan does not include Claude Code. **Max or Team is strongly recommended**: the Lab 1 build runs 60–80 minutes unattended, and Lab 2 runs a second Claude session in parallel with it. Pro seats may hit their usage limit before lunch; those attendees use the `reference-build` fallback. *Owner: Indus Motor IT / 10Pearls account lead*
- [ ] **Claude Code is enabled for the organization.** If an Enterprise or Team admin has disabled Claude Code or subscription access, `claude -p` fails and the setup check shows `[FAIL] Claude Code signed in`. Test with one attendee account now. *Owner: Indus Motor IT*
- [ ] **Network allow-list** on the training network and attendee laptops: *Owner: Indus Motor IT*
  - `claude.ai`, `*.anthropic.com` (Claude Code sign-in and API)
  - `api.nuget.org` (NuGet restore: the build and the tests)
  - `github.com`, `api.github.com` (clone, push, pull requests, the `gh` CLI)
  - `learn.microsoft.com` (Lab 6 Part A: the Microsoft Learn MCP server, `https://learn.microsoft.com/api/mcp`)
  - `registry.npmjs.org` (Lab 6 Part B: `npx @playwright/mcp@latest` downloads the Playwright MCP package on first use; it drives the Chrome or Edge already on the laptop and downloads no browser)
- [ ] **Install rights:** attendees can install the .NET 10 SDK, Git, Claude Code, and **Node.js LTS** (18 or later, for Lab 6 Part B), or IT pre-installs them. Google Chrome or Microsoft Edge is on every laptop. The GitHub CLI (`gh`) is optional. *Owner: Indus Motor IT*
- [ ] **GitHub organization:** an org you control, or a client org where you are an owner, to host a private training repository. *Owner: 10Pearls account lead*
- [ ] **Anthropic Console:** a dedicated workspace for the workshop, with a **monthly spend limit**, and one API key in it. Budget roughly 40–60 review runs for a room of 20, plus the dry run (`Lab-Solutions/lab5/SETUP.md`, "Cost"). *Owner: 10Pearls account lead*
- [ ] **Attendee count and OS mix** confirmed. The lab guide is written for Windows PowerShell with macOS in brackets. *Owner: 10Pearls account lead*

## T-7 days — Repository, GitHub App, secret

Follow `Lab-Solutions/lab5/SETUP.md` §1–§4. In short: *Owner: Instructor*

- [ ] Build the repository and push it to an **empty private** repository, for example `<org>/assetdesk-lab`:

  ```
  bash 05-Lab-Repo/make-lab-repo.sh ../assetdesk-lab
  cd ../assetdesk-lab
  git remote add origin https://github.com/<org>/assetdesk-lab.git
  git push -u origin main reference-build --tags
  ```

  (Windows: `powershell -ExecutionPolicy Bypass -File 05-Lab-Repo/make-lab-repo.ps1 ..\assetdesk-lab`.)
  **Check:** GitHub shows branches `main` and `reference-build`, tag `start`, and the Actions tab lists **Claude review**.
- [ ] Keep the repository name `assetdesk-lab`. The pre-work email and the lab guide say the clone folder is `assetdesk-lab`; if you choose another name, change both.
- [ ] Install the Claude GitHub App (https://github.com/apps/claude) on **only** the training repository. An org owner may have to approve it.
- [ ] Add the repository secret `ANTHROPIC_API_KEY` from the Console workspace above. Never put a key in a file.
- [ ] Confirm **Settings → Actions → General** allows actions to run (org policies sometimes block third-party actions; allow `anthropics/claude-code-action` and `actions/checkout`).
- [ ] Do **not** protect `build/*` or `feature/*` branches. Protecting `main` is optional.

## T-5 days — Send pre-work, collect GitHub usernames

- [ ] Fill in `<DATE>`, `<ROOM>`, `<REPO-URL>` and `<INSTRUCTOR NAME>` in `03-Send-Before-Workshop/Pre-Work-Email.md` and send it. *Owner: 10Pearls account lead*
- [ ] Collect every attendee's **GitHub username** by T-3. Add them to a team with **Write** access on the training repository. Writers can push branches and open PRs, and the action only runs for users with write access. *Owner: Instructor*
- [ ] Ask attendees to reply with a screenshot of the setup check showing six `[ OK ]` lines (git, .NET 10, claude, signed in, Node 18+, NuGet reachable) by T-2. *Owner: 10Pearls account lead*

## T-3 days — Dry run (do not skip)

On a clean Windows machine that matches the attendees' build, with an attendee-type account (not an admin): *Owner: Instructor*

- [ ] Clone the training repository, run the setup check: six `[ OK ]`. Run `git push --dry-run origin main`: `Everything up-to-date`.
- [ ] Run **every lab** in `lab/LAB-GUIDE.md` end to end, including the full Lab 1 build from `main`. Time each lab and note it next to the budget: Lab 1 20 min (plus the 60–80 minute build), Lab 2 30, Lab 3 25, Lab 4 35, Lab 5 20, Lab 6 15.
- [ ] Lab 1: note the build's total time, how many permission prompts it raised for commands outside the allow list, and its `Assumption:` lines. You will quote them on slide 15.
- [ ] Lab 2: the second Claude session in the same folder writes only `specs/asset-history.md` while the build runs in T1; `git status` shows nothing else changed by it.
- [ ] 12:15 DoD: the prompt `Walk SPEC.md §9, item by item. For each item run a command that proves it and show the evidence. Do not fix anything. If you start the app, use port 5198 and stop it when you are done.` (in a second session after `/clear`) walks every §9 item with a command and its output, marks the click-through items as not proven, and changes no files. Note how long it takes and confirm it stopped the app it started. The two-tab spot-check shows an **empty** red box on the reference build (planted defect 1).
- [ ] Lab 3: record the two count blocks. Typical on the reference build: `QUALITY: 0` before the skills, QUALITY above 0 after.
- [ ] Lab 4: `git switch -c feature/<name>-asset-history` from the build branch already has the spec; `dotnet test AssetDesk.Tests` goes red, then green.
- [ ] Lab 5: open **one real PR** on the training repository (`SETUP.md` §5) and confirm the inline comments, the summary count block, and the rerun on a new push. Note the run time and the cost in the Console.
- [ ] Lab 6 Part A: `claude mcp add --transport http microsoft-learn https://learn.microsoft.com/api/mcp`, new session, `/mcp` shows `microsoft-learn` connected with 3 tools; a .NET question calls `mcp__microsoft-learn__microsoft_docs_search` and the answer links a learn.microsoft.com page.
- [ ] Lab 6 Part B, on the attendee-type **Windows** account: `claude mcp add playwright -- cmd /c npx @playwright/mcp@latest`, new session, `/mcp` shows `playwright` connected. With the app on 5198, the Part B prompt (`Use the Playwright browser to open http://localhost:5198. Click each sidebar tab, then check the acceptance criteria in SPEC.md §5.2. Report pass or fail for each with what you saw. Do not change any code.`) opens a browser, clicks every tab and reports each §5.2 criterion. Time the first `npx` start through the client's network; if the proxy blocks `registry.npmjs.org`, plan Part B as a podium-only demo. Repeat once on macOS with the command without `cmd /c`.
- [ ] Remove both servers afterwards (`claude mcp remove microsoft-learn`, `claude mcp remove playwright`) so the podium demo adds them live.
- [ ] Try the Lab 5 local fallback once (`gh pr diff <n> | claude -p ...` from the lab guide) so you know its output.
- [ ] Take the screenshots in `05-Screenshot-Shot-List.md`.
- [ ] Pin the Claude Code version you rehearsed with (`claude --version`). Write it in the runbook header. If the plan-approval menu wording has changed, update Lab 1 step 8 and Lab 4 step 7 in the lab guide.
- [ ] Close the dry-run PR and delete its branches.

## T-2 days — Chase

- [ ] Anyone without a setup-check screenshot, or who has not accepted the GitHub invitation, gets a call. Offer a 15-minute setup slot on T-1. *Owner: 10Pearls account lead*
- [ ] Anyone whose `Claude Code signed in` check fails with an organization message: escalate to Indus Motor IT now. *Owner: Instructor*

## T-1 day — Forms, deck, podium, printing

- [ ] **Forms:** create three forms (Quiz A, Quiz B, feedback) from `04-Hand-To-Students/03-…`, `04-…` and `07-…`. *Owner: 10Pearls account lead*
- [ ] Paste the three links into `02-Slides/source/config.json` and rebuild the deck, which regenerates the QR codes on slides 5, 47 and 48: *Owner: Instructor*

  ```
  cd 02-Slides/source
  python3 -m pip install -r requirements.txt
  python3 build.py
  ```

  **Check:** the new PDF and PPTX in `02-Slides/` show your links under the QR codes. Scan one with a phone.
- [ ] **Podium clones** (see the runbook header): *Owner: Instructor*

  ```
  git clone <REPO-URL> ~/podium/assetdesk-live
  git clone <REPO-URL> ~/podium/assetdesk-ref
  cd ~/podium/assetdesk-ref
  git switch -c build/instructor origin/reference-build
  git push -u origin build/instructor
  git switch -c feature/instructor-asset-history
  mkdir AssetDesk.Tests
  cp <package>/01-Instructor/Lab-Solutions/lab2/asset-history.md specs/
  cp <package>/01-Instructor/Lab-Solutions/lab4/AssetDesk.Tests.csproj <package>/01-Instructor/Lab-Solutions/lab4/AssetHistoryTests.cs AssetDesk.Tests/
  git add specs AssetDesk.Tests
  git commit -m "Asset history: spec and failing tests"
  git apply --directory=AssetDesk <package>/01-Instructor/Lab-Solutions/lab4/reference-implementation.patch
  dotnet test AssetDesk.Tests          (10 passed)
  git add -A
  git commit -m "Asset history: implementation"
  git push -u origin feature/instructor-asset-history
  git switch build/instructor
  ```

  Do not open the PR yet: you open it live at 15:15.
- [ ] **Podium MCP:** Node.js 18 or later and Google Chrome on the podium machine. Run `npx @playwright/mcp@latest --help` once so the package is cached, and confirm `claude mcp list` in `assetdesk-ref` shows neither `microsoft-learn` nor `playwright`. *Owner: Instructor*
- [ ] Put the Lab 4 reference tests (`Lab-Solutions/lab4/AssetDesk.Tests.csproj` and `AssetHistoryTests.cs`) on a USB stick or a shared folder. Never the patch: it only applies to the reference build. *Owner: Instructor*
- [ ] **Print** one per attendee: Quiz Form A, Quiz Form B, Quick Reference, Stretch Cards, Feedback Form. Print the take-home exam, or prepare to email it at 17:00. The lab guide is not printed: attendees open `lab/LAB-GUIDE.md` in the repo. *Owner: 10Pearls account lead*
- [ ] Print one copy of `03-Answer-Keys.md` for yourself. Keep it out of sight. *Owner: Instructor*

## T — Morning of

- [ ] Run the 09:30 checklist at the top of the runbook. *Owner: Instructor*

## T+7 and after

- [ ] Grade the take-home exams with `03-Answer-Keys.md`. *Owner: Instructor*
- [ ] Once grading is done: delete the `ANTHROPIC_API_KEY` secret, **revoke the key** in the Console (deleting the secret alone leaves it valid), and archive or delete the training repository. *Owner: Instructor*

## Fallbacks to have ready

| If this fails | Do this |
|---|---|
| Network or API down for the room | Switch to the screenshot backups; narrate each demo; run the labs on the podium with the room directing |
| Corporate proxy blocks Claude sign-in | Podium on a phone hotspot; attendees pair around working machines |
| An attendee's build stalls, or they hit a usage limit | Commit, then `git switch -c build/<name>-ref origin/reference-build`. They lose nothing: every afternoon lab works on it |
| Most of the room is behind at 12:15 | Everyone switches to the reference build at lunch. Lab 3 still works: the reference build has five planted defects for the reviewer to find |
| NuGet blocked | Pre-restore on the podium and share the `~/.nuget/packages` folder. Without NuGet, Lab 1 and Lab 4 cannot build |
| npm registry blocked, or attendees have no Node | Lab 6 Part B is a podium-only demo: everyone does Part A, you run Part B on the projector and read the §5.2 report aloud |
| `learn.microsoft.com` blocked | Podium on a phone hotspot for the Part A demo; attendees do Part B only |
| GitHub push blocked or credentials fail | Pair with a neighbour who can push; review their PR together. Local review fallback below |
| GitHub Actions blocked, or the secret is wrong on the day | Everyone runs the review locally: `gh pr diff <n> \| claude -p "..."` (lab guide, Lab 5 "If stuck"). Same prompt, same count block, no CI |
| Playwright MCP will not connect on Windows | Check the command has `cmd /c` before `npx` (`claude mcp remove playwright`, add it again); if it still fails, pair with a working machine |
| One attendee's laptop broken | Pair them with a neighbour; fix at the next break |
