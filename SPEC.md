# AssetDesk — IT Asset Management Prototype

**Version** 3.0 · **Status** Ready to build · **Target build time** 80 minutes

> Changed in 3.0: rebuilt for .NET 10 + Blazor Web App (InteractiveServer) + Dapper + hand-written CSS. Replaces the Next.js 16 / TypeScript build in 2.0. SQLite stays. All screen behaviour in section 5 and all visual design intent in section 6 are unchanged since 1.1 — only the mechanism for expressing them differs.

---

## 1. Goal

A single-page internal tool for a ~50-person company's IT admin to answer three questions fast:

1. What hardware do we own?
2. Who has it right now?
3. What's sitting in the store room, ready to hand out?

This is a **prototype for a demo**, not a production system. Correct behaviour, a real persistence layer, and a clean, dense UI matter. Scale, auth, and deployment do not.

## 2. Non-goals — do not build these

Explicitly out of scope. Adding any of these fails the spec:

- No login, users, roles, identity, or `[Authorize]`
- No Entity Framework Core, no migrations, no `DbContext` — hand-written SQL through Dapper only
- No repository interfaces, no service layer, no AutoMapper, no MediatR, no Result<T> wrapper. One concrete `AssetRepository` class
- No Tailwind, Bootstrap, MudBlazor, or any CSS or component framework
- No Blazor routing beyond the single `/` page — tab state is a field on the page component. Do not add `/assets` or `/people` routes
- No component-scoped CSS (`.razor.css`). One stylesheet, `wwwroot/app.css`
- No JavaScript interop unless section 5.3 forces it
- No tests, no CI, no Docker
- No dark mode, no theme switcher
- No CSV import/export, no printing, no PDF
- No charts or graphs
- No date pickers beyond `<input type="date">`
- No animation beyond CSS hover/focus transitions

If something is not described in this document, do not build it.

## 3. Tech constraints

### 3.1 Create the project

```
dotnet new blazor -o AssetDesk -f net10.0 --interactivity Server --all-interactive --empty
cd AssetDesk
dotnet add package Dapper
dotnet add package Microsoft.Data.Sqlite
```

Those four commands are identical on macOS, Windows, and Linux. Run them with whatever shell the host provides.

Target framework is `net10.0`. Do not target `net8.0` or `net9.0`, and do not use pre-.NET-8 Blazor Server patterns (`_Host.cshtml`, `Startup.cs`, `App.razor` as a router). If your SDK rejects `--empty`, create it without that flag and delete every sample page, the `NavMenu` component, and the Bootstrap link in `Components/App.razor` before starting M0.

Exactly two packages. Nothing else may appear in the `.csproj` when you are done.

### 3.2 File layout

```
AssetDesk/
  AssetDesk.csproj
  Program.cs                      # DI, schema init, the six API endpoints
  Components/
    App.razor                     # render mode set here
    Routes.razor
    _Imports.razor
    Layout/
      MainLayout.razor
      Sidebar.razor
    Pages/
      Home.razor                  # the only page — owns all state
    Shared/
      Dashboard.razor
      AssetsTable.razor
      AddAssetDrawer.razor
      AssignDialog.razor
      People.razor
      StatusPill.razor
  Data/
    Models.cs                     # records and enums
    Db.cs                         # connection string, schema, seed
    AssetRepository.cs            # every SQL statement in the app
    Format.cs                     # enum <-> db strings, labels, currency
  wwwroot/
    app.css                       # the entire stylesheet
  data/
    assetdesk.db                  # gitignored
```

Create `AssetDesk/.gitignore` ignoring the build output and the database **by name**:

```
bin/
obj/
assetdesk.db
assetdesk.db-shm
assetdesk.db-wal
```

Do **not** put `data/` in `.gitignore`. On a case-insensitive filesystem — the default on both macOS
(APFS) and Windows (NTFS) — `Data/` (the source folder listed above) and `data/` (the database folder)
are the same physical directory, so a `data/` rule also matches `Data/Models.cs`, `Db.cs`,
`AssetRepository.cs` and `Format.cs`. The whole data layer disappears from `git status` and stops being
committed, while the build keeps succeeding. Ignoring the three sqlite files by name is equivalent and
safe on every filesystem.

