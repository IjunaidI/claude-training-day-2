---
name: blazor-component-hygiene
description: Use when writing or changing .razor components in AssetDesk - caps component size, keeps logic out of markup, forbids duplicate DTO declarations, and requires enum values over string literals.
---

# Components stay small and dumb

## 1. One component, one job

A component that fetches data, holds page state, filters it, and renders three views is four
responsibilities in one file. Extract the inner pieces into components under `Components/Shared/` and
pass them parameters.

`SPEC.md` §3.2 already names the components this app has — `Dashboard`, `AssetsTable`,
`AddAssetDrawer`, `AssignDialog`, `People`, `StatusPill`. Keeping them is not optional; collapsing two
of them into one file is a violation even if the result is short.

**Numeric tripwire: 250 lines.** Past that, a `.razor` file is doing too much and needs splitting. A
dense table component in the low 200s is fine — the line count is a signal, the responsibility count
is the rule.

## 2. No business logic in markup

`@if` on a field is fine. Filtering, sorting, totalling, and formatting belong behind a named member
in `@code`, or in `Data/Format.cs` where `SPEC.md` put them.

```razor
@* NEVER *@
@foreach (var a in Assets.Where(x => x.Status == Status.InStock && x.Cost > 500).OrderBy(x => x.Tag))

@* CORRECT *@
@foreach (var a in VisibleAssets)

@code {
    private IEnumerable<AssetDto> VisibleAssets =>
        Assets.Where(a => a.Status == Status.InStock && a.Cost > 500).OrderBy(a => a.Tag);
}
```

## 3. Declare each DTO once

One definition, in `Data/Dtos.cs`, shared by every component that needs it.

```csharp
// NEVER — a second source of truth for a shape Data/Dtos.cs already defines
@code {
    private record Row(string Tag, string Category, string Status, string AssignedTo, double Cost);
}
```

It will drift from the real shape, and the drift shows up as a rendering bug nobody can trace.

## 4. Enum values come from the enum

`Status`, `Category`, and `Condition` are enums in `Data/Models.cs`. Never compare them as strings.

```csharp
// NEVER
private int InStockCount => Assets.Count(a => a.Status.ToString() == "InStock");

// CORRECT
private int InStockCount => Assets.Count(a => a.Status == Status.InStock);
```

A string literal is unchecked: rename a member and the compiler stays silent while the count silently
goes to zero. `SPEC.md` §4.3 covers how these are stored and §4.1 names every member — use them.
