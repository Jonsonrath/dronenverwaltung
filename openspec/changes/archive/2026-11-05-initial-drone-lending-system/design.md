# Design

## Context

See proposal.md for the motivation and specs/ for the required behavior. Current state:

- `backend/DroneInventory.Api`: empty ASP.NET Core 10 Web API template (weather-forecast endpoint, OpenAPI). HTTP port 5087. No EF Core, no test project yet.
- `frontend/drone-inventory-ui`: empty Angular 22 app (standalone, routing, CSS, Vitest). No routes, no HTTP client.
- No existing specs; this is the first change.

Constraints from the assignment: domain rules live in the backend, the frontend only talks to the REST API, availability is derived and not stored, returned loans are never deleted.

## Goals / Non-Goals

**Goals:**
- A small, layered backend in one project in which every domain rule has exactly one place.
- Every spec scenario is covered by an automated backend test.
- Data model and migrations that can absorb the planned change requests (damaged drones, individual batteries, …) without starting over.

**Non-Goals:**
- No authentication, no CORS setup for production, no deployment.
- No pagination, search or sorting beyond a fixed order.
- No generic repository layer; EF Core's `DbContext` is used directly by the services.

## Decisions

### 1. Backend structure: one API project with folders, plus one test project
Folders in `DroneInventory.Api`: `Domain/` (entities and domain exceptions), `Data/` (`DroneInventoryDbContext`, migrations), `Services/` (use cases), `Controllers/` (HTTP only), `Contracts/` (request/response DTOs). Tests live in `backend/DroneInventory.Api.Tests` (xUnit), referenced from a solution `backend/DroneInventory.slnx`.
*Alternative:* separate Domain/Infrastructure projects (clean architecture). Rejected: too much ceremony for four entities; the folders keep the same separation.

### 2. Controllers instead of minimal APIs
`DronesController`, `StudentGroupsController`, `LoansController`. Controllers stay thin: map DTOs, call a service, return a result.
*Alternative:* minimal APIs (as in the template). Rejected: controllers group endpoints per resource and match the "Drone API / Loan API" tasks one to one.

### 3. Loan is the aggregate root; rules live on the entities and in `LoanService`
- `Loan` owns its `LoanItem`s (cascade, no own endpoint). `Loan.Return(now)` sets `Status = Returned` and `ReturnDate` and throws if the loan is already returned. Status and ReturnDate have private setters.
- `LoanItem` validates `Quantity >= 0` on creation. Each loan always gets exactly two LoanItems (Battery, Propeller), also with quantity 0, so every loan shows both numbers.
- `LoanService.CreateLoan` checks: drone exists, group exists, quantities ≥ 0, drone has no active loan.
*Alternative:* `LoanItem` as owned value object without Id. Rejected: the assignment's model gives LoanItem an Id, and change request 4 (individual batteries) will need a real entity anyway.

### 4. Availability is computed, never stored
`Drone` has no status column. The drone list is projected with `Loans.Any(l => l.DroneId == d.Id && l.Status == Active)` → `Available | Loaned`. `Loan` keeps its own `Status` (Active | Returned) as the single source of truth.
In addition, a **filtered unique index** on `Loans(DroneId)` with the filter `"Status" = 'Active'` guarantees REQ-005 in the database even if two requests arrive at the same time.
*Alternative:* `Drone.Status` column. Rejected: two truths that can drift apart (assignment, "Architekturfrage").

Because the drone status needs the `Loan` entity, the drone endpoints first return id, name and description (tasks 2.2, 2.3); the derived status is added in task 4.7 after loans exist. This keeps every task implementable and testable without pulling later work forward.

