# Screenshot Shot List

The Day 2 deck has no empty placeholders. Four slides carry **illustrative mockups** drawn in HTML: slide 15 (a build transcript with `Assumption:` lines), slide 20 (an ambiguity-hunt result), slide 26 (the author vs the cold reviewer), and slide 38 (a PR review comment). They are honest as they stand, and the speaker notes say they are illustrative. Real captures from your T-3 dry run make them stronger, and give you backup slides for the moments that depend on the network.

Take every shot during the T-3 dry run. Name the files `NN-slideXX-label.png` and keep them in `02-Slides/source/assets/screenshots/`.

**To use a shot:** either add it as a backup slide after the listed slide (a plain `<section class="slide">` with one `<img>` in `02-Slides/source/deck.html`, then `python3 build.py`), or keep the PNGs open in an image viewer on the podium. Replacing a mockup outright is optional: the mockups are designed to read at projector distance, which a terminal capture often is not.

## Capture settings (all shots)
- Windows Terminal, dark theme, font size 18. Crop tight: no taskbar, no desktop.
- Everything from a clone of the training repository, on `build/instructor` or your dry-run branches.
- Never show a real API key, the Console, a personal email, or a real GitHub username other than the training account. Blur the org name if the client asks.
- PNG, about twice the size it appears on the slide.

## Replace or accompany a mockup

| # | Slide | Label | Exactly what to capture |
|---|---|---|---|
| 1 | 15 | The build reports as it goes | T1 during the Lab 1 build, after M2: three or four one-line milestone reports and at least one real `Assumption:` line visible. Crop to those lines. |
| 2 | 20 | The ambiguity hunt | T3 in Lab 2, the ambiguity-hunt step: the subagent's numbered list of open decisions on your dry-run `specs/asset-history.md`. The whole list, readable. |
| 3 | 26 | A cold review | T1 in Lab 3, review #1 on the reference build: the `assetdesk-reviewer` subagent row, one group of findings with `file:line`, and the count block. |
| 4 | 38 | The real PR review | The GitHub PR page from the T-3 dry-run PR: one inline comment on a changed line that cites a clause (`SPEC.md §4.6` or `csharp-quality §1`). A second crop: the summary comment with its `SPEC / QUALITY / OTHER / TOTAL` block. |

## Backup slides (network fallbacks)

| # | After slide | Label | Exactly what to capture |
|---|---|---|---|
| 5 | 17 | Plan mode output | Lab 1 step 5: the build plan with M0 and M1 first, and the approval options visible at the bottom. |
| 6 | 17 | The deny rule | `/permissions` in T1, Deny tab: `Edit(SPEC.md)`, `Edit(lab/**)`, `Bash(git push *)`. |
| 7 | 19 | The interview | Lab 2, the interview: one AskUserQuestion question with its options and a recommended choice, as it appears in T3. |
| 8 | 24 | Claude proves done | T3 during the 12:15 §9 walk: one item with the command Claude ran and its output, ideally invariant 5 (the `curl` assign with a made-up employee id returning 400 and a §4.6 message). A second crop: the end of the walk, with the click-through items marked as not proven. |
| 9 | 24 | The empty red box | Browser, reference build: the Assign dialog after the two-tab check, showing the empty red message area. Same crop after Lab 3's fix, showing `AST-1003 is in repair and cannot be assigned.`. Two images, same size. |
| 10 | 28 | Same code, more rules | Two count blocks from `/review-build` on the reference build, before and after copying the two quality skills. Side by side, same terminal size. |
| 11 | 34 | Red, then green | `dotnet test AssetDesk.Tests` failing to compile before the feature (first error visible), then the `Passed!` summary after. |
| 12 | 41 | The Actions tab | The training repository's Actions tab: one **Claude review** run green, one **Cancelled** by a newer push on the same PR. |
| 13 | 43 | `/mcp` with both servers | `/mcp` in a new session after the two `claude mcp add` commands: `microsoft-learn` and `playwright` both connected, with their tool counts visible. |
| 14 | 44 | A Learn answer with a citation | Lab 6 Part A: the `mcp__microsoft-learn__microsoft_docs_search` call (and `microsoft_docs_fetch` if it used one) and Claude's answer ending with a learn.microsoft.com link. |
| 15 | 44 | Playwright drives the app | Lab 6 Part B, mid-run: the Chrome window Playwright opened on `http://localhost:5198` (a tab other than Dashboard selected) beside T3 showing a `mcp__playwright__browser_click` call. |
| 16 | 45 | The §5.2 report | Lab 6 Part B, the end: Claude's pass/fail list for each SPEC.md §5.2 acceptance criterion, with what it saw. Same terminal size as shot 14. |

## Better live than on a slide
Run these live and keep the screenshots only as a network fallback:
- **Slide 17** (plan): read one plan step aloud and ask the room if they agree, then push back live.
- **Slide 24** (Claude's §9 walk, then the empty red box): let the room watch Claude produce evidence item by item; then the two-tab spot-check by hand takes 60 seconds and lands the 13:30 lesson.
- **Slide 38** (the review): the real comment on the podium PR opened at 15:15.
- **Slide 44** (Playwright): a browser opening and clicking through the app on its own is the moment the room remembers. Keep shots 15 and 16 for when npm or the network is blocked.
