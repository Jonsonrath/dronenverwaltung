# Proposal

## Why

The first change delivered all required views (drone list, drone form, student groups, loan form, active loans with return, history), but the team finds the look too plain: light grey pages, plain tables and forms below long lists. The app is used at the drone cabinet, often on a phone, and should feel like the tool it is – closer to a drone control app than to a form page. Changing only the presentation now, before the change requests of the semester arrive, gives every later page a better base.

## What Changes

A frontend-only redesign as a dark, grown-up UI (revised after review: graphite instead of navy, almost no color, status as text with a bar). Same pages, same routes, same data, same REST calls.

*Second revision (task 5.1), after the team used the merged UI:* clean, but too plain to be recognisable. The app gets an identity from its subject, the drone cabinet – see the last bullet group and design §7.

- **Dark theme** in near-neutral graphite with high-contrast text; color only for status (Available, Loaned) and errors, readable on dark backgrounds (WCAG AA contrast).
- **Navigation:** a side navigation on desktop; the bottom tab bar stays on phones.
- **Drones page:** drones as **rows with a status bar** – name, description, status – instead of a table; the "Add a drone" form becomes a **side panel next to the list** on desktop (below on phones).
- **Student groups page:** list and "Add a student group" form side by side on desktop.
- **New loan page:**
  - drones as **selectable rows** grouped into *Available* and *Loaned right now* instead of a dropdown (loaned drones stay selectable – the backend decides, as before)
  - student group as clickable chips
  - **+/- steppers** for batteries and propellers
- **Active loans and history:** restyled for the dark theme; the existing phone layout (one block per loan) is kept.
- **Second design pass (task 5.1):**
  - one accent color (hi-vis yellow) for actions and the current page; green / blue only for Available / Loaned; a signage typeface (Barlow)
  - navigation with icons in all three layouts
  - drones page as the cabinet: "n of m in the cabinet" and one bay per drone with a drone glyph (filled = in, dashed outline = out)
  - shortcut from a bay: "Loan this drone" opens the loan form with that drone chosen, "Return this drone" opens the active loans
  - loan form in three numbered steps with a loan slip that reads the choice back before the button
  - student groups as name plates, loans as one plate per loan
- Unchanged on purpose: every request, every validation and every error message comes from the backend as before; the frontend still does not implement domain rules.

### Out of Scope

- Any backend or API change, any new data (e.g. showing which group has a loaned drone on the drone row would need a new API field – a separate change)
- New features: search, filters, sorting, statistics, a dashboard, editing or deleting drones and groups
- A light/dark switch – the app is dark only
- Login, roles and everything else that was already out of scope in the first change

## Capabilities

### New Capabilities
<!-- none: presentation only -->

### Modified Capabilities
<!-- none: no requirement changes. The change declares skip_specs: true. -->

## Impact

- **Frontend only** (`frontend/drone-inventory-ui`): `styles.css` (design tokens, dark theme), app shell (`app.html`, `app.css`), templates and styles of the five pages, the loan form's drone and group selection and the accessory fields.
- **Tests:** existing Vitest tests that look for table rows or `<select>` elements are updated to the new markup; their assertions (what is shown, which request is sent, which backend message appears) stay the same. New tests for the steppers and the tile selection.
- **No change** to the backend, the REST API, the database or the OpenSpec main specs.
- **Dependencies:** none expected (plain CSS, no UI library).
- **Second design pass:** new decorative component `shared/drone-glyph`, `public/favicon.svg`; the loan form reads the optional query parameter `drone` (a shortcut only – an unknown id is ignored, the backend still decides about every loan).
