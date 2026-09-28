# Lab 4 notes: build the feature, test first

What the instructor should expect when attendees build `specs/asset-history.md` on top of their
build. The reference spec is `../lab2/asset-history.md`.

There is no spec branch and no merge. Lab 2 wrote the spec in the same folder as the build, so by
Lab 4 it is on `build/<name>`: swept into a milestone commit by the build session, or committed by
the attendee at 12:15 or in Lab 4 step 2. Attendees branch `feature/<name>-asset-history` straight
from their build branch. If `git log --oneline -1 -- specs/asset-history.md` prints nothing, the
spec is not committed; `git add specs/asset-history.md` and commit it on whichever branch they are
on (on the feature branch is fine: the PR then carries the spec, which also lets the CI review read
it).

## Files in this folder

| File | What it is |
|---|---|
| `AssetHistoryTests.cs` | Ten xUnit tests, AC-1 to AC-10, one-to-one with the spec's acceptance criteria |
| `AssetDesk.Tests.csproj` | The test project. Goes in `AssetDesk.Tests/`, next to `AssetDesk/` |
| `reference-implementation.patch` | The feature on top of `../reference-build/AssetDesk/` (239 lines added, 3 removed, 10 files) |

## What a finished attendee repo looks like

```
AssetDesk/
  Data/Models.cs            + AssetEventType, AssetEvent
  Data/Format.cs            + the fourth lookup (event type <-> db string <-> label)
  Data/Db.cs                + CREATE TABLE asset_events ... and its index
  Data/AssetRepository.cs   + GetHistory, a private RecordEvent(conn, tx, ...), AssetEventRow, MapAssetEvent
  Program.cs                + GET /api/assets/{id}/history
  Components/Shared/HistoryDialog.razor   new
  Components/Shared/AssetsTable.razor (or Home.razor)   + History button, + dialog state
AssetDesk.Tests/
  AssetDesk.Tests.csproj
  AssetHistoryTests.cs
specs/asset-history.md
```

A build that talks to its own API from components (like the reference build) also gains an
`AssetEventDto` and a `GetHistoryAsync` on its HTTP client. That is not in the spec, and it is fine.

## Timing

- Stand-in run (Claude, scripted, on the reference build): tests written and red, then the
  implementation, then 10/10 green on the first run. About 3 minutes of edits once the spec existed.
- In the room, with plan mode and a plan to approve: expect 15 to 25 minutes. The plan takes 3 to 5,
  the tests 3 to 5, the implementation 5 to 10, the UI 3 to 5. The 35-minute slot leaves 10 for the
  browser check and the push.

## The check

The lab guide checks in the browser: Return AST-1001, Assign it to Mei Tanaka, click **History**,
expect `Assigned` above `Returned`. The API version below is for you, or for an attendee who asks
(the guide suggests asking Claude to run it, so nobody types a GUID).

Run the app on a fixed port, then look up AST-1001's id. `{id}` is the Guid, not the tag.

macOS / Linux:

```bash
dotnet run --project AssetDesk --urls http://localhost:5198
B=http://localhost:5198
ID=$(curl -s $B/api/state | jq -r '.assets[] | select(.tag=="AST-1001") | .id')
EMP=$(curl -s $B/api/state | jq -r '.employees[] | select(.name=="Mei Tanaka") | .id')
curl -s -X POST $B/api/assets/$ID/return
curl -s -X POST $B/api/assets/$ID/assign -H 'Content-Type: application/json' \
  -d "{\"employeeId\":\"$EMP\",\"assignedDate\":\"2026-09-28\"}"
curl -s $B/api/assets/$ID/history
```

Windows PowerShell:

```powershell
$B = "http://localhost:5198"
$s = Invoke-RestMethod "$B/api/state"
$ID = ($s.assets | Where-Object tag -eq "AST-1001").id
$EMP = ($s.employees | Where-Object name -eq "Mei Tanaka").id
Invoke-RestMethod -Method Post "$B/api/assets/$ID/return"
Invoke-RestMethod -Method Post "$B/api/assets/$ID/assign" -ContentType "application/json" `
  -Body (@{ employeeId = $EMP; assignedDate = "2026-09-28" } | ConvertTo-Json)
