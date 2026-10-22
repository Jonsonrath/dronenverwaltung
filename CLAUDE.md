# Project Architecture

Drone Lending System ("Drohnenverwaltung"): drones and accessories are loaned to student groups.

## Technology Stack
- Backend: C#, ASP.NET Core 10 Web API, Entity Framework Core
- Database: SQLite
- Frontend: Angular, TypeScript
- API: REST/JSON

## Architecture Overview
Two separate projects in one repository.

```
Angular Frontend   (frontend/drone-inventory-ui)
      |
      | REST / JSON
      v
ASP.NET Core Web API   (backend/DroneInventory.Api)
      |
      v
Entity Framework Core
      |
      v
SQLite
```

## Domain (Ubiquitous Language)
Use exactly these terms in specs, code, database and conversation. Do not invent synonyms like "Verleih", "Ausgabe" or "Buchung".

- `Drone` (Id, Name, Description)
- `StudentGroup` (Id, Name)
- `Loan` (Id, DroneId, StudentGroupId, LoanDate, ReturnDate, Status: Active | Returned)
- `LoanItem` (Id, LoanId, Type: Battery | Propeller, Quantity)

`Loan` is the central entity. `LoanItem` only exists inside a `Loan`.

### Domain Rules
- A drone has at most one active loan.
- Only available drones can be loaned.
- Returning a loan makes the drone available again.
- Accessory quantities are integers and never negative.
- Returned loans are never deleted (history is kept).
- Batteries and propellers are tracked as quantities only, not as individual items.
- Drone availability (Available | Loaned) is derived from the active loan. It is NOT stored as its own database field.

## Rules
- Business logic and validation belong in the backend. Domain rules are enforced there, never only in an Angular form.
- The frontend talks only to the REST API and never accesses the database directly.
- OpenSpec specifications are the source of truth for requirements.
- Behavior changes: change the spec first, then implement.
- Every Given/When/Then scenario needs a test (`dotnet test` for backend, `ng test` for frontend).

## Backend Conventions
- Domain rules live in the domain model / services, not in controllers.
- Compute drone availability from the active loan, never store a status field on `Drone`.
- HTTP status codes: 201 on create, 404 unknown drone/group/loan, 409 drone already loaned (REQ-005) or loan already returned, 422 invalid input (blank name, negative quantity).
- Never delete a returned `Loan`; the history must stay.
- Name tests after the requirement they cover, e.g. `REQ_005_RejectsLoanOfAlreadyLoanedDrone`.
- Do not add features beyond the current task.

## Out of Scope (initial change)
Login/users/roles, individual battery inventory (BAT-001 ...), reservations, damage management and return differences, statistics/exports. These come later as separate change requests.

## Workflow
- Requirements REQ-001 ... REQ-010 are written with SHALL and at least one Given/When/Then scenario.
- Plan with `/opsx:propose`, unclear changes first with `/opsx:explore`.
- Implement exactly one task / one GitHub issue per agent run. Do not implement other tasks.
- One issue = one branch (`feature/<issue-number>-<name>`) = one pull request.
- Read `git diff` before every commit. Never tick off tasks without verifying them.
- Archive a change (`/opsx:archive`) only after it is implemented, tested and merged.
- Traceability: REQ -> openspec/specs -> task -> GitHub issue -> branch -> pull request -> code + tests.

## Project Management
- Use GitHub Issues and the GitHub Project for all work (Backlog, Ready, In Progress, Review, Done).
- Use the GitHub CLI `gh`. Never commit tokens.
- Do not delete issues.
