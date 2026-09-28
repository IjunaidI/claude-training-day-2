# Asset history

**Version** 1.0 · **Status** Ready to build · **Amends** SPEC.md v3.0 §2, §3.2, §4.1, §4.2, §4.3, §4.5, §4.6, §4.7, §5.2

> Reference answer for Lab 2, written from `lab/templates/feature-spec.md`. A good interview
> converges on something close to this. Wording can differ; the decisions in §2, §4 and §6 should
> not. SPEC.md is not edited. Where this file and SPEC.md disagree, this file wins for this feature
> only. Target build time on top of any SPEC-conformant build: 20 minutes.

---

## 1. Context

AssetDesk answers "who has it right now". It cannot answer "who had it before" or "when did it go
to repair". The IT admin asks both during every laptop hand-over and every warranty claim, and today
the answer is lost the moment a row is updated. This touches the schema (SPEC.md §4.5), the
repository (§4.6), the API (§4.7) and the Assets table (§5.2).

## 2. The change

Every successful create, assign, return and status change writes one row to a new `asset_events`
table, **in the same transaction as the mutation**. A new repository method and endpoint read one
asset's events, newest first. A **History** button on each asset row shows them.

1. One event per asset per successful repository call that changes that asset's status.
   `CreateAssets` with quantity `n` writes `n` events, one per asset.
2. The event type follows the method: `CreateAssets` → `Created`, `Assign` → `Assigned`,
   `Return` → `Returned`, `SetStatus` → `StatusChanged`.
3. A call that leaves the status unchanged writes no event (for example `SetStatus(Repair)` on an
   asset already in repair, or `Return` on an asset already in stock). It still succeeds or fails
   exactly as SPEC.md says today.
4. A call that fails writes no event. The event insert runs on the mutation's own connection and
   transaction, after the mutation's own statement, and rolls back with it.
5. Retiring an assigned asset is one `StatusChanged` event (`Assigned` → `Retired`), not a
   `Returned` plus a `StatusChanged`. Invariant 7 still clears the assignment in the same transaction.
6. `employee_id` is the new holder on `Assigned`, the holder who handed it back on `Returned`
   (`NULL` if the asset was not assigned), and `NULL` on `Created` and `StatusChanged`.
7. `occurred_at` is the UTC time the event was written:
   `DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture)`.
   It is not the user-entered `assignedDate`.
8. No backfill. The seeded assets start with an empty history. Seeding in `Db.cs` is unchanged.

## 3. Non-goals

- No pagination, no limit parameter, no filtering, no search across history
- No editing or deleting events. No method, endpoint or button does it
- No history for employees, and no "who made the change" (there are no users, SPEC.md §2)
- No tracking of other field changes (make, model, notes, location, cost). Status changes only
- No backfill of events for seeded or pre-existing assets
- No history on the Dashboard or People tabs
- No new route. History opens in a modal on the existing `/` page
- No SQLite triggers. `AssetRepository` writes events next to the statement that changed the row
- No endpoint tests, no `WebApplicationFactory`, no mocking or assertion library, no test CI

## 4. Contract

### 4.1 Schema

Appended to the SQL that `Data/Db.cs` runs at startup:

```sql
CREATE TABLE IF NOT EXISTS asset_events (
  id          INTEGER PRIMARY KEY,
  asset_id    TEXT NOT NULL REFERENCES assets(id),
  type        TEXT NOT NULL CHECK (type IN
                ('created','assigned','returned','status_changed')),
  from_status TEXT CHECK (from_status IN
                ('in_stock','assigned','repair','retired')),
  to_status   TEXT NOT NULL CHECK (to_status IN
                ('in_stock','assigned','repair','retired')),
  employee_id TEXT REFERENCES employees(id),
  occurred_at TEXT NOT NULL,
  CHECK (
    (type =  'created' AND from_status IS NULL     AND to_status = 'in_stock') OR
    (type <> 'created' AND from_status IS NOT NULL AND from_status <> to_status)
  )
);

CREATE INDEX IF NOT EXISTS idx_asset_events_asset ON asset_events(asset_id);
```

- `id` is an integer, unlike the Guid ids in SPEC.md, because it is the ordering key. Rows are
  never deleted, so `ORDER BY id DESC` is newest first, with no tie when two events share a second.
- The table `CHECK` turns rule 3 ("no status change, no event") into a constraint, the way
  invariants 2 and 3 are in SPEC.md §4.4. The repository must not rely on it: skip the insert when
  the status did not change.
- `IF NOT EXISTS` means an existing `assetdesk.db` gains the table on the next start. No reset.

### 4.2 Models

Added to `Data/Models.cs`:

```csharp
public enum AssetEventType { Created, Assigned, Returned, StatusChanged }

public record AssetEvent(
    long Id,
    string AssetId,
    AssetEventType Type,
    Status? FromStatus,     // null only for Created
    Status ToStatus,
    string? EmployeeId,     // §2 rule 6
    string OccurredAt);     // UTC, "2026-09-28T09:15:02Z"
```