### 3.3 Five .NET and Blazor details that will cost you time if missed

**1. Interactivity.** Without an interactive render mode every button on the page silently does nothing and the app looks broken while compiling cleanly. `--all-interactive` sets this in `Components/App.razor`:

```razor
<HeadOutlet @rendermode="InteractiveServer" />
<Routes @rendermode="InteractiveServer" />
```

Verify it at the end of M2 by clicking a tab, before building anything else. This is the single most common Blazor failure and it is invisible in the build output.

**2. `PRAGMA foreign_keys` is per-connection, not per-database.** `Microsoft.Data.Sqlite` pools connections, so setting it once at init does nothing for later connections and invariant 5 silently stops holding. Put it in the connection string instead:

```csharp
$"Data Source={dbPath};Foreign Keys=True"
```

**3. Resolve the database path from the content root, and create the folder.** SQLite creates the file but not the directory, and the database itself is gitignored, so a fresh clone can arrive with no `data/` folder at all. In `Program.cs`, before building the app:

```csharp
var dataDir = Path.Combine(builder.Environment.ContentRootPath, "data");
Directory.CreateDirectory(dataDir);
var dbPath = Path.Combine(dataDir, "assetdesk.db");
```

**4. `cost` is `double`, never `decimal`.** `Microsoft.Data.Sqlite` stores `decimal` as TEXT to preserve precision, which quietly breaks numeric ordering and comparison. The column is `REAL` and the property is `double`.

**5. `@bind` fires on change, not on input.** The search box in 5.2 must filter as you type, which needs:

```razor
<input @bind="_search" @bind:event="oninput" />
```

Plain `@bind` only updates on blur and the search will feel broken.

### 3.4 Architecture

| Decision | Choice |
|---|---|
| UI | Blazor Web App, `InteractiveServer` render mode, global |
| Data access | `AssetRepository` only. Registered as a singleton. No component contains SQL |
| SQL | Dapper over `Microsoft.Data.Sqlite`. Synchronous — see below |
| Connections | One `using var conn = new SqliteConnection(_connectionString)` per repository method. Dapper opens it |
| Transactions | `conn.BeginTransaction()` for every mutation, passed to each `Execute` |
| Schema init | Runs once in `Program.cs` before `app.Run()` |
| Page state | `Home.razor` holds `AppState` and the active tab in private fields, passes them down as parameters |
| Refresh | After any mutation, `Home.razor` re-reads `repo.GetState()` and reassigns the field |

**Synchronous data access is deliberate.** Dapper's async methods would be correct for a networked database, but SQLite here is a local file, the queries are sub-millisecond, and synchronous calls in Blazor event handlers avoid every `StateHasChanged` and `await` ordering trap. State the trade-off out loud; do not silently switch to async.

**Components call the repository directly.** There is no HTTP between the UI and the data. This is the main advantage of Blazor Server for this app and it deletes the fetch layer, the JSON serialisation, and the error-code translation that a SPA would need.

**The six API endpoints exist anyway.** `Program.cs` maps them, and the UI never calls them. They exist so that M0 and M1 are verifiable with `curl` before a single component is written, and so that section 9 can prove the invariants hold against requests that bypass the UI entirely. Both paths go through `AssetRepository`, so there is no duplicated logic.

Icons, where needed, are inline SVG or a single Unicode character. Keep it boring.

## 4. Data model

### 4.1 Models

`Data/Models.cs`. Use these names verbatim.

