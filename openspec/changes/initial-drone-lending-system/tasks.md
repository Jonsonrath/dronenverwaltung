# Tasks

Each task is one GitHub Issue, one branch and one pull request. Implement only the selected task.

## 1. Backend: setup

- [ ] 1.1 Create solution `backend/DroneInventory.slnx` and xUnit test project `backend/DroneInventory.Api.Tests` referencing the API; remove the weather-forecast template endpoint; verify `dotnet build` and `dotnet test` succeed (one placeholder test) (#2)
- [ ] 1.2 Add EF Core SQLite, `DroneInventoryDbContext`, connection string `Data Source=droneinventory.db`, `TimeProvider` registration and `Database.Migrate()` at startup; verify the app starts and creates the database file (#3)
- [ ] 1.3 Add domain exceptions (`NotFoundException`, `ConflictException`, `ValidationException`), an `IExceptionHandler` mapping them to ProblemDetails 404/409/422, and 422 for automatic model validation; verify with tests for each mapping (#4)

## 2. Backend: drones (spec drone)

- [ ] 2.1 Implement `Drone` entity (Id, Name, Description, name required) and its EF mapping with migration `InitialCreate` part for drones; verify with a test that a blank name throws `ValidationException` (REQ-001) (#5)
- [ ] 2.2 Implement `DroneService` and `POST /api/drones`; verify with API tests: create returns 201 with id and status Available, description optional, blank name returns 422 (REQ-001 scenarios) (#6)
- [ ] 2.3 Implement `GET /api/drones` with status derived from active loans (no status column); verify with API tests for an empty list and status Available (REQ-002); Loaned is verified in 4.4 (#7)

## 3. Backend: student groups (spec student-group)

- [ ] 3.1 Implement `StudentGroup` entity, EF mapping and migration; verify with a test that a blank name throws `ValidationException` (REQ-003) (#8)
- [ ] 3.2 Implement `POST /api/student-groups` and `GET /api/student-groups`; verify with API tests: create returns 201, blank name returns 422, list and empty list (REQ-003 scenarios) (#9)

## 4. Backend: loans (spec loan)

- [ ] 4.1 Implement `Loan` (Status Active|Returned, LoanDate, ReturnDate, `Return(now)`) and `LoanItem` (Type Battery|Propeller, Quantity >= 0) with EF mapping, cascade, migration and the filtered unique index `Loans(DroneId) WHERE Status = 'Active'`; verify with domain tests (negative quantity throws, `Return` twice throws) and a DbContext test that two active loans for one drone fail (#10)
- [ ] 4.2 Implement `LoanService.CreateLoan` and `POST /api/loans` (always two LoanItems, missing quantity = 0, LoanDate from `TimeProvider`); verify with tests: loan available drone 201, 4 batteries / 8 propellers, 0 / 0, unknown drone 404, unknown group 404, negative quantity 422 (REQ-004, REQ-006) (#11)
- [ ] 4.3 Reject a second active loan for the same drone with 409; verify with tests: loaned drone cannot be loaned again and the existing loan is unchanged, drone can be loaned again after return (REQ-005) (#12)
- [ ] 4.4 Implement `GET /api/loans?status=Active` with group name, drone name, batteries, propellers, LoanDate; verify with tests for two active loans (fixed date 19.08.2026 via `FakeTimeProvider`), returned loans not listed, and `GET /api/drones` showing Loaned (REQ-007, REQ-002) (#13)
- [ ] 4.5 Implement `POST /api/loans/{id}/return`; verify with tests: status Returned and drone Available again, ReturnDate set and other fields unchanged, already returned 409, unknown loan 404 (REQ-008, REQ-009) (#14)
- [ ] 4.6 Implement `GET /api/loans?status=Returned` (loan history); verify with tests: returned loan listed with LoanDate and ReturnDate, active loan not listed, loan still listed after restarting the app on the same database file (REQ-010) (#15)

## 5. Frontend: setup

- [ ] 5.1 Add `provideHttpClient()`, `proxy.conf.json` (`/api` → `http://localhost:5087`), shared models in `core/`, routes with navigation (Drones, Student Groups, New Loan, Active Loans, History) and base styles in `styles.css`; verify `npx ng build` succeeds and navigation works in the browser at desktop and phone width (#16)

## 6. Frontend: drones and student groups

- [ ] 6.1 Implement `DroneApi` service and drone list page (name, status, description); verify with a Vitest service test (`GET /api/drones`) and manually against the running backend (REQ-002) (#17)
- [ ] 6.2 Implement the drone create form on the drone page; verify with a Vitest test that the backend error message (422) is shown, and manually that a new drone appears as Available (REQ-001) (#18)
- [ ] 6.3 Implement `StudentGroupApi` service and student group page (list + create form); verify with a Vitest service test and manually that a new group appears and a blank name shows the backend error (REQ-003) (#19)

## 7. Frontend: loans

- [ ] 7.1 Implement `LoanApi` service and the loan form (select drone and group, batteries, propellers); verify with a Vitest test that a 409 message is shown, and manually that a loan is created and the drone becomes Loaned (REQ-004, REQ-005, REQ-006) (#20)
- [ ] 7.2 Implement the active loans page (group, drone, batteries, propellers, loan date as dd.MM.yyyy); verify with a Vitest component test rendering two loans, and manually against the backend (REQ-007) (#21)
- [ ] 7.3 Add the return action to the active loans page; verify with a Vitest test that return calls `POST /api/loans/{id}/return` and the list reloads, and manually that the drone is Available again (REQ-008, REQ-009) (#22)
- [ ] 7.4 Implement the loan history page (group, drone, accessories, loan date, return date); verify with a Vitest component test and manually that a returned loan appears (REQ-010) (#23)

## 8. Integration and documentation

- [ ] 8.1 Run the example from the assignment end to end (create TELLO-07 and Group 3, loan with 4 batteries / 8 propellers, check active loans, return, check history, restart backend, history still there); verify `dotnet test` and `npx ng test --watch=false` pass and update README run instructions if anything changed (#24)

## Workflow follow-up

- Create GitHub Issues for these tasks with `/openspec-to-github initial-drone-lending-system` and write the issue numbers back into this file.
- Implement each issue with `openspec-apply-with-github` on its own branch `feature/<issue-number>-<name>`.
- Archive the change with `openspec-archive-with-github` after all tasks are implemented, tested, reviewed and merged.
