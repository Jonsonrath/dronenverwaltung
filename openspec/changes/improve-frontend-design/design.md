# Design

## Context

See proposal.md – Why. Current state of `frontend/drone-inventory-ui` (from the first change):

- Angular 22, standalone components, Signal Forms, `rxResource` + `HttpClient` services in `core/`, pages in `features/`.
- One global `styles.css` with tokens on `:root` (light theme: navy text on cool grey), page CSS per component, `loan-table.css` shared by active loans and history.
- Top navigation on desktop, bottom tab bar on phones (`app.html`, `app.css`).
- 48 Vitest tests; several query table rows (`tbody tr`) or the `<select>` elements of the loan form.

Constraints: no backend/API change; the frontend shows backend states and messages only (AGENTS.md); usable at phone width; one consistent visual direction.

## Goals / Non-Goals

**Goals:**
- A dark, distinctive "cockpit" look that is still calm and readable at the cabinet, also in daylight on a phone.
- Faster handling on the loan form: tap a drone, tap a group, tap +/- – no dropdowns, no keyboard needed for typical quantities.
- Keep every test assertion about behavior (requests, shown values, backend messages) – only selectors change.

**Non-Goals:**
- No UI component library, no icon font, no animation library.
- No new data on the pages (see proposal – Out of Scope).
- No theme switch.

## Decisions

### 1. Visual direction: dark, grown-up, a pro tool
*Revised after the first theme was reviewed:* the team found navy + teal + glowing dots "too AI" and chose **"dark, but grown-up"** – near-neutral graphite, very little color, status as text with a small bar, large calm typography (the direction of pro tools like Linear or Raycast, but its own).
- **Palette** (tokens on `:root`):

  | Token | Value | Use |
  |---|---|---|
  | `--bg` | `#161718` graphite | page background (not `#111`, slightly warm) |
  | `--surface` | `#1D1F21` | list rows, forms, side navigation |
  | `--raised` | `#25282B` | hover, selected item, inputs |
  | `--line` | `#303438` | dividers |
  | `--text` / `--muted` | `#EDEEEF` / `#8E949A` | text |
  | `--available` | `#7FBF8E` sage | Available |
  | `--loaned` | `#D9A441` ochre | Loaned |
  | `--error` | `#E5786D` | backend error messages |

  Color only means status or error. The primary action is a light grey button (`--text` on `--bg`), not an accent color. Every text and status color ≥ 4.5:1 on `--bg`, `--surface` and `--raised` (checked in task 1.1).
- **Status:** the word (Available / Loaned) in its color plus a **narrow status bar on the left edge of the row** – no glow, color is never the only signal.
- **The one memorable element:** that status bar down the left of every drone row – the whole cabinet readable at a glance. Everything else stays quiet: no gradients, no shadows, no decoration, no viewfinder corners (dropped from the first draft).
- **Type:** IBM Plex Sans (400 / 500 / 600) – technical, engineering, fits hardware and a school; page titles large in weight 500; tabular figures for quantities and dates; sentence case, no all-caps labels, no monospace labels.

*Alternatives:* navy + teal + glow (first draft) – rejected by the team as generic; near-black with a neon accent – the most common "dark tech" default.

### 2. Layout
- **Desktop (≥ 64rem):** fixed side navigation (≈ 15rem) with brand on top and the five links; content to the right, max 72rem.
- **Tablet (40–64rem):** side navigation collapses to the top bar from the first design.
- **Phone (< 40rem):** bottom tab bar as today.
- **List + form pages** (drones, student groups): two columns on desktop – content left, form as a **sticky side panel** (≈ 22rem) right; stacked on smaller screens, form below the list.

### 3. Drones as rows
A list (`<ul>`/`<li>`), not a table – each drone is one item: status bar on the left, name large, description muted below, status word on the right. Rows are separated by thin lines, not boxed. Empty, loading and error states keep their texts.

### 4. Loan form: tiles, chips, steppers
- **Drone choice:** native **radio inputs** styled as selectable rows/tiles, grouped in two `fieldset`s (*Available*, *Loaned right now*). Native radios give keyboard navigation and screen-reader semantics for free. Loaned drones stay selectable; the backend answers 409 as before.
- **Student group choice:** radio inputs styled as chips.
- **Batteries / propellers:** a small reusable **`QuantityStepper`** control in `shared/` (−, number field, +), usable with `[formField]` as a Signal Forms custom control (`FormValueControl<number>`). "−" is disabled at 0 as a convenience; typing a negative number is still possible and the backend's 422 message is shown – the rule stays in the backend.
- Signal Forms model keeps string ids for drone and group (as now); only the bound controls change.

*Alternative:* keep `<select>` and only restyle it. Rejected: at the cabinet the team wants to tap, and native selects cannot be styled consistently on phones.

### 5. Tables for loans
Active loans and history stay tables on desktop (they are tabular data with numbers) and keep the one-block-per-loan layout on phones; only colors, spacing and the status/return button styling change.

### 6. Tests
Selectors are updated where markup changes (`tbody tr` → drone list items, `<select>` → radio inputs). The asserted behavior stays identical: same requests and bodies, same shown values, same backend messages. New tests: `QuantityStepper` (+, −, disabled at 0, typing, works with `[formField]`) and tile/chip selection sending the right ids.

## Risks / Trade-offs

- [Dark UI hard to read in sunlight on a phone] → AA contrast checked in task 1.1; large tap targets (≥ 44 px); status always as word + bar + color.
- [Google Fonts: IBM Plex Sans unavailable offline] → system font fallback.
- [Signal Forms radio binding with values from data] → if `[formField]` on radios does not accept data-driven values, the tile group becomes a small custom control (`FormValueControl<string>`); decided during task 3.2, no effect on scope.
- [Many selector changes in tests] → each page task updates its own spec; behavior assertions are not removed, only re-targeted.
- [Google Fonts unavailable offline] → system font fallback stays as today.

## Migration Plan

Frontend only, no data migration. Each task is its own issue and pull request; the app stays usable after every merge (pages not yet restyled still work with the new dark tokens).

## Open Questions

- None that change the tasks.
