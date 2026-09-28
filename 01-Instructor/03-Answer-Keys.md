# Answer Keys — INSTRUCTOR ONLY

Do not share this file with participants.

---

## Quiz Form A (10:00) and Form B (16:35)

Both forms test the same 12 concepts, one question per concept, so the room-average jump is a fair comparison.

| # | Concept | Block | Form A | Form B |
|---|---|---|---|---|
| 1 | Context: why a spec beats a long chat | 2 | **B** | **C** |
| 2 | Non-goals | 3 | **D** | **A** |
| 3 | Give Claude a way to check its work: done-when and evidence | 3–4 | **A** | **D** |
| 4 | The agent's working agreement (§10) | 3 | **C** | **B** |
| 5 | Where a rule belongs | 4 | **B** | **C** |
| 6 | Delta specs, interview, ambiguity hunt | 4, 6 | **A** | **D** |
| 7 | Independent review: the author doesn't grade | 5 | **D** | **A** |
| 8 | Review buckets: an audit is only as good as its rulebook | 5 | **C** | **B** |
| 9 | Headless `-p` in CI | 7 | **A** | **D** |
| 10 | CI security (forks, untrusted input, least privilege) | 7 | **B** | **C** |
| 11 | MCP: descriptions drive tool choice; tool output is data, not orders | 8 | **D** | **A** |
| 12 | MCP scopes, approval, and loading a new server | 8 | **C** | **B** |

Practical "read this and decide" questions: Form A Q10 (a workflow diff) and Form B Q11 (a hidden instruction in a web page).

### One-line explanations (use at the debrief)

1. A context window fills with tool output; early decisions lose weight and compaction summarises them. A file on disk is re-read in full by every session and every reviewer. A document does not get tired.
2. A non-goal names something the agent must not build. Form A: the other options are a quality adjective, a build order, and a done-when. Form B: SPEC.md §2 says "Adding any of these fails the spec", so AutoMapper is a failure, not an extra.
3. Done-when is a command and an expected output, so the agent can check its own work. Form B: "it says it's done" is a claim, and a clean build proves only that it compiles. The strongest evidence climbs the ladder (build → tests → API → browser), with Claude showing what it saw for each item and a human spot-checking one.
4. §10 is the agent's instructions for when the spec runs out: pick the simplest reading and state it in one line (item 6); park new ideas under "Possible next steps" (item 7). An unattended build never stops to ask.
5. SPEC.md = what the product is. `specs/` = one change. CLAUDE.md = how every session works here. A skill = a detailed standard a reviewer can count against. The prompt = this task only. A deny rule = must hold every time.
6. Brownfield work gets a delta spec that states only what changes and which SPEC.md sections it amends. Form B: the session that wrote the spec shares its blind spots; a fresh-context subagent finds what it would have to guess.
7. The author reviewing its own work re-reads its own reasoning and under-reports. `/review-build` dispatches a reviewer with fresh context and only Read, Grep and Glob. Independent review is one idea used three times today: the ambiguity hunt, `/review-build`, and the CI review.
8. The reviewer reports against the rules it was given and "does not import standards from elsewhere". Zero QUALITY with no quality skills means no rules, not no problems. Adding skills adds findings to unchanged code.
9. `-p` runs once, prints, and exits; without it a CI job waits for input forever (Day 1 slide 54). `--output-format json` makes the result machine-readable; `--max-turns` caps the run.
10. Fork PRs get no secrets by design. `pull_request_target` runs with secrets and a write token, so checking out the PR head there runs untrusted code with your credentials. Treat PR text as data; grant the fewest permissions and tools.
11. Form A: Claude picks a tool by reading its description, so the description is the interface; a vague one gets the wrong tool or none. Form B: anything a tool returns, a web page included, is data, not orders. A hidden instruction changes nothing: Claude should report it, and the task, permission prompts and deny rules still decide what runs. Visible or hidden makes no difference, and a deny rule covering one action does not make the others acceptable.
12. Form A: `--scope project` writes `.mcp.json` at the repo root, to be committed; each teammate approves a project server the first time; a new server loads only in a new session (`/exit`, `claude`), so an already-open session does not see it. Form B: local scope, the default, is you in this project only; user scope is you in every project.

### How to report the result
Count correct answers per sheet, then report only the **room average** for Form A and Form B, for example "5.2 → 10.1 out of 12". Never show individual scores.

**Answer distribution check:** on each form A, B, C, and D are each correct exactly three times.

---

## Take-Home Exam (practical, 20 points, pass mark 14)