### 4a. Enum mapping and the one-active-loan guarantee
- `LoanStatus` (Active | Returned) and `LoanItemType` (Battery | Propeller) are stored as **strings** (`HasConversion<string>()`), not as integers. Only then the index filter `"Status" = 'Active'` matches, and the database stays readable.
- `LoanService.CreateLoan` first checks for an active loan and throws `ConflictException` (normal case).
- If two requests pass that check at the same time, the second `SaveChanges` violates the filtered unique index. `LoanService` catches this `DbUpdateException` (inner `SqliteException` with error code 19, constraint violation) and throws `ConflictException`, so the client gets **409, never 500**.
- A test sends two `POST /api/loans` for the same drone in parallel and expects one 201, one 409 and exactly one active loan.

### 4c. Returning a loan only once
`Loan.Status` is an EF Core **concurrency token**: the UPDATE of a return only succeeds if the row still has the status that was loaded (`Active`). If two requests load the same active loan and both call `Return()`, the second `SaveChanges` updates no row and throws `DbUpdateConcurrencyException`; `LoanService.ReturnAsync` maps it to `ConflictException` (409), so the first ReturnDate is never overwritten. The token needs no extra column and no migration (`dotnet ef migrations has-pending-model-changes` reports no changes).

### 4b. Unique drone and group names
Names are trimmed before saving. Uniqueness ignores upper/lower case **including non-ASCII letters** (e.g. "Ärzte" = "ärzte"): `Drone` and `StudentGroup` keep an additional column `NormalizedName = Name.Trim().ToUpperInvariant()`, set only by the entity, with a unique index on it. The displayed `Name` keeps its original spelling. The service checks `NormalizedName` first and throws `ConflictException`; a unique-constraint violation from the index is mapped to `ConflictException` the same way as in 4a, so duplicates always give 409.
*Alternative:* `COLLATE NOCASE`. Rejected: SQLite's NOCASE only folds ASCII letters, so "Ärzte" and "ärzte" would count as different names.

### 5. Error mapping
Domain and service code throws small exceptions (`NotFoundException`, `ConflictException`, `ValidationException`). One `IExceptionHandler` maps them to RFC 7807 ProblemDetails:

| Situation | Status |
|---|---|
| created | 201 Created |
| invalid input (blank name, negative quantity, malformed body) | 422 Unprocessable Entity |
| unknown drone / student group / loan | 404 Not Found |
| drone already loaned, loan already returned, duplicate drone or group name | 409 Conflict |

ASP.NET's automatic model-validation response is switched from 400 to 422 so all invalid input looks the same.
*Alternative:* return `Result` objects instead of exceptions. Rejected: more code for the same effect at this size.

### 6. REST endpoints

| Method | Path | Purpose | Spec |
|---|---|---|---|
| GET | `/api/drones` | list drones with status | REQ-002 |
| POST | `/api/drones` | create drone `{ name, description? }` | REQ-001 |
| GET | `/api/student-groups` | list student groups | REQ-003 |
| POST | `/api/student-groups` | create group `{ name }` | REQ-003 |
| POST | `/api/loans` | create loan `{ droneId, studentGroupId, batteries, propellers }` | REQ-004/005/006 |
| GET | `/api/loans?status=Active` | active loans | REQ-007 |
| GET | `/api/loans?status=Returned` | loan history | REQ-010 |
| POST | `/api/loans/{id}/return` | return a loan | REQ-008/009 |

Loan responses are flat: `{ id, droneId, droneName, studentGroupId, studentGroupName, batteries, propellers, loanDate, returnDate, status }`. Enums are serialized as strings.
Return is a `POST` action and not `DELETE`, because nothing is deleted (REQ-010).

### 7. Time via `TimeProvider`
LoanDate and ReturnDate are set from an injected `TimeProvider` and stored in UTC. Tests use `FakeTimeProvider` to get fixed dates like 19.08.2026. The frontend shows dates as `dd.MM.yyyy` in local time.

### 8. Persistence: SQLite with EF Core migrations
Connection string `Data Source=droneinventory.db` in `appsettings.json`; the `.db` file is git-ignored. Schema via EF Core migrations (`dotnet ef migrations add`), applied at startup with `Database.Migrate()`.

