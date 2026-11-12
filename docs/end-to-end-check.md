# End-to-end check – the assignment's example

The example from the assignment, run completely through the user interface against the real backend and database.

- **Run 2 – 12.11.2026:** task 4.1 (#59) of the OpenSpec change `improve-frontend-design`, through the redesigned German UI, including rename and delete of student groups (`manage-student-groups`).
- **Run 3 – 12.11.2026:** the feature backlog (REQ-013 … REQ-024), see the section at the end.
- **Run 1 – 05.11.2026:** task 8.1 (#24) of the OpenSpec change `initial-drone-lending-system`, through the first English UI. Result: all 10 steps passed (see the git history of this file).

## Setup

1. Backend stopped. The run uses its own empty database file, so the normal `backend/DroneInventory.Api/droneinventory.db` is not touched:
   `ConnectionStrings__DroneInventory="Data Source=<temp>/e2e.db"` (or delete `droneinventory.db*` for the same effect).
2. `cd backend/DroneInventory.Api && dotnet run --urls http://localhost:5087`
3. `cd frontend/drone-inventory-ui && npx ng serve` (http://localhost:4200, `/api` proxied to the backend)
4. Browser: Chrome, 1280 × 800; at the end every page again at 400 px (phone).

## Steps and results – run 2, 12.11.2026

| # | Step in the UI | Expected (spec) | Result |
|---|---|---|---|
| 0 | *Drohnen* with the empty database | "Noch keine Drohnen. Füge die erste hinzu." | ✅ |
| 1 | *Drohnen* → **+ Drohne hinzufügen**: "TELLO-07", description "SSID: TELLO-9FA21B, rechter Propellerschutz beschädigt" | "TELLO-07 hinzugefügt. Sie ist jetzt verfügbar.", status **Verfügbar** (REQ-001) | ✅ |
| 2 | **+ Drohne hinzufügen** with the name "   " | backend message in the sheet: "Der Name einer Drohne darf nicht leer sein." (REQ-001, 422) | ✅ |
| 3 | *Schülergruppen* → **+ Gruppe hinzufügen**: "Group 3", then "Group 1" | both groups listed (REQ-003) | ✅ |
| 4 | *Drohnen* → **Ausleihen** on the TELLO-07 bay → Group 3, 4 Akkus, 8 Propeller | TELLO-07 already chosen; bar "TELLO-07 für Group 3"; "TELLO-07 an Group 3 ausgeliehen: 4 Akkus, 8 Propeller." (REQ-004, REQ-006) | ✅ |
| 5 | *Neue Ausleihe* → open "Ausgeliehen (1)", TELLO-07 again for Group 1 | "Die Drohne "TELLO-07" ist bereits ausgeliehen." (REQ-005, 409) | ✅ |
| 6 | *Drohnen* | TELLO-07 **Ausgeliehen**, "0 von 1 im Schrank" (REQ-002) | ✅ |
| 7 | *Aktive Ausleihen* | one row: TELLO-07 / Group 3 / 4 / 8 / 12.11.2026 / seit heute (REQ-007) | ✅ |
| 8 | *Schülergruppen* → Group 3 shows "hat TELLO-07"; **Löschen** | "… hat Ausleihen und kann nicht gelöscht werden. Der Verlauf bleibt erhalten." (REQ-012, 409) | ✅ |
| 9 | *Aktive Ausleihen* → **Zurückgeben** | "TELLO-07 von Group 3 zurückgegeben. Sie ist wieder verfügbar.", no active loan left (REQ-008) | ✅ |
| 10 | *Verlauf* | one row: TELLO-07 / Group 3 / 4 / 8 / 12.11.2026 / 12.11.2026 (REQ-009, REQ-010) | ✅ |
| 11 | *Schülergruppen* → Group 1 **Umbenennen** to "Gruppe 1" | "Group 1 heißt jetzt Gruppe 1." (REQ-011) | ✅ |
| 12 | **Stop the backend** and start it again with the same database | "No migrations were applied. The database is already up to date." | ✅ |
| 13 | *Verlauf* after the restart | the same row is still there (REQ-010 "Loans survive a restart") | ✅ |
| 14 | *Drohnen* / *Aktive Ausleihen* after the restart | TELLO-07 **Verfügbar**, no active loan | ✅ |
| 15 | Every page at 400 px | readable, bottom navigation, no horizontal scrolling | ✅ |

The steps were driven with a Playwright script in Chrome (not part of the repository); no browser errors were logged. The loan date and return date were the day of the run.

## Automated tests in the same state (`main` at b569500)

| Command | Result |
|---|---|
| `dotnet test backend/DroneInventory.slnx` | 131 passed, 0 failed |
| `npx ng test --watch=false` (in `frontend/drone-inventory-ui`) | 76 passed, 0 failed |
| `npx ng build` | no errors, no warnings |
| `openspec validate improve-frontend-design --strict` | valid |
| `openspec validate manage-student-groups --strict` | valid |

## How to repeat it

Follow the steps above by hand; it takes about three minutes. Start with an empty database so that the drone list, active loans and history begin empty.

## Run 3 – feature backlog (REQ-013 … REQ-024), 12.11.2026

Same setup (own empty database file, Chrome 1280 × 860, then 400 px). Driven with a Playwright script (not part of the repository); `main` at 65631ce.

| # | Step in the UI | Expected (spec) | Result |
|---|---|---|---|
| 1 | *Drohnen* → add "TELO-07" with SSID "TELLO-9FA21B" and "TELLO-11" without SSID | bay shows "SSID TELLO-9FA21B" (REQ-024) | ✅ |
| 2 | **Bearbeiten** TELLO-11, SSID "tello-9fa21b" | backend 409 "… SSID … gibt es bereits" in the sheet (REQ-024) | ✅ |
| 3 | **Bearbeiten** TELO-07 → name "TELLO-07" | "TELLO-07 gespeichert." (REQ-013) | ✅ |
| 4 | **Defekt melden** TELLO-11, "Propeller gebrochen" | status **Defekt**, reason in the bay (REQ-014, REQ-002) | ✅ |
| 5 | *Schülergruppen* → Group 3, **+ Schüler hinzufügen** Anna Berger and Ben Huber (4AHIF) | both listed in Group 3 (REQ-016, REQ-017) | ✅ |
| 6 | Anna Berger → **Verantwortlich machen** | Group 3 shows "verantwortlich" (REQ-020) | ✅ |
| 7 | **Löschen** Group 3 | 409 "… hat noch Schüler …" (REQ-012) | ✅ |
| 8 | *Neue Ausleihe* → damaged TELLO-11 for Group 3 | 409 "… ist defekt …" (REQ-004) | ✅ |
| 9 | *Neue Ausleihe* → TELLO-07, Group 3, 4 Akkus, 8 Propeller | bar shows Anna Berger; active loan lists her (REQ-021) | ✅ |
| 10 | *Aktive Ausleihen* → **Zurückgeben**: 3 Akkus, Schaden "Kamera ausgefallen" | "… zurückgegeben und als defekt markiert." (REQ-022, REQ-023) | ✅ |
| 11 | *Verlauf* | "3 von 4", report "Kamera ausgefallen" (REQ-022, REQ-023) | ✅ |
| 12 | *Drohnen* → TELLO-07 **Defekt**; TELLO-11 **Repariert** | TELLO-11 **Verfügbar** (REQ-015) | ✅ |
| 13 | Every page at 400 px | no horizontal scrolling | ✅ |

No browser errors were logged. Automated tests in the same state: `dotnet test` 305 passed, `npx ng test --watch=false` 135 passed, `npx ng build` without errors, no pending EF model changes.
