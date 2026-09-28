# Sample review: what Lab 5 produces

A representative result of `claude-review.yml` on a PR from `feature/<name>-asset-history` into
`build/<name>`. Your attendees' findings will differ in detail. The shape will not: every finding
names a clause, points at a line, and lands in one bucket of the count block.

Use it on slide 38, and to answer "is that what it is supposed to look like?" during the lab.

---

## The summary comment (one per push)

> **Claude review · 3f9c2a1**
>
> - SPEC · `specs/asset-history.md` AC-3 · `AssetDesk/Data/AssetRepository.cs:214`: history is ordered `occurred_at ASC`; AC-3 requires newest first.
> - SPEC · `SPEC.md` §3.4 · `AssetDesk/Data/AssetRepository.cs:131`: the history row is written on a new connection after `tx.Commit()`, so an assign can succeed with no event recorded. Every mutation's writes share one transaction.
> - SPEC · `specs/asset-history.md` AC-10, AC-11, `SPEC.md` §4.6 · `AssetDesk/Program.cs:88`: `GET /api/assets/{id}/history` for an unknown id returns `200 []`; the spec requires 400 with "That asset no longer exists. Refresh to see current data."
> - QUALITY · csharp-quality §1 · `AssetDesk/Components/Shared/HistoryDialog.razor:41`: `catch (AssetDeskException) { _error = "Could not load history."; }` discards the repository's message and writes new copy.
> - QUALITY · blazor-component-hygiene §2 · `AssetDesk/Components/Shared/HistoryDialog.razor:18`: `@foreach (var e in Events.OrderByDescending(x => x.OccurredAt).Take(20))` sorts and slices in markup; move it behind a named member.
> - OTHER · no clause · `AssetDesk.Tests/AssetHistoryTests.cs:12`: the tests open `data/assetdesk.db`, so `dotnet test` rewrites the demo data the app is running on.
>
> ```
> SPEC:    3
> QUALITY: 2
> OTHER:   1
> TOTAL:   6
> ```

## Two of the inline comments behind it

On `AssetDesk/Data/AssetRepository.cs`, line 214:

> **specs/asset-history.md AC-3.** History is returned oldest first (`ORDER BY occurred_at ASC`). AC-3 requires the three events "newest first", with strictly decreasing ids. `ORDER BY id DESC` satisfies it and keeps same-second events stable.

On `AssetDesk/Components/Shared/HistoryDialog.razor`, line 41:

> **csharp-quality §1.** This catch replaces the exception's message with "Could not load history." SPEC.md §4.6 says the UI shows the repository's message verbatim and never writes its own copy. `catch (AssetDeskException ex) { _error = ex.Message; }` satisfies both.

## What to point out to the room

- No finding without a clause. The reviewer did not say "consider adding logging" or "this could be cleaner".
- Line numbers are real: click through from the comment to the diff.
- The QUALITY count exists only because `.claude/skills/` on the base branch holds the Lab 3 skills. A build branch without them gets QUALITY: 0. Same code, fewer rules, fewer findings.
- OTHER is the escape hatch for a real consequence nothing written covers. If OTHER findings keep recurring, that is a rule someone should write down.
- It commented. It did not approve. Humans still merge.
