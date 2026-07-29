# AssetDesk — IT Asset Management Prototype

**Version** 1.0 · **Status** Ready to build · **Target build time** 60 minutes

---

## 1. Goal

A single-page internal tool for a ~50-person company's IT admin to answer three questions fast:

1. What hardware do we own?
2. Who has it right now?
3. What's sitting in the store room, ready to hand out?

This is a **prototype for a demo**, not a production system. Correct behaviour and a clean, dense UI matter. Scale, auth, and persistence beyond the browser do not.

## 2. Non-goals — do not build these

Explicitly out of scope. Adding any of these fails the spec:

- No login, users, roles, or permissions
- No backend, database, or API calls
- No routing library — tab state in React state is enough
- No tests, no CI, no Docker
- No dark mode, no theme switcher
- No CSV import/export, no printing, no PDF
- No charts or graphs
- No date pickers beyond `<input type="date">`
- No animation beyond CSS hover/focus transitions

If something is not described in this document, do not build it.

## 3. Tech constraints

| Decision | Choice |
|---|---|
| Build tool | Vite (`npm create vite@latest assetdesk -- --template react`) |
| Framework | React 18+, function components, hooks only |
| Styling | Tailwind CSS v4 via `@tailwindcss/vite` |
| State | One `useState` tree in `App.jsx`, passed down as props |
| Persistence | `localStorage` under key `assetdesk.v1`, seeded on first load |
| IDs | `crypto.randomUUID()` |
| Dependencies | React and Tailwind only. No UI kit, no date library, no state library, no icon package |

Icons, where needed, are inline SVG or a single Unicode character. Keep it boring.

## 4. Data model

Two entities. Field names are exact — use them verbatim.

### Employee

```js
{
  id: string,           // uuid
  name: string,
  email: string,
  department: string,   // free text
  title: string
}
```

### Asset

```js
{
  id: string,           // uuid
  tag: string,          // unique, format "AST-1001"
  category: string,     // "Laptop" | "Monitor" | "Headset" | "Dock" | "Phone" | "Keyboard" | "Other"
  make: string,         // "Apple"
  model: string,        // "MacBook Pro 14 M3"
  serial: string,
  status: string,       // "in_stock" | "assigned" | "repair" | "retired"
  condition: string,    // "new" | "good" | "fair" | "poor"
  purchaseDate: string, // "2024-03-11" (ISO date, no time)
  cost: number,         // 2400
  location: string,     // "HQ / Store Room"
  notes: string,        // may be ""
  assignedTo: string | null,   // Employee.id
  assignedDate: string | null  // ISO date
}
```

### Invariants — enforce these in code

1. `tag` is unique across all assets. Reject a duplicate on create with an inline field error.
2. If `status === "assigned"` then `assignedTo` and `assignedDate` are both non-null.
3. If `status !== "assigned"` then `assignedTo` and `assignedDate` are both `null`.
4. Only an asset with `status === "in_stock"` can be assigned.
5. Retiring an assigned asset returns it first, then sets `status = "retired"`.
6. Deleting an employee is not supported. Assign/return only.

### Display labels

Never show raw enum values in the UI.

| Value | Label |
|---|---|
| `in_stock` | In stock |
| `assigned` | Assigned |
| `repair` | In repair |
| `retired` | Retired |
| `new` / `good` / `fair` / `poor` | New / Good / Fair / Poor |

Currency is a single module-level constant `const CURRENCY = "USD"` formatted with `Intl.NumberFormat`. One line to change.

## 5. Screens

The app is a fixed left sidebar plus a content area. Three tabs: **Dashboard**, **Assets**, **People**. Tab state lives in `App.jsx`.

---

### 5.1 Dashboard

Read-only overview.

**Top row — four stat tiles**, each showing a large number and a label:

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

---

### 5.3 Add asset drawer

Slides in from the right, 420px wide, with a translucent backdrop. Escape and the backdrop both close it.

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

**Quantity** is the stock-entry shortcut: submitting with quantity `n` creates `n` assets with identical details and sequential tags starting from the entered tag. Serial gets a `-1`, `-2` … suffix when `n > 1`.

New assets are always created with `status: "in_stock"`, `assignedTo: null`, `assignedDate: null`.

**Acceptance criteria**

- [ ] Submitting with a required field empty shows a red message under that field and does not close the drawer
- [ ] A duplicate tag shows "Tag AST-1004 is already in use" under the Tag field
- [ ] On success the drawer closes and the new rows appear in the table with no page reload
- [ ] Reopening the drawer shows a fresh empty form with a newly pre-filled tag

