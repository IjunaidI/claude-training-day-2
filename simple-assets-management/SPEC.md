# AssetDesk — IT Asset Management Prototype

**Version** 2.0 · **Status** Ready to build · **Target build time** 75 minutes

> Changed in 2.0: rebuilt for Next.js 16 (App Router) + TypeScript + React 19. The Express server is gone — Route Handlers replace it. SQLite stays. All screen behaviour in section 5 and all visual design in section 6 are unchanged from 1.1.

---

## 1. Goal

A single-page internal tool for a ~50-person company's IT admin to answer three questions fast:

1. What hardware do we own?
2. Who has it right now?
3. What's sitting in the store room, ready to hand out?

This is a **prototype for a demo**, not a production system. Correct behaviour, a real persistence layer, and a clean, dense UI matter. Scale, auth, and deployment do not.

## 2. Non-goals — do not build these

Explicitly out of scope. Adding any of these fails the spec:

- No login, users, roles, or permissions
- No ORM, query builder, or migration tool — hand-written SQL only
- No Server Actions — Route Handlers only, so every endpoint is testable with `curl` (see 3.4)
- No data-fetching library (no React Query, SWR, Axios) — `fetch` is enough
- No routing — one page at `/`, tab state in React state. Do not add `/assets` or `/people` routes
- No `any`, no `@ts-ignore`, no `!` non-null assertions
- No tests, no CI, no Docker
- No dark mode, no theme switcher
- No CSV import/export, no printing, no PDF
- No charts or graphs
- No date pickers beyond `<input type="date">`
- No animation beyond CSS hover/focus transitions

If something is not described in this document, do not build it.

## 3. Tech constraints

The project already exists: Next.js 16.2.12, React 19.2.4, TypeScript 5, Tailwind v4 via `@tailwindcss/postcss`, ESLint 9. Do not re-scaffold it and do not change the existing `package.json` scripts.

### 3.1 Dependencies to add

Exactly two. Nothing else may appear in `package.json` when you are done.

```
npm i better-sqlite3
npm i -D @types/better-sqlite3
```

### 3.2 File layout

```
app/
  api/
    health/route.ts
    state/route.ts
    assets/route.ts
    assets/[id]/assign/route.ts
    assets/[id]/return/route.ts
    assets/[id]/status/route.ts
  globals.css
  layout.tsx
  page.tsx            # "use client" — owns all state
components/
  Sidebar.tsx
  Dashboard.tsx
  AssetsTable.tsx
  AddAssetDrawer.tsx
  AssignDialog.tsx
  People.tsx
  StatusPill.tsx
lib/
  types.ts            # every shared type
  db.ts               # connection, schema, seed, row mappers
  api.ts              # every fetch call, typed
  format.ts           # currency, dates, display labels
data/
  assetdesk.db        # gitignored
```

Add `data/*.db*` to `.gitignore`.

### 3.3 Three Next.js details that will cost you time if missed

**1. `better-sqlite3` is a native module.** It must not be bundled. In `next.config.ts`:

```ts
const nextConfig: NextConfig = {
  serverExternalPackages: ["better-sqlite3"],
};
```

**2. Dev hot-reload re-evaluates modules and leaks database handles.** `lib/db.ts` must hold the connection on `globalThis`:

```ts
const g = globalThis as unknown as { __assetdesk?: Database.Database };
export const db = g.__assetdesk ?? init();
if (process.env.NODE_ENV !== "production") g.__assetdesk = db;
```

**3. Route params are a Promise in Next 16.** Every `[id]` handler:

```ts
export async function POST(
  req: Request,
  { params }: { params: Promise<{ id: string }> }
) {
  const { id } = await params;
}
```

Also put `export const dynamic = "force-dynamic";` on `GET /api/state`, or Next will cache it and the table will stop updating after mutations.

### 3.4 Architecture

| Decision | Choice |
|---|---|
| Data access | `lib/db.ts` only. No component and no client file ever imports it |
| Runtime | Node (the default for Route Handlers). Never Edge — `better-sqlite3` cannot run there |
| SQLite driver | `better-sqlite3`, synchronous. No `async`/`await` in the data layer |
| DB file | `data/assetdesk.db`, created and seeded on first request if absent |
| Client state | `app/page.tsx` is `"use client"` and holds one `useState<AppState>`, passed down as props |
| Fetching | `lib/api.ts` wraps every call. Components never call `fetch` directly |