```csharp
public enum Category { Laptop, Monitor, Headset, Dock, Phone, Keyboard, Other }
public enum Status   { InStock, Assigned, Repair, Retired }
public enum Condition { New, Good, Fair, Poor }

public record Employee(
    string Id,          // Guid.NewGuid().ToString()
    string Name,
    string Email,
    string Department,
    string Title);

public record Asset(
    string Id,
    string Tag,             // unique, format "AST-1001"
    Category Category,
    string Make,            // "Apple"
    string Model,           // "MacBook Pro 14 M3"
    string Serial,
    Status Status,
    Condition Condition,
    string PurchaseDate,    // "2024-03-11", ISO date, no time
    double Cost,            // 2400
    string Location,        // "HQ / Store Room"
    string Notes,           // may be ""
    string? AssignedTo,     // Employee.Id
    string? AssignedDate);  // ISO date

public record AppState(List<Employee> Employees, List<Asset> Assets);

public record NewAssetInput(
    string Tag,
    Category Category,
    string Make,
    string Model,
    string Serial,
    Condition Condition,
    string PurchaseDate,
    double Cost,
    string Location,
    string Notes,
    int Quantity);          // 1-20

public class AssetDeskException(string message) : Exception(message);
```

The enums are load-bearing. A typo in a status is a compile error, and the same rule is repeated as a `CHECK` in 4.4 so values arriving from outside C# are rejected too. `AssignedTo` and `AssignedDate` are the only nullable reference types in the model — keep nullable reference types enabled and do not silence a warning with `!`.

`AssetDeskException` carries a message that is safe to show a user directly. Every message in the table in 4.6 is thrown as one of these, and the UI displays `ex.Message` verbatim.

### 4.2 Naming

C# is PascalCase. The database is snake_case (`purchase_date`, `assigned_to`). Enums are C# names; the database stores the strings in 4.3. Convert in exactly two private methods in `AssetRepository` — `MapAsset(AssetRow)` and `MapEmployee(EmployeeRow)` — reading from private `record AssetRow(...)` and `record EmployeeRow(...)` types that mirror the columns exactly. No component ever touches a row type.

### 4.3 Enum storage

`Data/Format.cs` owns both directions. There are three lookups, not scattered `switch` statements.

| C# | Database | UI label |
|---|---|---|
| `Status.InStock` | `in_stock` | In stock |
| `Status.Assigned` | `assigned` | Assigned |
| `Status.Repair` | `repair` | In repair |
| `Status.Retired` | `retired` | Retired |
| `Condition.New` / `Good` / `Fair` / `Poor` | `new` / `good` / `fair` / `poor` | New / Good / Fair / Poor |
| `Category.*` | same as the C# name | same as the C# name |

Currency is a single constant `const string Currency = "USD"` formatted through `Format.Money(double)` using `CultureInfo.InvariantCulture`. One line to change.

Dates are `CultureInfo.InvariantCulture` too. Every date in this app is an ISO `yyyy-MM-dd` string, and a machine whose locale is not English must still produce `2024-03-11` — so format with an explicit `"yyyy-MM-dd"` and parse with `DateTime.TryParseExact`, never the culture-sensitive default overloads.

### 4.4 Invariants

The database enforces what it can. The repository enforces the rest. The UI assumes neither.

| # | Invariant | Enforced by |
|---|---|---|
| 1 | `tag` is unique across all assets | `UNIQUE` constraint |
| 2 | If `status = 'assigned'`, both `assigned_to` and `assigned_date` are non-null | table `CHECK` |
| 3 | If `status <> 'assigned'`, both `assigned_to` and `assigned_date` are null | table `CHECK` |
| 4 | `category`, `status`, `condition` only hold listed values | column `CHECK` + C# enum |
| 5 | `assigned_to` references a real employee | `FOREIGN KEY` + `Foreign Keys=True` in the connection string |
| 6 | Only an asset with `status = 'in_stock'` can be assigned | repository, throws `AssetDeskException` |
| 7 | Retiring an assigned asset clears the assignment in the same transaction | repository |
| 8 | Employees are never deleted — assign and return only | no method exists |

Invariants 2 and 3 are the ones to show students: a rule written in prose became a `CHECK`, and a bug that was possible became impossible.

### 4.5 Schema

`Data/Db.cs` runs this once at startup. `IF NOT EXISTS` throughout, so a restart is safe.