Invoke-RestMethod "$B/api/assets/$ID/history" | ConvertTo-Json
```

No `jq`? Open `/api/state` in the browser, copy AST-1001's `"id"`, and paste it into the URL.

### Sample output (reference build + patch, fresh database, port 5199)

History before any action (seeded assets have no backfill):

```
[]
```

After return, then assign to Mei Tanaka:

```json
[{"id":2,"assetId":"2ed473a4-fe6f-4688-9e0a-6c5fca2c2262","type":"Assigned","fromStatus":"InStock","toStatus":"Assigned","employeeId":"fed91692-b3a7-4899-a62b-1f1f7d3bfc29","occurredAt":"2026-09-28T09:24:03Z"},{"id":1,"assetId":"2ed473a4-fe6f-4688-9e0a-6c5fca2c2262","type":"Returned","fromStatus":"Assigned","toStatus":"InStock","employeeId":"64889a07-86bb-49dd-acc1-c318b3f1fcc1","occurredAt":"2026-09-28T09:24:02Z"}]
```

The other checks from the spec's "Done when":

```
$ curl -s -w ' %{http_code}\n' $B/api/assets/nope/history
{"error":"That asset no longer exists. Refresh to see current data."} 400

$ curl -s -w ' %{http_code}\n' -X POST $B/api/assets/$REPAIR_ID/assign ...   # AST-1004
{"error":"AST-1004 is in repair and cannot be assigned."} 400
$ curl -s $B/api/assets/$REPAIR_ID/history
[]

$ curl -s -X POST $B/api/assets -d '{"tag":"AST-1013",...,"quantity":2}'    # 201
$ curl -s $B/api/assets/$AST_1014_ID/history
[{"id":4,"assetId":"d8150f8f-…","type":"Created","fromStatus":null,"toStatus":"InStock","employeeId":null,"occurredAt":"2026-09-28T09:24:04Z"}]
```

### Tests

```
$ dotnet test AssetDesk.Tests --logger "console;verbosity=normal"
  Passed AssetDesk.Tests.AssetHistoryTests.AC01_CreateWithQuantity3_RecordsOneCreatedEventPerAsset
  Passed AssetDesk.Tests.AssetHistoryTests.AC02_Assign_RecordsAssignedEventWithTheEmployee
  Passed AssetDesk.Tests.AssetHistoryTests.AC03_AssignThenReturn_AddsTwoEventsNewestFirst
  Passed AssetDesk.Tests.AssetHistoryTests.AC04_SetStatusRepair_RecordsStatusChanged
  Passed AssetDesk.Tests.AssetHistoryTests.AC05_RetireAssigned_RecordsExactlyOneStatusChanged
  Passed AssetDesk.Tests.AssetHistoryTests.AC06_SetStatusToCurrentStatus_SucceedsAndRecordsNothing
  Passed AssetDesk.Tests.AssetHistoryTests.AC07_AssignAssetInRepair_ThrowsAndRecordsNothing
  Passed AssetDesk.Tests.AssetHistoryTests.AC08_AssignToFabricatedEmployee_ThrowsAndRecordsNothing
  Passed AssetDesk.Tests.AssetHistoryTests.AC09_CreateWithCollidingSecondTag_WritesNoAssetsAndNoEvents
  Passed AssetDesk.Tests.AssetHistoryTests.AC10_History_UnknownIdThrows_KnownIdWithoutEventsIsEmpty
Test Run Successful.
Total tests: 10
     Passed: 10