Mark from the pull request link and the submitted text. Check the commit history, not just the final diff.

### Part 1 — Delta spec (6 points)

| Points | Award when |
|---|---|
| 1 | **Amends** lists the right SPEC.md sections by number. Option 1: §4.1, §4.5 (new column), §5.1 and §5.2 (dashboard and filter), §4.7 if an endpoint changes. Option 2: §4.6 (new repository method), §4.7 (new endpoint), §5.5 (button). Both: §2 for tests only. |
| 1 | At least three **non-goals** that a builder might plausibly add. Option 2 must say the employee record is not deleted or deactivated (invariant 8). Option 1 must rule out things like notifications or renewal workflows. |
| 1 | **Contract (§4.4 API)** is complete: method, path, body, response shape, status codes. Option 2: something like `POST /api/employees/{id}/return-all` returning the returned assets. |
| 1 | **Errors** reuse §4.6 messages where they fit (unknown employee → `That asset no longer exists. Refresh to see current data.` or a new message in the same "what happened, what to do" style) and are returned as `{ "error": ... }` with 400. |
| 1 | **Acceptance criteria** are checkable, not adjectives. Option 2 must include the all-or-none case (for example, one asset fails and nothing is returned). Option 1 must pin the boundary (is day 30 in or out?). |
| 1 | **Done when** has commands with expected output, including at least one `curl` that bypasses the UI. |

### Part 2 — Ambiguity hunt (3 points)

| Points | Award when |
|---|---|
| 1 | The list is included unedited and came from a fresh-context subagent (not the interviewing session answering itself). |
| 1 | Two open decisions are fixed in the spec, each with a one-line reason, in a separate commit. |
| 1 | One item is deliberately left unfixed with a sensible reason (out of scope, a non-goal, or not worth deciding yet). |

Typical good finds: Option 1 — are retired assets counted? is the warranty date required for existing seed rows? what is "today" in tests? Option 2 — what if the employee holds nothing? does it change the People count without collapsing the panel (§5.5)? what `AssignedDate` or history does a bulk return leave?

### Part 3 — Test-first build and pull request (6 points)

| Points | Award when |
|---|---|
| 1 | Branch `feature/<name>-<feature>` is based on the build branch, and the spec is committed on it before the tests. |
| 1 | History shows a commit of failing tests **before** the implementation commit. |
| 1 | Evidence of zero warnings, green tests, and the done-when `curl` (pasted output, the PR description, or CI), shown as commands with their output, not as "it works". |
| 1 | `SPEC.md` is unchanged in the diff, and nothing from SPEC.md §2 was added beyond the test project the spec allowed. |
| 1 | The Claude review ran on the pull request. |
| 1 | One finding is resolved: a follow-up commit that fixes it, or a reply citing the clause that makes it wrong. The PR was **not** merged by its author. |

Partial credit: if CI could not run (no access, runner outage), award the review points for a `/review-build` count block pasted into the PR description with one finding addressed.

### Part 4 — Written answers (5 points, one each)

| Q | Award the point when the answer says |
|---|---|
| 1 | Any two of: SPEC.md is the agreed baseline and is protected by a deny rule; a small delta is easy to review in a PR; the spec is reviewed before the code; several features can be specified in parallel without conflicting edits; folding deltas into SPEC.md is a separate, deliberate release decision. |
| 2 | A real quoted finding, the clause it cited, a correct bucket (BOUNDARY, QUALITY or SPEC, per the reviewer's definitions), and a judgement with a reason. Disagreeing with the reviewer is fine if the reason cites the spec. |
| 3 | Any two of: the prompt tells Claude PR text is data, not instructions; the job has least-privilege permissions and a narrow `--allowedTools`; fork PRs get no secrets; `--max-turns` and a timeout bound the run; the review only comments, and a person approves and merges. |
| 4 | In a skill (for example, extending `csharp-quality`) or CLAUDE.md, because it applies to every feature and the reviewer can count against a skill; a feature spec covers one change only. Answers naming a CI prompt as well are fine; answers naming only the feature spec get no point. |
| 5 | The Playwright report is pasted with a pass or fail and an observation for each UI acceptance criterion; the answer names one thing the browser check proved that `dotnet test` could not (for example, that the button appears only on the right rows, or that the count on screen updates), and the one thing the attendee checked by hand. A report with verdicts but no observations gets no point. |

**Grading:** 14–20 points = Pass. 10–13 = Pass with review (send the rationale for the rows they missed). Below 10 = offer a 30-minute follow-up session.