```sql
PRAGMA journal_mode = WAL;

CREATE TABLE IF NOT EXISTS employees (
  id         TEXT PRIMARY KEY,
  name       TEXT NOT NULL,
  email      TEXT NOT NULL UNIQUE,
  department TEXT NOT NULL,
  title      TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS assets (
  id            TEXT PRIMARY KEY,
  tag           TEXT NOT NULL UNIQUE,
  category      TEXT NOT NULL CHECK (category IN
                  ('Laptop','Monitor','Headset','Dock','Phone','Keyboard','Other')),
  make          TEXT NOT NULL,
  model         TEXT NOT NULL,
  serial        TEXT NOT NULL,
  status        TEXT NOT NULL CHECK (status IN
                  ('in_stock','assigned','repair','retired')),
  condition     TEXT NOT NULL CHECK (condition IN ('new','good','fair','poor')),
  purchase_date TEXT NOT NULL,
  cost          REAL NOT NULL CHECK (cost >= 0),
  location      TEXT NOT NULL,
  notes         TEXT NOT NULL DEFAULT '',
  assigned_to   TEXT REFERENCES employees(id),
  assigned_date TEXT,
  CHECK (
    (status =  'assigned' AND assigned_to IS NOT NULL AND assigned_date IS NOT NULL) OR
    (status <> 'assigned' AND assigned_to IS     NULL AND assigned_date IS     NULL)
  )
);

CREATE INDEX IF NOT EXISTS idx_assets_status   ON assets(status);
CREATE INDEX IF NOT EXISTS idx_assets_assigned ON assets(assigned_to);
```

`journal_mode = WAL` matters: you will query this file with the `sqlite3` CLI while the app holds it open.

Note that `PRAGMA foreign_keys` is deliberately absent here — see 3.3 item 2. It belongs in the connection string.

### 4.6 Repository surface and errors

```csharp
AppState     GetState();
List<Asset>  CreateAssets(NewAssetInput input);
Asset        Assign(string assetId, string employeeId, string assignedDate);
Asset        Return(string assetId);
Asset        SetStatus(string assetId, Status status);
```

Every failure throws `AssetDeskException` with one of these messages. The UI shows the message verbatim and never writes its own copy.

| Case | Message |
|---|---|
| Missing or invalid field | `Cost must be a number of 0 or more.` |
| Duplicate tag | `Tag AST-1004 is already in use.` |
| Assigning a non-in-stock asset | `AST-1004 is in repair and cannot be assigned.` |
| Unknown asset or employee id | `That asset no longer exists. Refresh to see current data.` |

**Translate SQLite constraint failures rather than pre-checking.** Do not `SELECT` first to see whether a tag is taken — let the constraint fire and catch it. `Microsoft.Data.Sqlite` exposes an extended error code that tells you which kind:

```csharp
catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == 2067)  // UNIQUE
catch (SqliteException ex) when (ex.SqliteExtendedErrorCode == 1811)  // CHECK
```

Validate `NewAssetInput` with a hand-written guard method in the repository. No validation library, no data annotations.

### 4.7 API endpoints

Mapped in `Program.cs`. The UI does not use them. They exist for command-line verification during M0 and M1 and for the invariant tests in section 9.

| Method | Path | Body | Returns |
|---|---|---|---|
| GET | `/api/health` | — | `{ "ok": true }` |
| GET | `/api/state` | — | `AppState` |
| POST | `/api/assets` | `NewAssetInput` | created assets, 201 |
| POST | `/api/assets/{id}/assign` | `{ employeeId, assignedDate }` | the asset |
| POST | `/api/assets/{id}/return` | — | the asset |
| POST | `/api/assets/{id}/status` | `{ status }` | the asset |

Catch `AssetDeskException` in a single place and return `{ "error": ex.Message }` with 400. `System.Text.Json` serialises PascalCase properties to camelCase by default, which matches the JSON above with no configuration. Configure enum serialisation as strings with `JsonStringEnumConverter`.

## 5. Screens

One page. A fixed left sidebar plus a content area. Three tabs: **Dashboard**, **Assets**, **People**. Tab state is a private field on `Home.razor`.

---

### 5.1 Dashboard

