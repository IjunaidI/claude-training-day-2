---
name: csharp-quality
description: Use when writing or changing C# in AssetDesk - forbids swallowed exceptions, warning suppressions, and endpoints that trust their input.
---

# Three things that must never ship

## 1. No swallowed exceptions

An empty or message-discarding `catch` turns a failure into silence. `SPEC.md` §4.6 requires the UI to
show the repository's message verbatim, so swallowing one breaks the spec as well as the user.

```csharp
// NEVER
try { await Client.AssignAsync(id, employeeId, date, ct); }
catch (AssetDeskException) { }

// NEVER — discards the message the UI is required to show
catch (AssetDeskException) { _error = ""; }

// NEVER — invents copy the spec says not to write
catch (AssetDeskException) { _error = "Something went wrong."; }

// CORRECT
catch (AssetDeskException ex) { _error = ex.Message; }
```

Catch the specific type. If you cannot handle it, let it propagate.

## 2. No suppressions

No `!` null-forgiving operator. No `#pragma warning disable`. No `<NoWarn>` in the `.csproj`. A
warning is information; suppressing it deletes the information and keeps the bug.

```csharp
// NEVER — the .csproj carries <NoWarn>CS8602</NoWarn>, so this compiles clean
// and keeps the NullReferenceException
private string AssigneeName(AssetDto a) =>
    Employees.FirstOrDefault(e => e.Id == a.AssignedTo).Name;

// ALSO NEVER — same bug, suppressed inline instead of in the .csproj
private string AssigneeName(AssetDto a) =>
    Employees.FirstOrDefault(e => e.Id == a.AssignedTo)!.Name;

// CORRECT
private string AssigneeName(AssetDto a) =>
    Employees.FirstOrDefault(e => e.Id == a.AssignedTo)?.Name ?? "—";
```

```csharp
// NEVER
var state = await response.Content.ReadFromJsonAsync<AppStateDto>(ct)!;

// CORRECT
var state = await response.Content.ReadFromJsonAsync<AppStateDto>(ct)
            ?? throw new AssetDeskException("The server returned no data. Refresh to try again.");
```

One documented exception exists: `NU1903` for `SQLitePCLRaw.lib.e_sqlite3`, pinned transitively by
`Microsoft.Data.Sqlite` with no newer patch. If you suppress that one, the `.csproj` carries a comment
saying which package and why.

## 3. Validate at the boundary, not only in the UI

Every endpoint validates its own body. UI validation is a convenience for the user, never a defence —
`curl` bypasses it entirely, and `SPEC.md` §9 explicitly tests that path.

`NewAssetInput` goes through the repository's hand-written guard before any SQL runs, per `SPEC.md`
§4.6. Drawer validation does not replace it, and commenting the guard out because the drawer already
checks is the exact mistake this rule exists to prevent.
