# Lab 2 sample: the ambiguity hunt

After the interview, the first draft of `specs/asset-history.md` goes to a cold subagent: one that
was not in the interview and sees only the files. This is what it typically returns for a FIRST
draft, and which two items the attendee fixed.

## The prompt (T3)

```
Use a subagent that has not seen this conversation. Give it SPEC.md and specs/asset-history.md
and nothing else. Ask it: "You must implement this spec without asking anyone a question.
List every decision this spec still leaves open: what you would have to guess. One line each,
most costly first. No suggestions, no praise."
```

## What came back (first draft)

1. **Return on an asset that is not assigned.** SPEC.md never says Return must reject a Repair or
   In stock asset. Does that call write a `Returned` entry, a status-changed entry, or nothing?
2. **A call that changes nothing.** `SetStatus(Repair)` on an asset already in repair succeeds today.
   Does it write an entry with from = to?
3. **Ordering inside one second.** "Newest first" by `occurred_at` ties when assign and return
   happen in the same second. Which comes first?
4. **What `employee_id` holds on Returned.** The employee who had it, or null because nobody holds it now?
5. **Seeded assets.** Do the twelve seeded assets get a backfilled Created entry, or an empty history?
6. **Timestamp format and clock.** `occurred_at` is "the time", but local or UTC, and with seconds?
   SPEC.md §4.3 only defines `yyyy-MM-dd`.
7. **Which id the endpoint takes.** An admin thinks in tags (`AST-1001`); the other
   `/api/assets/{id}` endpoints take the Guid `Id`. Is `{id}` a tag or an id?

## The two the attendee fixed

**Fixed: item 2 (a call that changes nothing).** Added §2 rule 3, "A call that leaves the status
unchanged writes no event", and made it a table `CHECK`
(`from_status <> to_status`), so a repository that forgets the rule fails loudly instead of writing noise. Added AC-6.

**Fixed: item 3 (ordering inside one second).** Changed `id` to `INTEGER PRIMARY KEY` and defined
"newest first" as `ORDER BY id DESC`. Added "with strictly decreasing ids" to AC-3.

## The rest, in the reference spec

The reference answer (`asset-history.md`) closes all seven. In the room, two is the target.
Items 1 and 4 are §2 rules 3 and 6, item 5 is rule 8 (no backfill), item 6 is rule 7
(UTC, `yyyy-MM-dd'T'HH:mm:ss'Z'`), item 7 is §4.4 (the Guid `Id`, not the tag).

## Why this works

The subagent had no memory of the interview, so it could not fill a gap with "what we meant". Every
line above is a place where two attendees' builds would have diverged, and a test written from the
acceptance criteria would have passed on one build and failed on the other.

## What to watch for in the room

- The subagent returns suggestions ("consider adding pagination"). That is scope, not ambiguity.
  Re-run with "decisions left open, not features to add".
- An attendee fixes an item by adding a paragraph of prose. Push for one sentence plus an
  acceptance criterion that would catch it.
- Nobody fixes item 7. Point at it: it is the one that breaks the Lab 4 curl check, where people
  type `/api/assets/AST-1001/history` and get the unknown-id 400.