Read-only overview. All figures are computed in the component from `AppState` — no extra query.

**Top row — four stat tiles**, each a large number and a label:

- Total assets (excludes `Retired`)
- In stock
- Assigned
- In repair

**Below — stock by category table.** Columns: Category · Total owned · In stock · Assigned · Stock level.

Stock level is a badge derived from the in-stock count:

| In stock | Badge |
|---|---|
| 0 | **Out** (red) |
| 1–2 | **Low** (amber) |
| 3+ | **OK** (green) |

**Acceptance criteria**

- [ ] Tile numbers recompute immediately after any assign, return, add, or retire action
- [ ] Retired assets are excluded from every tile and from "Total owned"
- [ ] Categories with zero assets do not appear
- [ ] Clicking a category row switches to the Assets tab with that category filter applied

---

### 5.2 Assets

The main screen. A toolbar above a table.

**Toolbar (single row, left to right)**

1. Search input — matches `Tag`, `Make`, `Model`, or `Serial`, case-insensitive, substring
2. Category select — "All categories" plus each category
3. Status select — "All statuses" plus each status label
4. Spacer
5. **Add asset** button (primary), opens the Add asset drawer

**Table columns**

| Column | Content |
|---|---|
| Tag | Monospace, e.g. `AST-1001` |
| Category | Text |
| Device | `Make` + `Model` on one line, `Serial` in small muted text beneath |
| Status | `StatusPill` component |
| Assigned to | Employee name, or `—` |
| Since | `AssignedDate`, or `—` |
| Location | Text |
| Actions | Buttons, see below |

**Row actions** — show only what is legal for that row's status:

| Status | Buttons shown |
|---|---|
| `InStock` | Assign · Mark repair · Retire |
| `Assigned` | Return · Retire |
| `Repair` | Mark in stock · Retire |
| `Retired` | *(none)* |

**Acceptance criteria**

- [ ] Search and both filters combine with AND
- [ ] Search filters as you type — see 3.3 item 5
- [ ] A result count reads "Showing 7 of 12 assets" above the table and updates live
- [ ] Empty result shows: heading "No assets match these filters", plus a **Clear filters** button that resets search and both selects
- [ ] Table is sorted by `Tag` ascending by default
- [ ] Clicking a column header for Tag, Category, or Status toggles ascending/descending sort on that column
- [ ] Retired rows render at 60% opacity
- [ ] A repository error surfaces as a dismissible banner above the table showing the exception message verbatim

---

### 5.3 Add asset drawer

Slides in from the right, 420px wide, translucent backdrop. Clicking the backdrop closes it.

**Fields, in order**

| Field | Control | Required | Default |
|---|---|---|---|
| Tag | text | yes | next free `AST-####` pre-filled |
| Category | select | yes | Laptop |
| Make | text | yes | — |
| Model | text | yes | — |
| Serial | text | yes | — |
| Condition | select | yes | New |
| Purchase date | `<input type="date">` | yes | today |
| Cost | number, min 0 | yes | — |
| Location | text | yes | `HQ / Store Room` |
| Notes | textarea, 3 rows | no | — |
| Quantity | number, 1–20 | yes | 1 |

Do not use `EditForm` or `DataAnnotationsValidator`. Bind fields to a local `NewAssetInput` and validate in a method on the component.

**Quantity** is the stock-entry shortcut: `CreateAssets` with quantity `n` creates `n` assets with identical details and sequential tags from the entered tag. Serial gets a `-1`, `-2` … suffix when `n > 1`. The repository expands this inside one transaction — either all `n` rows are written or none are.

New assets are always created with `Status.InStock`, `AssignedTo = null`, `AssignedDate = null`. The form does not offer these; the repository sets them.

The pre-filled tag is `AST-` plus one above the highest numeric suffix currently in state. It is a convenience, not a reservation — the unique constraint is what guarantees correctness.

**Escape to close** requires `tabindex="-1"` on the drawer root plus `@onkeydown`, and focusing the element when it opens via `@ref` and `ElementReference.FocusAsync()`. If this fights you, cut it — backdrop click is enough. Do not reach for JS interop.