**Why Route Handlers and not Server Actions.** Server Actions would be the idiomatic Next 16 choice and would delete a layer. Route Handlers are specified here because an HTTP endpoint can be exercised with `curl` before any component exists, which is what makes the build order in section 8 work. Note this trade-off out loud rather than silently changing it.

**Client data flow.** `page.tsx` calls `GET /api/state` on mount. Every mutation awaits its POST, then re-calls `GET /api/state`. No optimistic updates, no cache invalidation, no partial state patching. Slower than necessary and correct every time, which is the right trade for a prototype.

Icons, where needed, are inline SVG or a single Unicode character. Keep it boring.

## 4. Data model

### 4.1 Types

`lib/types.ts` is the single source of truth for the client and the API. Use these names verbatim.

```ts
export type Category =
  | "Laptop" | "Monitor" | "Headset" | "Dock" | "Phone" | "Keyboard" | "Other";
export type Status    = "in_stock" | "assigned" | "repair" | "retired";
export type Condition = "new" | "good" | "fair" | "poor";

export interface Employee {
  id: string;          // uuid
  name: string;
  email: string;
  department: string;
  title: string;
}

export interface Asset {
  id: string;          // uuid
  tag: string;         // unique, format "AST-1001"
  category: Category;
  make: string;        // "Apple"
  model: string;       // "MacBook Pro 14 M3"
  serial: string;
  status: Status;
  condition: Condition;
  purchaseDate: string;        // "2024-03-11", ISO date, no time
  cost: number;                // 2400
  location: string;            // "HQ / Store Room"
  notes: string;               // may be ""
  assignedTo: string | null;   // Employee.id
  assignedDate: string | null; // ISO date
}

export interface AppState {
  employees: Employee[];
  assets: Asset[];
}

export interface NewAssetInput {
  tag: string;
  category: Category;
  make: string;
  model: string;
  serial: string;
  condition: Condition;
  purchaseDate: string;
  cost: number;
  location: string;
  notes: string;
  quantity: number;    // 1–20
}
```

The union types are load-bearing. A typo in a status string is a compile error, not a runtime surprise — and the same rule is repeated as a `CHECK` in 4.3 so that data arriving from outside TypeScript is rejected too.

### 4.2 Naming

The API speaks camelCase. The database stores snake_case (`purchase_date`, `assigned_to`). Convert in exactly two functions in `lib/db.ts` — `rowToAsset` and `rowToEmployee` — and nowhere else. No client code ever sees a snake_case key.

### 4.3 Invariants

The database enforces what it can. The route handler enforces the rest. The client assumes neither.

| # | Invariant | Enforced by |
|---|---|---|
| 1 | `tag` is unique across all assets | `UNIQUE` constraint |
| 2 | If `status = 'assigned'`, both `assigned_to` and `assigned_date` are non-null | table `CHECK` |
| 3 | If `status <> 'assigned'`, both `assigned_to` and `assigned_date` are null | table `CHECK` |
| 4 | `category`, `status`, `condition` only hold listed values | column `CHECK` + TS union |
| 5 | `assigned_to` references a real employee | `FOREIGN KEY` (pragma on) |
| 6 | Only an asset with `status = 'in_stock'` can be assigned | route handler, 409 |
| 7 | Retiring an assigned asset clears the assignment in the same transaction | route handler |
| 8 | Employees are never deleted — assign and return only | no endpoint exists |

Invariants 2 and 3 are the ones to show students: a rule written in prose became a `CHECK`, and a bug that was possible became impossible.

### 4.4 Schema

`lib/db.ts` runs this on init. `IF NOT EXISTS` throughout, so a restart is safe.

