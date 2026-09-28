# Reference build — the five planted defects (instructor only)

`AssetDesk/` here is the finished build that ships on the training repo's `reference-build` branch
as the fallback for attendees whose own build stalls. It follows the `api-boundary` skill (the UI
goes through the HTTP API) and carries five quality defects, planted on purpose so that Lab 3 has
something to find. The labelling comments were removed so the reviewer and the attendees read the
code cold. This file is not copied to the student branch.

| # | Defect | Where | Breaks | Caught by |
|---|---|---|---|---|
| 1 | Swallowed exception: `_error = ""`, so the banner renders empty | `Components/Shared/AssignDialog.razor`, the `catch (AssetDeskException)` | SPEC.md §4.6, §9 | SPEC bucket before Lab 3; `csharp-quality` rule 1 after |
| 2 | `<NoWarn>…;CS8602</NoWarn>` hiding a real null dereference in `AssigneeName` | `AssetDesk.csproj`; `Components/Pages/Home.razor` | §9 "zero warnings" in spirit | invisible before Lab 3; `csharp-quality` rule 2 after |
| 3 | Server-side guard commented out: `// ValidateNewAsset(input);` | `Data/AssetRepository.cs`, `CreateAssets` | §4.6, §9 (curl bypasses validation) | SPEC; `csharp-quality` rule 3 |
| 4 | Table inlined into `Home.razor` with a duplicate `record Row` | `Components/Pages/Home.razor` | §3.2 (names `AssetsTable.razor`) | SPEC; `blazor-component-hygiene` rules 1 and 3 |
| 5 | Status compared as a string literal: `a.Status.ToString() == "InStock"` | `Components/Pages/Home.razor` | — | invisible before Lab 3; `blazor-component-hygiene` rule 4 |

Show the room defect 1 at the 12:15 DoD check. A repair row has no Assign button, so use two browser
tabs on AST-1003: open Assign in tab 1, choose Mark repair in tab 2, then confirm the assignment in
tab 1. The repository refuses with `AST-1003 is in repair and cannot be assigned.` and the red banner
comes up empty.

Typical `/review-build` count blocks on this build (a live review is not deterministic; the shape
repeats): before the quality skills `QUALITY: 0`, `SPEC: 3`; after copying both skills in, the same
unchanged code gives five QUALITY/SPEC findings between them.