**Acceptance criteria**

- [ ] Submitting with a required field empty shows a red message under that field and calls no repository method
- [ ] A duplicate tag shows the exception message under the Tag field and leaves the drawer open with the form intact
- [ ] On success the drawer closes and the new rows appear after the state refresh
- [ ] Reopening the drawer shows a fresh empty form with a newly pre-filled tag
- [ ] Submitting quantity 3 with a tag that collides on the second row creates zero assets, not one

---

### 5.4 Assign dialog

Opened by the **Assign** row action. A small centred modal.

- Shows the asset tag and device name as static text
- One select: employee, listing `Name — Department`, with a "Select an employee" placeholder
- One `<input type="date">` for assignment date, defaulting to today
- Buttons: **Cancel** and **Assign asset**

On confirm the repository sets `assigned_to`, `assigned_date`, and `status = 'assigned'` in one transaction.

**Return** takes no dialog. It clears the assignment and sets `status = 'in_stock'` immediately.

**Acceptance criteria**

- [ ] **Assign asset** is disabled until an employee is selected
- [ ] After assigning, the row updates and the Dashboard counts change
- [ ] Returning an asset makes it immediately assignable again

---

### 5.5 People

A table of employees. Columns: Name · Title · Department · Email · Assets held (count).

Clicking a row expands an inline panel beneath it listing that person's assigned assets as `Tag — Make Model`, each with a **Return** button.

**Acceptance criteria**

- [ ] Assets-held count matches the Assigned-to column on the Assets tab
- [ ] A person with zero assets shows a `0` count and expands to "No assets assigned"
- [ ] Returning from this panel updates the count without collapsing the panel

## 6. Visual design

Utilitarian ops console. Dense, quiet, high information per pixel. Nothing decorative.

Everything lives in `wwwroot/app.css`. Replace the template's contents entirely and remove any Bootstrap link from `Components/App.razor`. Target roughly 200 lines. Write semantic classes — `.tile`, `.table`, `.pill`, `.btn`, `.btn-primary`, `.drawer`, `.modal`, `.toolbar` — not utilities. No `.razor.css` files.

**Colour tokens**

```css
:root {
  --canvas:  #ECEEF0;   /* app background */
  --surface: #FFFFFF;   /* cards, table, drawer */
  --ink:     #16191D;   /* primary text */
  --muted:   #6E747D;   /* secondary text, serials, labels */
  --line:    #D8DCE0;   /* borders, dividers */
  --accent:  #0F5C58;   /* primary buttons, active tab, focus ring */
  --warn:    #B45309;
  --danger:  #A82C22;
  --info:    #1F4E9C;
}
```

Status pills: In stock → `--accent`, Assigned → `--info`, In repair → `--warn`, Retired → `--muted`. Tinted background using `color-mix(in srgb, var(--accent) 12%, transparent)` with the solid colour as text. No outlines.

**Type**

- UI: system stack (`system-ui, -apple-system, "Segoe UI", sans-serif`). No web fonts.
- Data: `ui-monospace, "Cascadia Mono", Consolas, monospace` for asset tags, serials, and every number in a table cell or stat tile. This is the signature of the interface: identifiers read as identifiers.
- Scale: stat tile numbers 32px/600 · section headings 15px/600 · body and table 14px/400 · labels and serials 12px/500 uppercase with 0.04em tracking for column headers only.

**Layout**

- Sidebar 220px fixed, `--surface`, right border `--line`. Wordmark "AssetDesk" at top, three tab buttons beneath. Active tab: 3px `--accent` left bar and `--accent` text.
- Content area max-width 1200px, 32px padding.
- Table rows 44px tall, 1px `--line` bottom border, `--surface` background, hover row background `--canvas`.
- Border radius 6px everywhere. Shadows only on the drawer and modal.

**Quality floor** — not optional, but do not spend time announcing it:

- Every interactive element has a visible `:focus-visible` ring in `--accent`
- Buttons and inputs have a hover state
- Layout does not break below 1024px (the table may scroll horizontally)