```sql
PRAGMA foreign_keys = ON;
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

`journal_mode = WAL` matters here: you will query this file with the `sqlite3` CLI while the dev server holds it open.

### 4.5 API

Six routes. All JSON. All mutations wrapped in `db.transaction()`.

| Method | Path | Body | Returns |
|---|---|---|---|
| GET | `/api/health` | — | `{ ok: true }` |
| GET | `/api/state` | — | `AppState` — everything, unsorted |
| POST | `/api/assets` | `NewAssetInput` | `{ created: Asset[] }`, 201 |
| POST | `/api/assets/[id]/assign` | `{ employeeId, assignedDate }` | `Asset` |
| POST | `/api/assets/[id]/return` | — | `Asset` |
| POST | `/api/assets/[id]/status` | `{ status }` — `repair`, `in_stock`, `retired` | `Asset` |

**Errors.** Every failure returns a matching status and `{ error: "<sentence the UI shows verbatim>" }`. The client displays `error` as-is and never writes its own copy for a server failure.

| Case | Status | `error` |
|---|---|---|
| Missing or invalid field | 400 | `Cost must be a number of 0 or more.` |
| Duplicate tag | 409 | `Tag AST-1004 is already in use.` |
| Assigning a non-in-stock asset | 409 | `AST-1004 is in repair and cannot be assigned.` |
| Unknown asset or employee id | 404 | `That asset no longer exists. Refresh to see current data.` |

Catch the SQLite `UNIQUE constraint failed` throw and translate it into the 409. Do not pre-check with a `SELECT` — let the constraint do its job. That is the whole point of having one.

Validate request bodies with a hand-written type guard per route. No validation library.

### 4.6 Display labels

Never show raw enum values in the UI. `lib/format.ts` owns these maps.

| Value | Label |
|---|---|
| `in_stock` | In stock |
| `assigned` | Assigned |
| `repair` | In repair |
| `retired` | Retired |
| `new` / `good` / `fair` / `poor` | New / Good / Fair / Poor |

Currency is a single exported constant `const CURRENCY = "USD"` formatted with `Intl.NumberFormat`. One line to change.

## 5. Screens

One page. A fixed left sidebar plus a content area. Three tabs: **Dashboard**, **Assets**, **People**. Tab state lives in `page.tsx`.

---

### 5.1 Dashboard

Read-only overview. All figures are computed on the client from `AppState` — no extra endpoint.

**Top row — four stat tiles**, each a large number and a label:

- Total assets (excludes `retired`)
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

1. Search input — matches `tag`, `make`, `model`, or `serial`, case-insensitive, substring
2. Category select — "All categories" plus each category
3. Status select — "All statuses" plus each status label
4. Spacer
5. **Add asset** button (primary), opens the Add asset drawer

**Table columns**

| Column | Content |
|---|---|
| Tag | Monospace, e.g. `AST-1001` |
| Category | Text |
| Device | `make` + `model` on one line, `serial` in small muted text beneath |
| Status | Status pill |
| Assigned to | Employee name, or `—` |
| Since | `assignedDate`, or `—` |
| Location | Text |
| Actions | Buttons, see below |

**Row actions** — show only what is legal for that row's status:

| Status | Buttons shown |
|---|---|
| `in_stock` | Assign · Mark repair · Retire |
| `assigned` | Return · Retire |
| `repair` | Mark in stock · Retire |
| `retired` | *(none)* |

**Acceptance criteria**

- [ ] Search and both filters combine with AND
- [ ] A result count reads "Showing 7 of 12 assets" above the table and updates live
- [ ] Empty result shows: heading "No assets match these filters", plus a **Clear filters** button that resets search and both selects
- [ ] Table is sorted by `tag` ascending by default
- [ ] Clicking a column header for Tag, Category, or Status toggles ascending/descending sort on that column
- [ ] Retired rows render at 60% opacity
- [ ] Every row action disables itself while its request is in flight

---

### 5.3 Add asset drawer

Slides in from the right, 420px wide, translucent backdrop. Escape and the backdrop both close it.

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

**Quantity** is the stock-entry shortcut: posting quantity `n` creates `n` assets with identical details and sequential tags from the entered tag. Serial gets a `-1`, `-2` … suffix when `n > 1`. The route handler expands this inside one transaction — either all `n` rows are written or none are. The client sends one request.

New assets are always created with `status: 'in_stock'`, `assigned_to: null`, `assigned_date: null`. The client does not send these; the handler sets them.

The pre-filled tag is `AST-` plus one above the highest numeric suffix currently in state. It is a convenience, not a reservation — the unique constraint is what guarantees correctness.

**Acceptance criteria**

- [ ] Submitting with a required field empty shows a red message under that field and sends no request
- [ ] A duplicate tag shows the server's `error` sentence under the Tag field and leaves the drawer open with the form intact
- [ ] On success the drawer closes and new rows appear after the state refetch, with no page reload
- [ ] Reopening the drawer shows a fresh empty form with a newly pre-filled tag
- [ ] Submitting quantity 3 with a tag that collides on the second row creates zero assets, not one

---

### 5.4 Assign dialog

Opened by the **Assign** row action. A small centred modal.

- Shows the asset tag and device name as static text
- One select: employee, listing `name — department`, with a "Select an employee" placeholder
- One `<input type="date">` for assignment date, defaulting to today
- Buttons: **Cancel** and **Assign asset**

On confirm the handler sets `assigned_to`, `assigned_date`, and `status = 'assigned'` in one transaction.

**Return** takes no dialog. It clears the assignment and sets `status = 'in_stock'` immediately.

**Acceptance criteria**

- [ ] **Assign asset** is disabled until an employee is selected
- [ ] After assigning, the row updates and the Dashboard counts change
- [ ] Returning an asset makes it immediately assignable again

---

### 5.5 People

A table of employees. Columns: Name · Title · Department · Email · Assets held (count).

Clicking a row expands an inline panel beneath it listing that person's assigned assets as `tag — make model`, each with a **Return** button.

**Acceptance criteria**

- [ ] Assets-held count matches the Assigned-to column on the Assets tab
- [ ] A person with zero assets shows a `0` count and expands to "No assets assigned"
- [ ] Returning from this panel updates the count without collapsing the panel

## 6. Visual design

Utilitarian ops console. Dense, quiet, high information per pixel. Nothing decorative.

**Colour tokens.** Tailwind v4 takes theme values from CSS, not a config file. In `app/globals.css`:

```css
@import "tailwindcss";

