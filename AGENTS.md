# AGENTS.md – Drone Lending System

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

- `Drone` (Id, Name, Description, DamageReason)
- `StudentGroup` (Id, Name)
- `Student` (Id, Name, Class, StudentGroupId)
- `Loan` (Id, DroneId, StudentGroupId, LoanDate, ReturnDate, Status: Active | Returned)
- `LoanItem` (Id, LoanId, Type: Battery | Propeller, Quantity, ReturnedQuantity)

`Loan` is the central entity. `LoanItem` only exists inside a `Loan`. A `Student` belongs to exactly one `StudentGroup`.

### Domain Rules
- A drone has at most one active loan.
- Only available drones can be loaned. A damaged drone cannot be loaned.
- A drone is damaged while it has a damage reason (`Drone.DamageReason`, null = not damaged). Reporting damage again replaces the reason; marking the drone as repaired removes it. A loaned drone can be reported as damaged; its loan stays unchanged.
- Returning a loan makes the drone available again.
- Accessory quantities are integers and never negative.
- Returned loans are never deleted (history is kept).
- Batteries and propellers are tracked as quantities only, not as individual items.
- Drone status (Available | Loaned | Damaged) is derived: Loaned with an active loan, otherwise Damaged if a damage reason is set, otherwise Available. It is NOT stored as its own database field.
- At the return the returned quantity of each accessory is recorded (0 ... loaned quantity, default: the loaned quantity); missing = loaned − returned is derived, not stored (REQ-022).
- A student's name is unique within its student group; Class is optional free text.
- A student group with a loan or with students cannot be deleted.

## Rules
- Business logic and validation belong in the backend. Domain rules are enforced there, never only in an Angular form.
- The frontend talks only to the REST API and never accesses the database directly.
- OpenSpec specifications are the source of truth for requirements.
- Behavior changes: change the spec first, then implement.
- Every Given/When/Then scenario needs a test (`dotnet test` for backend, `ng test` for frontend).

## Backend Conventions
- Domain rules live in the domain model / services, not in controllers.
- Compute the drone status from the active loan and the damage reason in one place (`DroneStatusRules.Derive`), never store a status field on `Drone`.
- HTTP status codes: 201 on create, 404 unknown drone/group/loan/student, 409 drone already loaned (REQ-005) or damaged (REQ-004), repair of a drone that is not damaged (REQ-015), loan already returned, duplicate drone/group name or student name within a group, group with loans or students (REQ-012), 422 invalid input (blank name, blank damage reason, negative quantity, returned quantity out of range).
- Never delete a returned `Loan`; the history must stay.
- Name tests after the requirement they cover, e.g. `REQ_005_RejectsLoanOfAlreadyLoanedDrone`.
- Do not add features beyond the current task.

## Frontend Conventions
- Follow the `angular-developer` skill (official Angular skill from github.com/angular/skills, in `.agents/skills/` and `.claude/skills/`): standalone components, signal-based `input()`/`output()`, `@if`/`@for` control flow, `inject()`, `httpResource`/`HttpClient` for REST calls.
- Create the Angular app with `ng new drone-inventory-ui` as the project assignment says; generate components and services with the Angular CLI.
- Run `ng build` after generating code and fix errors before continuing.
- The frontend only displays state and sends requests. It must not re-implement domain rules; show the backend's error responses (409, 422) to the user.
- For visual design pick one clear direction (colors, typography, spacing) and keep it consistent across all pages. The UI must be usable at phone width.
- Keep the UI simple. Required views: drone list with status, drone form, student group form, loan form (with batteries/propellers), active loans, return action, loan history.

## Out of Scope (initial change)
Login/users/roles, individual battery inventory (BAT-001 ...), reservations, statistics/exports. These come later as separate change requests. (Damage management and return differences were added with the changes `support-damaged-drones` and `return-differences`.)

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

## Agent Skills
Skills live in `.agents/skills/` (Codex) and `.claude/skills/` (Claude Code). Keep both copies identical.
- `openspec-propose`, `openspec-explore`, `openspec-apply-change`, `openspec-archive-change`: OpenSpec workflow.
- `openspec-to-github`: create GitHub Issues from `tasks.md` and add them to the project.
- `openspec-apply-with-github`: set the issue to "In Progress" and implement one task.
- `openspec-archive-with-github`: set the issues to "Done" and archive the change.
- `angular-developer`: modern Angular conventions.
GitHub Project: `@Jonsonrath's Dronenverwaltung` (number 4, owner `Jonsonrath`).