**Copy rules**

- Sentence case for all buttons and headings. "Add asset", not "Add Asset".
- A button's verb matches its result. "Assign asset" → the row shows "Assigned".
- Empty states are an instruction, not an apology: "No assets match these filters."
- Errors state what happened and what to do: "Tag AST-1004 is already in use."

## 7. Seed data

`Data/Db.cs` seeds at startup if `SELECT COUNT(*) FROM assets` returns 0. Insert exactly this, in one transaction, employees first. Generate ids with `Guid.NewGuid().ToString()`; the `emp-N` references map to the employee at that index.

To reset, stop the app, delete `assetdesk.db` **together with its `-wal` and `-shm` siblings**, and restart. Deleting only the base file leaves a confusing half-state, because WAL mode keeps live pages in the sidecar files. The exact command differs per shell — see `INSTRUCTIONS-MACOS.md` or `INSTRUCTIONS-WINDOWS.md` §5.

**Employees**

| # | name | email | department | title |
|---|---|---|---|---|
| 1 | Aisha Rahman | aisha.rahman@northwind.co | Engineering | Backend Engineer |
| 2 | Daniel Okafor | daniel.okafor@northwind.co | Design | Product Designer |
| 3 | Mei Tanaka | mei.tanaka@northwind.co | Sales | Account Executive |
| 4 | Lucas Moreau | lucas.moreau@northwind.co | Engineering | QA Engineer |
| 5 | Priya Nair | priya.nair@northwind.co | People Ops | HR Generalist |

**Assets**

| tag | category | make | model | serial | status | assigned_to | assigned_date | condition | purchase_date | cost | location |
|---|---|---|---|---|---|---|---|---|---|---|---|
| AST-1001 | Laptop | Apple | MacBook Pro 14 M3 | C02XK1QF | assigned | emp-1 | 2024-04-02 | good | 2024-03-28 | 2400 | HQ / Floor 3 |
| AST-1002 | Laptop | Apple | MacBook Air 13 M2 | C02YT8LM | assigned | emp-2 | 2024-06-17 | good | 2024-06-10 | 1350 | HQ / Floor 2 |
| AST-1003 | Laptop | Lenovo | ThinkPad X1 Carbon G11 | PF3K92XA | in_stock | — | — | new | 2025-01-15 | 1800 | HQ / Store Room |
| AST-1004 | Laptop | Lenovo | ThinkPad T14 G4 | PF2M40BC | repair | — | — | fair | 2023-09-04 | 1200 | HQ / IT Bench |
| AST-1005 | Monitor | Dell | UltraSharp U2723QE | CN0TX41A | assigned | emp-1 | 2024-04-02 | good | 2024-03-28 | 620 | HQ / Floor 3 |
| AST-1006 | Monitor | Dell | UltraSharp U2723QE | CN0TX41B | in_stock | — | — | good | 2024-03-28 | 620 | HQ / Store Room |
| AST-1007 | Headset | Sony | WH-1000XM5 | S5H88201 | assigned | emp-3 | 2025-02-03 | good | 2025-01-29 | 380 | HQ / Floor 1 |
| AST-1008 | Headset | Jabra | Evolve2 65 | JB2065X1 | in_stock | — | — | new | 2025-05-20 | 210 | HQ / Store Room |
| AST-1009 | Headset | Jabra | Evolve2 65 | JB2065X2 | in_stock | — | — | new | 2025-05-20 | 210 | HQ / Store Room |
| AST-1010 | Dock | CalDigit | TS4 Thunderbolt | CD4TS091 | assigned | emp-2 | 2024-06-17 | good | 2024-06-10 | 380 | HQ / Floor 2 |
| AST-1011 | Phone | Apple | iPhone 15 | F17GK220 | assigned | emp-3 | 2024-11-11 | good | 2024-11-05 | 900 | HQ / Floor 1 |
| AST-1012 | Keyboard | Keychron | K3 Pro | KC3P7741 | retired | — | — | poor | 2022-08-19 | 95 | HQ / Store Room |