---

### 5.4 Assign dialog

Opened by the **Assign** row action. A small centred modal.

- Shows the asset tag and device name as static text
- One select: employee, listing `name — department`, with a "Select an employee" placeholder
- One `<input type="date">` for assignment date, defaulting to today
- Buttons: **Cancel** and **Assign asset**

On confirm: set `assignedTo`, `assignedDate`, and `status = "assigned"`.

**Return** takes no dialog. It clears `assignedTo` and `assignedDate` and sets `status = "in_stock"` immediately.

**Acceptance criteria**

- [ ] **Assign asset** is disabled until an employee is selected
- [ ] After assigning, the row updates in place and the Dashboard counts change
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

**Colour tokens** — define as CSS custom properties in `index.css`:

```
--canvas:  #ECEEF0   /* app background */
--surface: #FFFFFF   /* cards, table, drawer */
--ink:     #16191D   /* primary text */
--muted:   #6E747D   /* secondary text, serials, labels */
--line:    #D8DCE0   /* borders, dividers */
--accent:  #0F5C58   /* primary buttons, active tab, focus ring */
--ok:      #0F5C58
--warn:    #B45309
--danger:  #A82C22
--info:    #1F4E9C
```

Status pills: In stock → `--ok`, Assigned → `--info`, In repair → `--warn`, Retired → `--muted`. All pills are a tinted background at ~12% opacity with the solid colour as text. No outlines.

**Type**

- UI: system stack (`ui-sans-serif, system-ui, sans-serif`). No web fonts — they cost build time.
- Data: `ui-monospace, monospace` for asset tags, serials, and every number in a table cell or stat tile. This is the signature of the interface: identifiers read as identifiers.
- Scale: stat tile numbers 32px/600 · section headings 15px/600 · body and table 14px/400 · labels and serials 12px/500 uppercase with 0.04em tracking for column headers only.

**Layout**

- Sidebar 220px fixed, `--surface`, right border `--line`. Wordmark "AssetDesk" at top, three tab buttons beneath. Active tab: `--accent` left bar, 3px, and `--accent` text.
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

On first load, if `localStorage.assetdesk.v1` is absent, seed exactly this. Generate uuids at seed time; the `emp-N` references below map to the employee at that index.

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

Notes may be `""` for all seeded assets. This seed deliberately produces one Out badge (Dock), several Low badges, and one retired row, so every visual state is visible on first load.

## 8. Build order

Work in this order. Run the app and look at it after each milestone before moving on.

| # | Milestone | Time | Done when |
|---|---|---|---|
| M1 | Scaffold, Tailwind, tokens, sidebar shell, seed + `localStorage` hook | 10 min | Three empty tabs switch; seed data is in state and survives a refresh |
| M2 | Assets table with all columns and status pills | 15 min | All 12 seeded rows render correctly |
| M3 | Search, both filters, result count, sorting, empty state | 10 min | Filters combine correctly and clear |
| M4 | Add asset drawer with validation and quantity | 10 min | Adding 3 headsets creates AST-1013/14/15 |
| M5 | Assign dialog, return, repair, retire | 10 min | An asset can go in stock → assigned → in stock |
| M6 | Dashboard tiles and stock table, People tab | 15 min | Counts match the Assets tab exactly |

If a milestone overruns, cut scope inside that milestone rather than skipping the next one. Sorting (M3) and the People expand panel (M6) are the first two things to cut.

## 9. Definition of done

- [ ] `npm run dev` starts with no console errors or warnings
- [ ] Every acceptance-criteria checkbox in section 5 passes when clicked through by hand
- [ ] Refreshing the browser preserves all changes
- [ ] Data invariants in section 4 hold after any sequence of actions
- [ ] No dependency outside React and Tailwind appears in `package.json`
- [ ] Nothing from section 2 has been built

## 10. Working agreement for the implementing agent

1. Read this whole document before writing code.
2. Build in the milestone order in section 8. Do not jump ahead.
3. Use the exact field names, enum values, and colour tokens given here.
4. If something is genuinely ambiguous, pick the simplest reading, state the assumption in one line, and keep going. Do not stop to ask.
5. If you want to add something not in this spec, don't. Note it under a "Possible next steps" heading at the end instead.
6. Report progress as a one-line note per completed milestone. No essays.