Amends §4.1: `AssetEvent.EmployeeId` is a third nullable reference type in the model.

Enum storage, added to `Data/Format.cs` as a fourth lookup beside the three in SPEC.md §4.3.
`from_status` and `to_status` use the existing Status lookup.

| C# | Database | UI label |
|---|---|---|
| `AssetEventType.Created` | `created` | Created |
| `AssetEventType.Assigned` | `assigned` | Assigned |
| `AssetEventType.Returned` | `returned` | Returned |
| `AssetEventType.StatusChanged` | `status_changed` | Status changed |

### 4.3 Repository surface

Added to `AssetRepository`:

```csharp
List<AssetEvent> GetHistory(string assetId);
```

- Returns the asset's events ordered by `id` descending (newest first).
- An existing asset with no events returns an empty list.
- An unknown `assetId` throws `AssetDeskException` (§5).
- Reads through a private `record AssetEventRow(...)` that mirrors the columns, mapped in one
  private `MapAssetEvent(AssetEventRow)`. Amends §4.2: three Map methods, not two.
- The four existing mutations keep their signatures. Each inserts its event with the connection
  and `tx` it already has.

### 4.4 API

Added to `Program.cs`. Amends §3.2: seven endpoints, not six.

| Method | Path | Body | Returns |
|---|---|---|---|
| GET | `/api/assets/{id}/history` | — | `AssetEvent[]`, newest first, 200 |

`{id}` is the asset's `Id` (the Guid), the same id every other `/api/assets/{id}/...` endpoint
takes, not its tag. JSON is camelCase with enums as strings, like every other endpoint:

```json
[
  { "id": 2, "assetId": "2ed473a4-…", "type": "Assigned", "fromStatus": "InStock",
    "toStatus": "Assigned", "employeeId": "fed91692-…", "occurredAt": "2026-09-28T09:24:03Z" },
  { "id": 1, "assetId": "2ed473a4-…", "type": "Returned", "fromStatus": "Assigned",
    "toStatus": "InStock", "employeeId": "64889a07-…", "occurredAt": "2026-09-28T09:24:02Z" }
]
```

### 4.5 UI

Amends §5.2.

- The Actions column of the Assets table gains a **History** button, last on every row, including
  Retired rows (which otherwise have no buttons).
- It opens `Components/Shared/HistoryDialog.razor`, a small centred modal built from the existing
  `.modal` and `.backdrop` classes. Heading `History`, then the tag and device as static text, like
  the Assign dialog. One button: **Close**. Backdrop click also closes it.
- Body: a `.table` with two columns, **When** and **Event**, newest first.
  - When: `OccurredAt` as `yyyy-MM-dd HH:mm UTC`, monospace.
  - Event: `Created` · `Assigned to {employee name}` · `Returned by {employee name}` (`Returned`
    when `EmployeeId` is null) · `{from label} → {to label}` for `StatusChanged`, for example
    `In stock → In repair`. Names come from the employees already in `AppState`.
- No events: `No history recorded yet.`
- A load error shows the exception message verbatim inside the modal.
- History loads when the modal opens. It does not live-update while open.

## 5. Errors

No new messages. The unknown-id case reuses SPEC.md §4.6 verbatim. The API returns
400 `{ "error": "<message>" }`; the UI shows the message verbatim.

| Case | Message |
|---|---|
| `GetHistory` with an unknown asset id | `That asset no longer exists. Refresh to see current data.` |
| Any failed mutation | The existing SPEC.md §4.6 message, unchanged. No event is written |

## 6. Acceptance criteria

AC-1 to AC-10 are tests in `AssetDesk.Tests/AssetHistoryTests.cs`. AC-11 is a curl check, AC-12 is
checked by hand.

- **AC-1** Given an empty database, when `CreateAssets` runs with quantity 3, then each of the three
  assets has exactly one event: `Created`, from `null` to `InStock`, `EmployeeId` null, and
  `OccurredAt` in the §2 rule 7 format.
- **AC-2** Given an in-stock asset (AST-1003 in the seed), when it is assigned to an employee, then
  its newest event is `Assigned`, from `InStock` to `Assigned`, with that employee's id.
- **AC-3** Given an asset that was created, assigned and then returned, when its history is read,
  then it holds three events newest first: `Returned` (from `Assigned` to `InStock`, with the
  employee who held it), `Assigned`, `Created`, with strictly decreasing ids.
- **AC-4** Given an in-stock asset, when `SetStatus(Repair)` runs, then its newest event is
  `StatusChanged` from `InStock` to `Repair`, `EmployeeId` null.
- **AC-5** Given an assigned asset, when `SetStatus(Retired)` runs, then exactly one event is added,
  `StatusChanged` from `Assigned` to `Retired`, and the asset has no assignment.
