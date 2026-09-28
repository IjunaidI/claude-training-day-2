# Day 30 Adoption Check

Send this 30 days after Day 2. It turns attendance into evidence of adoption.

## Record from the workshop (fill in on the day)

| Measure | Value |
|---|---|
| Attendees | |
| Builds that reached the definition of done by 12:15 (without `reference-build`) | |
| Quiz Form A room average (out of 12) | |
| Quiz Form B room average (out of 12) | |
| Take-home exam pass rate (14/20 points or more) | |
| Feedback: average of "pace was right" | |
| Commitments captured (attach board photo) | |

## The 30-day survey (send to every attendee)

**Subject:** Claude Code Day 2 — 30 days on, 2 minutes

1. Since the workshop, how many specs or feature specs have you written before building something with Claude? (0 / 1 / 2–3 / 4+)
2. Did the spec go into a pull request before the code did? (Always / Sometimes / Never)
3. Does a repo you work in now run a Claude review on pull requests in CI? (Yes / No; if no, what is in the way?)
4. Have you added a skill that a reviewer checks against, or a hook that enforces a rule? (Yes / No; which one?)
5. Do you use an MCP server with Claude Code in your daily work? (Yes / No; which one?)
6. Did you write the first spec you named on the feedback form? (Yes / Partly / No; what stopped you?)
7. What is the one thing blocking you from working spec-first more often?

## Evidence you can check without asking (with repo access)

- Count files under `specs/` (or a committed `SPEC.md`) created or changed since the workshop.
- Count workflows under `.github/workflows/` that use `anthropics/claude-code-action`, and the pull requests they commented on in the last 30 days.
- Count pull requests where a human approved and merged after a Claude review, versus merged with no review at all.
- Count files under `.claude/skills/` and `.claude/agents/`, and `.claude/settings.json` files containing `deny` or `hooks`.
- Count committed `.mcp.json` files, and check that none contains a literal token instead of `${VAR}`.

## Report (one slide for leadership)

- Score lift: Form A → Form B average
- Spec-first: number of specs written, and % of them that reached a PR before the code
- Review coverage: repos with a Claude review in CI, and pull requests it reviewed
- Tooling: skills, hooks, and MCP servers in active use
- Top blocker, and the proposed fix