@theme {
  --color-canvas:  #ECEEF0;   /* app background */
  --color-surface: #FFFFFF;   /* cards, table, drawer */
  --color-ink:     #16191D;   /* primary text */
  --color-muted:   #6E747D;   /* secondary text, serials, labels */
  --color-line:    #D8DCE0;   /* borders, dividers */
  --color-accent:  #0F5C58;   /* primary buttons, active tab, focus ring */
  --color-warn:    #B45309;
  --color-danger:  #A82C22;
  --color-info:    #1F4E9C;
}
```

That generates `bg-canvas`, `text-ink`, `border-line`, `ring-accent` and so on. Use those utilities. Do not write arbitrary hex values in `className`.

Status pills: In stock → `accent`, Assigned → `info`, In repair → `warn`, Retired → `muted`. Tinted background at ~12% opacity with the solid colour as text. No outlines.

**Type**

- UI: system stack. No web fonts — they cost build time. Do not add `next/font`.
- Data: `ui-monospace, monospace` for asset tags, serials, and every number in a table cell or stat tile. This is the signature of the interface: identifiers read as identifiers.
- Scale: stat tile numbers 32px/600 · section headings 15px/600 · body and table 14px/400 · labels and serials 12px/500 uppercase with 0.04em tracking for column headers only.

**Layout**

- Sidebar 220px fixed, `surface`, right border `line`. Wordmark "AssetDesk" at top, three tab buttons beneath. Active tab: 3px `accent` left bar and `accent` text.
- Content area max-width 1200px, 32px padding.
- Table rows 44px tall, 1px `line` bottom border, `surface` background, hover row background `canvas`.
- Border radius 6px everywhere. Shadows only on the drawer and modal.

**Quality floor** — not optional, but do not spend time announcing it:

- Every interactive element has a visible `:focus-visible` ring in `accent`
- Buttons and inputs have a hover state
- Layout does not break below 1024px (the table may scroll horizontally)

**Copy rules**

- Sentence case for all buttons and headings. "Add asset", not "Add Asset".
- A button's verb matches its result. "Assign asset" → the row shows "Assigned".
- Empty states are an instruction, not an apology: "No assets match these filters."
- Errors state what happened and what to do: "Tag AST-1004 is already in use."

## 7. Seed data

`lib/db.ts` seeds on init if `SELECT COUNT(*) FROM assets` returns 0. Insert exactly this, in one transaction, employees first. Generate uuids with `crypto.randomUUID()`; the `emp-N` references map to the employee at that index.

To reset during the demo, stop the dev server, delete `data/assetdesk.db*`, and restart. Say that out loud — students should see the data lives in a file now.

**Employees**

| # | name | email | department | title |
|---|---|---|---|---|
| 1 | Aisha Rahman | aisha.rahman@northwind.co | Engineering | Backend Engineer |
| 2 | Daniel Okafor | daniel.okafor@northwind.co | Design | Product Designer |
| 3 | Mei Tanaka | mei.tanaka@northwind.co | Sales | Account Executive |
| 4 | Lucas Moreau | lucas.moreau@northwind.co | Engineering | QA Engineer |
| 5 | Priya Nair | priya.nair@northwind.co | People Ops | HR Generalist |

**Assets**

| tag | category | make | model | serial | status | assignedTo | assignedDate | condition | purchaseDate | cost | location |
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

Notes are `""` for all seeded assets. This seed deliberately produces one Out badge (Dock), several Low badges, and one retired row, so every visual state is visible on first load.

## 8. Build order

Work in this order. Verify each milestone before moving on.

| # | Milestone | Time | Done when |
|---|---|---|---|
| M0 | `lib/types.ts`, `lib/db.ts` (schema, seed, singleton, mappers), `next.config.ts`, `/api/health`, `/api/state` | 12 min | `curl localhost:3000/api/state` returns 5 employees and 12 assets |
| M1 | The four mutation routes, with transactions and the full error table | 10 min | Create, assign, return, and status all work from `curl` — before any UI exists |
| M2 | `globals.css` `@theme`, `layout.tsx`, sidebar shell, `page.tsx` client component, `lib/api.ts`, load state on mount | 10 min | Three tabs switch; seeded data is in React state |
| M3 | Assets table with all columns and status pills | 12 min | All 12 rows render correctly |
| M4 | Search, both filters, result count, empty state | 8 min | Filters combine correctly and clear |
| M5 | Add asset drawer with validation and quantity | 10 min | Adding 3 headsets creates AST-1013/14/15 in the database |
| M6 | Assign dialog, return, repair, retire | 8 min | An asset can go in stock → assigned → in stock |
| M7 | Dashboard tiles and stock table, People tab | 12 min | Counts match the Assets tab exactly |

M0 and M1 come first on purpose: the API is exercisable from the command line before a single component exists, so a later bug is either server-side or client-side and never ambiguously both.

If a milestone overruns, cut scope inside that milestone rather than skipping the next one. Column sorting (M4) and the People expand panel (M7) are the first two things to cut.

## 9. Definition of done

- [ ] `npm run dev` starts with no errors and no warnings in the terminal or the browser console
- [ ] `npx tsc --noEmit` passes clean
- [ ] `npm run lint` passes clean
- [ ] `npm run build` succeeds
- [ ] Deleting `data/assetdesk.db*` and restarting reproduces the exact seed state
- [ ] Every acceptance-criteria checkbox in section 5 passes when clicked through by hand
- [ ] Refreshing the browser preserves all changes, because they are in the database
- [ ] Every invariant in section 4.3 holds — including when the request comes from `curl` and bypasses the UI entirely
- [ ] Every server error surfaces in the UI as the server's own `error` sentence
- [ ] No `any`, no `@ts-ignore`, no `!` assertions in the codebase
- [ ] `package.json` gained exactly `better-sqlite3` and `@types/better-sqlite3`
- [ ] Nothing from section 2 has been built

## 10. Working agreement for the implementing agent

1. Read this whole document before writing code.
2. The project already exists. Do not re-scaffold, do not change existing scripts, do not upgrade dependencies.
3. Build in the milestone order in section 8. Do not jump ahead to the UI.
4. Use the exact type names, field names, enum values, route paths, and colour tokens given here.
5. If something is genuinely ambiguous, pick the simplest reading, state the assumption in one line, and keep going. Do not stop to ask.
6. If you want to add something not in this spec, don't. Note it under a "Possible next steps" heading at the end instead.
7. Report progress as a one-line note per completed milestone. No essays.