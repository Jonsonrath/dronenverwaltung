# End-to-end check – the assignment's example

Task 8.1 (#24) of the OpenSpec change `initial-drone-lending-system`.
The example from the assignment, run once completely through the user interface against the real backend and database.

## Setup

1. Backend stopped, `backend/DroneInventory.Api/droneinventory.db*` deleted (empty database).
2. `cd backend/DroneInventory.Api && dotnet run` (http://localhost:5087)
3. `cd frontend/drone-inventory-ui && npx ng serve` (http://localhost:4200, `/api` proxied to the backend)
4. Browser: Chrome, 1280 × 800.

## Steps and results – 05.11.2026

| # | Step in the UI | Expected (spec) | Result |
|---|---|---|---|
| 1 | *Drones* → add "TELLO-07", description "SSID: TELLO-9FA21B, rechter Propellerschutz beschädigt" | drone created, status **Available** (REQ-001) | ✅ |
| 2 | *Student groups* → add "Group 3" | group listed (REQ-003) | ✅ |
| 3 | *New loan* → TELLO-07, Group 3, 4 batteries, 8 propellers | "TELLO-07 loaned to Group 3 with 4 batteries and 8 propellers." (REQ-004, REQ-006) | ✅ |
| 4 | *Drones* | TELLO-07 is **Loaned** (REQ-002) | ✅ |
| 5 | *Active loans* | one row: Group 3 / TELLO-07 / 4 / 8 / loan date (REQ-007) | ✅ |
| 6 | *Active loans* → **Return** | "TELLO-07 returned by Group 3. It is available again.", no active loan left (REQ-008) | ✅ |
| 7 | *History* | one row: Group 3 / TELLO-07 / 4 / 8 / loan date / return date (REQ-009, REQ-010) | ✅ |
| 8 | **Stop the backend** and start it again with the same `droneinventory.db` | no migration applied on the second start (database already up to date) | ✅ |
| 9 | *History* after the restart | the same row is still there (REQ-010 "Loans survive a restart") | ✅ |
| 10 | *Drones* / *Active loans* after the restart | TELLO-07 **Available**, no active loan | ✅ |

The steps were driven with a Playwright script in Chrome (not part of the repository); the loan date and return date were the day of the run.

## Automated tests in the same state

| Command | Result |
|---|---|
| `dotnet test backend/DroneInventory.slnx` | 108 passed, 0 failed |
| `npx ng test --watch=false` (in `frontend/drone-inventory-ui`) | 48 passed, 0 failed |
| `npx ng build` | no errors, no warnings |
| `openspec validate initial-drone-lending-system --strict` | valid |

## How to repeat it

Follow the steps above by hand; it takes about two minutes. Start with an empty database so that the drone list, active loans and history begin empty.