Notes are `''` for all seeded assets. This seed deliberately produces one Out badge (Dock), several Low badges, and one retired row, so every visual state is visible on first load.

## 8. Build order

Work in this order. Verify each milestone before moving on.

| # | Milestone | Time | Done when |
|---|---|---|---|
| M0 | `Models.cs`, `Format.cs`, `Db.cs` (schema + seed), connection string, DI wiring, `GetState`, `/api/health`, `/api/state` | 14 min | `curl localhost:5xxx/api/state` returns 5 employees and 12 assets |
| M1 | The four mutation methods with transactions and the full error table, plus their four endpoints | 12 min | Create, assign, return, and status all work from `curl` — before any component exists |
| M2 | `app.css` tokens, `MainLayout`, `Sidebar`, `Home.razor` with tab state, `StatusPill` | 10 min | Clicking a tab changes the view — this proves interactivity is on |
| M3 | Assets table with all columns | 12 min | All 12 rows render correctly |
| M4 | Search, both filters, result count, empty state, error banner | 8 min | Filters combine correctly and clear |
| M5 | Add asset drawer with validation and quantity | 12 min | Adding 3 headsets creates AST-1013/14/15 in the database |
| M6 | Assign dialog, return, repair, retire | 8 min | An asset can go in stock → assigned → in stock |
| M7 | Dashboard tiles and stock table, People tab | 12 min | Counts match the Assets tab exactly |

M0 and M1 come first on purpose: the data layer is exercisable from the command line before a single component is written, so a later bug is either in the repository or in the UI and never ambiguously both.

Run `dotnet watch` from M2 onward, but expect to restart it manually after structural `.razor` edits — hot reload handles markup and method bodies, not new components or changed parameters.

If a milestone overruns, cut scope inside that milestone rather than skipping the next one. Column sorting (M4), Escape-to-close (M5), and the People expand panel (M7) are the first three things to cut, in that order.

## 9. Definition of done

- [ ] `dotnet build` succeeds with zero warnings, including nullable warnings
- [ ] `dotnet run` starts with no exceptions and no errors in the browser console
- [ ] Clicking a sidebar tab changes the view — interactivity is confirmed, not assumed
- [ ] Deleting the database and its `-wal`/`-shm` siblings and restarting reproduces the exact seed state
- [ ] Every acceptance-criteria checkbox in section 5 passes when clicked through by hand
- [ ] Refreshing the browser preserves all changes, because they are in the database
- [ ] Every invariant in 4.4 holds, including when the request comes from `curl` and bypasses the UI
- [ ] Invariant 5 specifically: a `curl` assign with a fabricated employee id is rejected, proving `Foreign Keys=True` took effect
- [ ] Every repository error surfaces in the UI as the exception message verbatim
- [ ] No `!` null-forgiving operator and no suppressed warnings anywhere
- [ ] The `.csproj` gained exactly `Dapper` and `Microsoft.Data.Sqlite`
- [ ] Nothing from section 2 has been built

## 10. Working agreement for the implementing agent

1. Read this whole document before writing code.
2. Target `net10.0` and current Blazor Web App patterns. Do not use `Startup.cs`, `_Host.cshtml`, or any pre-.NET-8 Blazor structure.
3. Build in the milestone order in section 8. Do not jump ahead to the UI.
4. Use the exact type names, property names, enum members, SQL column names, method signatures, and CSS tokens given here.
5. Resist idiomatic .NET layering. Section 2 rules out interfaces, service layers, and mappers on purpose — this is a prototype and the indirection would cost more than it returns.
6. If something is genuinely ambiguous, pick the simplest reading, state the assumption in one line, and keep going. Do not stop to ask.
7. If you want to add something not in this spec, don't. Note it under a "Possible next steps" heading at the end instead.
8. Report progress as a one-line note per completed milestone. No essays.
9. Nothing in this document is macOS- or Windows-specific, and nothing you write may be. Build every path with `Path.Combine` and never a literal `/` or `\`. Read the host platform from your own environment and use the shell it actually has — the person you are building for should not have to translate a command for you, and will not be told which OS you are on.