- **AC-6** Given an asset in repair, when `SetStatus(Repair)` runs again, then it succeeds and no
  event is added.
- **AC-7** Given an asset in repair (AST-1004 in the seed), when `Assign` is called, then it throws
  `AssetDeskException` with `AST-1004 is in repair and cannot be assigned.` (its own tag) and no
  event is added.
- **AC-8** Given an in-stock asset, when `Assign` is called with a fabricated employee id, then it
  throws `That asset no longer exists. Refresh to see current data.`, the asset is still in stock,
  and no event is added.
- **AC-9** Given an existing tag, when `CreateAssets` runs with quantity 3 and the second tag
  collides, then it throws `Tag {tag} is already in use.` and neither assets nor events are added.
- **AC-10** Given an asset id that does not exist, when `GetHistory` is called, then it throws
  `AssetDeskException` with `That asset no longer exists. Refresh to see current data.`
  An existing asset with no events returns an empty list.
- **AC-11** (curl) `GET /api/assets/{id}/history` returns 200 and a JSON array in the shape of §4.4,
  newest first. `GET /api/assets/nope/history` returns 400
  `{ "error": "That asset no longer exists. Refresh to see current data." }`.
- **AC-12** (by hand) Every row in the Assets table has a **History** button. After Return then
  Assign on the same row, reopening History shows `Assigned to …` above `Returned by …`. A seeded
  asset with no activity shows `No history recorded yet.`

## 7. Tests

This section amends SPEC.md §2 ("No tests, no CI, no Docker") for this feature only: one xUnit
project is allowed. CI and Docker stay out.

```
AssetDesk/                        unchanged layout, plus the files named in §4
AssetDesk.Tests/
  AssetDesk.Tests.csproj          net10.0; xunit, xunit.runner.visualstudio, Microsoft.NET.Test.Sdk;
                                  ProjectReference to ../AssetDesk/AssetDesk.csproj
  AssetHistoryTests.cs            one [Fact] per AC-1 to AC-10, named AC01_… to AC10_…
```

- `AssetDesk.csproj` still has exactly the two packages from SPEC.md §3.1. Test packages live only
  in `AssetDesk.Tests.csproj`. `Microsoft.Data.Sqlite` reaches the tests through the project reference.
- Tests call the repository directly: `new AssetRepository(connectionString)`.
- Each test creates its own temp file,
  `Path.Combine(Path.GetTempPath(), $"assetdesk-test-{Guid.NewGuid()}.db")`, with connection string
  `Data Source={path};Foreign Keys=True;Pooling=False`, creates the schema (SPEC.md §4.5 plus §4.1
  above), inserts one employee with SQL, and deletes the file and its `-wal`/`-shm` siblings in
  `Dispose`. No test opens `AssetDesk/data/assetdesk.db`.
- There is no solution file. Build and test with the project named:
  `dotnet build AssetDesk` and `dotnet test AssetDesk.Tests`.
- Write the tests first. Run `dotnet test AssetDesk.Tests` and see them fail (they will not compile
  until `GetHistory` and `AssetEventType` exist) before touching `AssetRepository`.

## 8. Done when

- [ ] `dotnet build AssetDesk` succeeds with zero warnings
- [ ] `dotnet test AssetDesk.Tests` passes all ten tests
- [ ] `git diff --stat SPEC.md` is empty
- [ ] `AssetDesk.csproj` still lists exactly `Dapper` and `Microsoft.Data.Sqlite`
- [ ] Starting the app against an existing `assetdesk.db` creates `asset_events` without a reset
- [ ] curl: return and re-assign AST-1001, then its history shows `Assigned` above `Returned`
- [ ] curl: `GET /api/assets/nope/history` returns 400 with the §5 message verbatim
- [ ] curl: assigning AST-1004 (in repair) returns 400, and its history is still `[]`
- [ ] By hand: AC-12
- [ ] Nothing in §3 has been built
- [ ] No `bin/`, `obj/` or `*.db` file is committed

## 9. Working agreement for the agent

1. Read SPEC.md and this file before writing code. Do not edit SPEC.md.
2. Write the failing tests from §6 first, run them, then implement until they pass.
3. Use the exact names in §4: `asset_events` and its columns, `AssetEventType`, `AssetEvent`,
   `GetHistory`, `HistoryDialog.razor`, `/api/assets/{id}/history`.
4. Every event insert uses the mutation's own connection and `tx`. No second connection, no insert
   after `tx.Commit()`.
5. Everything in SPEC.md §2 still holds except the one amendment in §7.
6. If something here is ambiguous, pick the simplest reading, state the assumption in one line, and
   keep going.
7. Commit once the tests pass, with the feature name in the message. Anything beyond this file goes
   under "Possible next steps" in your final message, not in the code.
