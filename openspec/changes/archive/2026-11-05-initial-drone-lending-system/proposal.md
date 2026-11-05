# Proposal

## Why

Drones and accessories are loaned to student groups, and today this is tracked on a sheet of paper on the cabinet door. Nobody can reliably tell which drones are available, which group has which drone, how many batteries and propellers were handed out, or which loans existed in the past. The Drone Lending System replaces the paper with a traceable, persistent record that the class will also use in the second semester.

## What Changes

- Manage drones: create a drone (name, description) and list all drones with their availability (Available | Loaned). Availability is derived from the active loan, never entered (REQ-001, REQ-002).
- Manage student groups: create a student group with a name (REQ-003).
- Loan a drone to a student group, including batteries and propellers as quantities (REQ-004, REQ-006).
- Enforce that a drone has at most one active loan (REQ-005).
- Show all active loans with group, drone, batteries, propellers and loan date (REQ-007).
- Return a loan: the loan becomes Returned, the return date is stored and the drone becomes available again (REQ-008, REQ-009).
- Keep returned loans forever and show them as loan history (REQ-010).
- All domain rules are enforced in the backend. The Angular frontend only displays data and sends requests via the REST API.

Source: `docs/requirements.md` (REQ-001 … REQ-010).

### Out of Scope

- Login, user accounts, roles
- Individual battery inventory (BAT-001 …); accessories are quantities only
- Reservations in advance
- Damage management and return differences (missing accessories on return)
- Statistics, reports, exports
- Editing or deleting drones and student groups

These follow later as separate change requests.

## Capabilities

### New Capabilities
- `drone`: creating drones and listing them with their derived availability (REQ-001, REQ-002).
- `student-group`: creating student groups (REQ-003).
- `loan`: loaning a drone with accessories, the one-active-loan rule, active loans, returning a loan with return date, and the loan history (REQ-004 … REQ-010).

### Modified Capabilities
<!-- none: this is the first change, no specs exist yet -->

## Impact

- **Backend** (`backend/DroneInventory.Api`): new domain model (`Drone`, `StudentGroup`, `Loan`, `LoanItem`), EF Core with SQLite, REST endpoints for drones, student groups and loans. The weather-forecast template endpoint is removed.
- **Backend tests**: a new xUnit test project; one test per Given/When/Then scenario.
- **Frontend** (`frontend/drone-inventory-ui`): new views for drones, student groups, loan form, active loans with return action and loan history; HTTP client to the backend.
- **Dependencies**: `Microsoft.EntityFrameworkCore.Sqlite` (backend); no new frontend dependencies expected.