```

`dotnet build AssetDesk` and `dotnet build AssetDesk.Tests`: 0 warnings, 0 errors.

The tests were also checked against two deliberate bugs: dropping the "no status change, no event"
guard fails AC-6; ordering oldest first fails AC-2, AC-3, AC-4 and AC-5; writing the event on a
second connection fails every test that mutates, with `database is locked`.

## What the tests assume

Written at the top of `AssetHistoryTests.cs`. In short:

1. `new AssetRepository(string connectionString)`. SPEC.md implies it (§3.4 `_connectionString`)
   but never writes it. Every build seen so far (reference build, checkpoint-3, dotnet-demo,
   work/cp1, work/cp2) matches.
2. Nothing about `Db.cs`. SPEC.md does not name the init method, so the tests create the schema
   from SPEC.md §4.5 plus the feature spec's DDL. Swapping in `Db.Initialize(connectionString)` also
   passes 10/10 on the reference build.
3. Models in `AssetDesk.Data` or `AssetDesk`. An empty `namespace AssetDesk.Data { }` in the test
   file keeps the `using` compiling either way.

## Handing the tests to an attendee

Only when they are stuck, or to compare with what Claude wrote:

```bash
mkdir AssetDesk.Tests
cp <path>/lab4/AssetDesk.Tests.csproj <path>/lab4/AssetHistoryTests.cs AssetDesk.Tests/
dotnet test AssetDesk.Tests
```

Before the feature exists this fails to compile (`GetHistory`, `AssetEventType` missing). That is
the red step.

## Applying the reference implementation

From the root of a repo whose `AssetDesk/` is the reference build (for example the
`reference-build` branch):

```bash
git apply --directory=AssetDesk <path>/lab4/reference-implementation.patch
```

Or from inside `AssetDesk/`: `patch -p1 < <path>/lab4/reference-implementation.patch`.
The patch is written against `../reference-build/AssetDesk/` and will not apply cleanly to an
attendee's own build. For their build, hand them the spec and the tests, not the patch.

## Common failure modes

| Symptom | Cause | Fix |
|---|---|---|
| Claude tries to add "tests are allowed" to SPEC.md and the edit is refused | `.claude/settings.json` denies `Edit(SPEC.md)` | Working as intended. Point Claude at the feature spec's §7: the amendment lives there, not in SPEC.md |
| `dotnet test` from the repo root: `MSB1003` | No project in the current folder, and there is no solution file | `dotnet test AssetDesk.Tests` |
| Attendee's seeded data changes after `dotnet test` | Tests open `AssetDesk/data/assetdesk.db` (Claude copied the path logic from `Program.cs`) | Tests must use a temp file per test (spec §7). Reset the DB: stop the app, delete `assetdesk.db` and its `-wal`/`-shm` siblings by name |
| AC-9 fails: one event left after the colliding create | Events written on a second connection, or after `tx.Commit()`, or the insert not passed `tx` | Every event insert takes the mutation's `conn` and `tx`, before `Commit` |
| `dotnet test` takes minutes; tests fail with `SQLite Error 5: 'database is locked'` after 30 s each | Event written on a new connection while the mutation's transaction holds the write lock (reproduced: 4.5 minutes for ten tests) | Same fix: reuse `conn` and `tx` |
| `SqliteException ... CHECK constraint failed` from `SetStatus` | The insert runs even when the status did not change | Skip the insert when from = to (spec §2 rule 3). The CHECK is doing its job |
| `/api/assets/AST-1001/history` returns the unknown-id 400 | `{id}` is the Guid, not the tag | Look up the id from `/api/state` (commands above) |
| History always `[]` for seeded assets | Correct: no backfill (spec §2 rule 8) | Return and re-assign one first |
| Claude adds `Microsoft.AspNetCore.Mvc.Testing`, `FluentAssertions` or `Moq` | Habit | Spec §3 and §7 rule them out. Test packages are exactly the three in the csproj |
| Claude edits `AssetDesk.csproj` to add xunit | Wrong project | Test packages go only in `AssetDesk.Tests.csproj`. SPEC.md §9: exactly two packages in the app |
| `rm -rf AssetDesk/data` deletes the source `Data/` folder | Case-insensitive file system (SPEC.md §3.2) | Delete the three sqlite files by name. `.claude/settings.json` denies `rm -rf` for this reason; if it already happened, `git checkout -- AssetDesk/Data` |

## Getting a stuck attendee unstuck

In this order, stop at the first one that works:

1. **Red but not compiling for the wrong reason.** Read the first error aloud. If it is a name
   (`AssetHistory` instead of `AssetEvent`), ask Claude to "use the exact names in §4 of the spec".
2. **No tests yet after 10 minutes.** Copy in `AssetDesk.Tests/` from this folder (commands above)
   and prompt: `The tests in AssetDesk.Tests are the acceptance criteria. Implement until dotnet test AssetDesk.Tests passes. Do not edit the tests.`
3. **One test stays red.** Paste the failing test name and message to Claude with
   `This is AC-n in specs/asset-history.md. Fix the implementation, not the test.`
4. **Their build is too broken to extend** (behind on Lab 1). Commit, `git switch -c build/<name>-ref origin/reference-build`,
   bring the spec across (`git checkout build/<name> -- specs/asset-history.md`, then commit),
   create the feature branch from there, and apply `reference-implementation.patch` only if the
   clock is under 10 minutes. Otherwise let Claude build it on the reference build; it is the same
   spec.
5. **Out of time.** They still push the branch with whatever passes. Lab 5's PR review works on a
   partial feature and gives them something to fix.