Migrations are created in this order, one per task, and each task's tests run against exactly that state:

| Task | Migration | Creates |
|---|---|---|
| 1.2 | – | empty `DbContext`, `Migrate()` with no migrations is a no-op |
| 2.1 | `InitialCreate` | `Drones` with unique `NormalizedName` |
| 3.1 | `AddStudentGroups` | `StudentGroups` with unique `NormalizedName` |
| 4.1 | `AddLoans` | `Loans`, `LoanItems`, filtered unique index on `Loans(DroneId)` |

Tests build their database by applying the same migrations (`Database.Migrate()` on an in-memory SQLite connection), not with `EnsureCreated()`, so tests and production use the same schema. Migrations are never edited after they are merged; a fix is a new migration.
*Alternative:* `EnsureCreated()`. Rejected: it cannot evolve the schema, and the change requests will change it.

### 9. Backend tests
Two kinds, both in `DroneInventory.Api.Tests`:
- **Service tests** against a real SQLite in-memory connection (not the EF InMemory provider, so the filtered unique index and constraints are real).
- **API tests** with `WebApplicationFactory<Program>` for status codes and JSON shape.
Test names start with the requirement id, e.g. `REQ_005_RejectsLoanOfAlreadyLoanedDrone`. The restart scenario of REQ-010 uses a temporary database file and a second app instance.

### 10. Frontend structure
- Standalone components and signals as in the `angular-developer` skill. Feature folders: `drones/`, `student-groups/`, `loans/`, and `core/` for API services and models.
- One service per resource (`DroneApi`, `StudentGroupApi`, `LoanApi`) using `HttpClient`; `provideHttpClient()` in `app.config.ts`.
- Routes: `/drones` (list + create form), `/student-groups` (list + create form), `/loans/new` (loan form), `/loans` (active loans with return button), `/loans/history`. Default route → `/drones`.
- Forms use Angular 22 Signal Forms; client-side checks are only for convenience (required, min 0). The backend's ProblemDetails message is always shown on error.
- Dev setup uses an Angular proxy (`proxy.conf.json`: `/api` → `http://localhost:5087`) so no CORS configuration is needed.
- Plain CSS with a few CSS custom properties (colors, spacing) in `styles.css`; one consistent look, usable at phone width.
*Alternative:* Angular Material. Rejected for now: adds a dependency and styling layer the team would also have to explain; can be added later.

### 11. Frontend tests
Vitest: API services with `HttpTestingController` (correct URL/method/body) and components for the main display and error cases (e.g. a 409 message is shown). Domain rules are not re-tested in the frontend.

## Risks / Trade-offs

- [SQLite filtered index syntax and EF Core translation] → covered by a test that creates two active loans for the same drone directly in the DbContext and expects a failure.
- [Parallel requests on SQLite are serialized by the database lock, so the race may be hard to provoke] → the parallel test asserts the outcome (one 201, one 409, one active loan), and the `DbUpdateException` mapping is additionally tested directly in a service test.
- [Exceptions for control flow can hide bugs] → only three custom exception types, everything else stays a 500 and is visible in tests.
- [Signal Forms are new; little team experience] → the `angular-developer` skill has a reference; fallback to Reactive Forms is allowed if it blocks progress (implementation detail only, no spec change).
- [Dates in UTC vs. local display] → tests compare dates only; display formatting is done in one pipe/place.
- [Weather template removal touches `Program.cs`] → done in the first backend task together with the new setup.

## Migration Plan

Greenfield: no data to migrate. The schema is built by the three migrations from §8 (`InitialCreate` → `AddStudentGroups` → `AddLoans`). Rollback during development = delete `droneinventory.db`.

## Team Decisions

- A loan with 0 batteries and 0 propellers is allowed (quantities >= 0, as in the assignment).
- Drone names and student group names are unique, ignoring case (also for umlauts) and surrounding whitespace (see 4b).
