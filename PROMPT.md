# Kickoff prompt

Paste this into Claude Code with `SPEC.md` in the working directory.

---

You are implementing a prototype from a specification. Read `SPEC.md` in full before writing any code.

Build it exactly as written. The spec is the source of truth — its field names, enum values, colour tokens, seed data, and acceptance criteria are all deliberate, so use them verbatim rather than substituting your own conventions.

Work through the milestones in section 8 in order. After each milestone:

1. Run `npm run dev` and confirm it compiles with no errors
2. Check the acceptance criteria for what you just built
3. Report one line: milestone number, what works, what you cut if anything

Two rules that matter more than the rest:

- Section 2 is a list of things not to build. Respect it. A smaller app that matches the spec beats a bigger one that drifts from it.
- If something is ambiguous, take the simplest reading, state your assumption in one line, and keep moving. Don't stop to ask me questions.

Start with M1.

---

## Follow-up prompts for the demo

Useful for showing students how a spec turns changes into small, safe edits.

**Change a requirement, not the code**

> In SPEC.md section 5.1, change the low-stock thresholds to: 0 → Out, 1–4 → Low, 5+ → OK. Update the implementation to match, and nothing else.

**Add a feature the spec-driven way**

> Add a new section 5.6 to SPEC.md for an Activity log tab: a reverse-chronological list of every assign, return, and status change, with timestamp, asset tag, and a one-line description. Follow the format of the existing screen sections including acceptance criteria. Then implement it.

**Show the spec catching a bug**

> Verify every invariant in SPEC.md section 4 against the current implementation. For each one, tell me whether it holds and how you checked. Fix any that fail.

**Show what happens without a spec** (run this in a separate, empty session for contrast)

> Build me an IT asset management app in React.
