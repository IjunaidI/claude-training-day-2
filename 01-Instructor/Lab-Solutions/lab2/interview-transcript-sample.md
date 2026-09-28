# Lab 2 sample: the interview

What a good Lab 2 interview looks like in T3. Nine questions, one at a time, through AskUserQuestion.
The attendee picks an option or types "Other". The picks below produce `asset-history.md` in this
folder. Show this on the projector if a room is slow to start, or to compare after the lab.

## The prompt (T3, a second Claude session in the same folder as the build)

```
Interview me one question at a time, using AskUserQuestion, about an "asset history" feature:
every create / assign / return / status change is recorded and viewable per asset.
Ask about scope, data, API, errors, UI and tests. Offer 2-4 concrete options per question and say
which one you recommend and why. Stop after about ten questions.
Then write specs/asset-history.md from lab/templates/feature-spec.md.
```

## The questions

**Q1. Which changes should create a history entry?**
- A. Status changes only: create, assign, return, repair, in stock, retire *(recommended: that is what the admin asks about; every other field is rarely edited)*
- B. Status changes plus edits to make, model, notes, location, cost
- C. Everything, including reads

> Picked **A**. "There is no edit screen anyway."

**Q2. When should the entry be written, relative to the change itself?**
- A. In the same transaction as the mutation, so a failed change leaves no entry *(recommended: SPEC.md §3.4 already opens a transaction for every mutation)*
- B. After the commit, best effort
- C. By a SQLite trigger on `assets`

> Picked **A**. Claude notes that C would hide the rule from `AssetRepository`, which SPEC.md calls "every SQL statement in the app".

**Q3. Adding 3 headsets at once (quantity 3): how many entries?**
- A. Three, one per asset *(recommended: history is read per asset, so each asset needs its own)*
- B. One entry for the batch, linked to all three

> Picked **A**.

**Q4. Retiring an asset that is assigned clears the assignment (invariant 7). How is that recorded?**
- A. One entry: status changed, Assigned → Retired *(recommended: one call, one entry)*
- B. Two entries: Returned, then Retired

> Picked **A**.

**Q5. What should each entry store?**
- A. Event type, from status, to status, employee (when there is one), UTC timestamp *(recommended)*
- B. A full JSON snapshot of the asset row after the change
- C. A free-text description, like "Assigned to Aisha"

> Picked **A**. Claude proposes the table, enum-as-text columns with `CHECK` lists as in SPEC.md §4.5, and an integer id so "newest first" never ties inside one second.

**Q6. What should the API look like?**
- A. `GET /api/assets/{id}/history`, newest first, where `{id}` is the asset id like the other endpoints *(recommended)*
- B. `GET /api/history?assetId=...`
- C. Add a `history` array to every asset in `GET /api/state`

> Picked **A**. The attendee asks "can I pass the tag instead?" Claude recommends not: every other `/api/assets/{id}` route takes the Guid, and a second id format is a second code path.

**Q7. What happens for an asset id that does not exist?**
- A. 400 with the existing §4.6 message, "That asset no longer exists. Refresh to see current data." *(recommended: reuse, don't invent copy)*
- B. 404 with a new message
- C. 200 with an empty list

> Picked **A**. Claude adds: an existing asset with no entries returns 200 `[]`, which is different from unknown.

**Q8. Where does history appear in the UI?**
- A. A History button on each row of the Assets table, opening a small modal *(recommended: reuses `.modal`, one new component, no new route)*
- B. Expand the row inline, like the People panel
- C. A fourth tab, "History", for all assets

> Picked **A**. The attendee asks for Retired rows to have the button too. Claude writes it in.

**Q9. SPEC.md §2 says "No tests". How do we prove this feature works?**
- A. Amend §2 for this feature only: one xUnit project, `AssetDesk.Tests`, next to `AssetDesk/`, one test per acceptance criterion, a temp database per test *(recommended)*
- B. curl checks only, written in the spec
- C. Add tests for the whole app

> Picked **A**. Claude adds the rule "no test opens `AssetDesk/data/assetdesk.db`" after the attendee asks what happens to their seeded data.

## What Claude said after Q9

> I have enough. Two things I decided without asking; say if you disagree:
> 1. Seeded assets get no backfilled entries. Their history starts empty.
> 2. `occurredAt` is the server's UTC time, not the date typed into the Assign dialog.
>
> Writing `specs/asset-history.md` from the template now.

Both of these belong in the spec. If Claude does not say them out loud, the ambiguity hunt finds them
(see `ambiguity-hunt-sample.md`).

## What to watch for in the room

- Claude asks three questions in one message. Say "one at a time" again.
- Options with no recommendation. Ask "which one would you pick, and why?"
- The interview drifts into implementation (Dapper calls, component code). Pull it back to
  behaviour: "what does the admin see?"
- An attendee picks "everything" on Q1. Let it stand. The ambiguity hunt will list a dozen open
  decisions, and that is the lesson.
