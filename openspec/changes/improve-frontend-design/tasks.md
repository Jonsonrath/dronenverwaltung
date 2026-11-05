# Tasks

Each task is one GitHub Issue, one branch and one pull request. Implement only the selected task. Frontend only – no backend task in this change.

## 1. Frontend: theme and shell

- [ ] 1.1 Replace the light tokens in `styles.css` with the dark OSD palette from design §1 (bg, panel, panel-raised, line, text, text-muted, loaned, available, error), restyle the shared base styles (buttons, fields, tables, alerts, empty state, status LED with glow, tile corner brackets) and check AA contrast of every text/status color on `--bg` and `--panel`; verify `npx ng build`, all existing `npx ng test` pass and screenshots of all five pages at 1280 px and 390 px are readable (#52)
- [ ] 1.2 Rebuild the app shell: side navigation on desktop (≥ 64rem), top bar on tablet, bottom tab bar on phones (design §2); verify the navigation tests in `app.spec.ts` (links, active link, routes) pass and screenshots at 1280 / 800 / 390 px show the right navigation (#53)

## 2. Frontend: drones and student groups

- [ ] 2.1 Drones page: drones as tiles with corner brackets, name, status LED + word and description (design §3), "Add a drone" form as sticky side panel on desktop and below on phones; update `drones.spec.ts` selectors to the tile list without removing any assertion; verify all drone page tests pass and manually that adding a drone shows a new Available tile (#54)
- [ ] 2.2 Student groups page: list and "Add a student group" form side by side on desktop, stacked on phones, dark styling; verify `student-groups.spec.ts` passes and manually that a new group appears and a blank name shows the backend error (#55)

## 3. Frontend: loans

- [ ] 3.1 Add a reusable `QuantityStepper` control in `shared/` (−, number field, +; "−" disabled at 0; works with `[formField]` as Signal Forms custom control, design §4); verify with Vitest tests for +, −, disabled at 0, typed value and binding to a form field (#56)
- [ ] 3.2 Loan form: drones as radio tiles grouped into *Available* / *Loaned right now*, student groups as radio chips, batteries and propellers with `QuantityStepper` (design §4); update `new-loan.spec.ts` to the new controls keeping every assertion (request body, 409 and 422 messages, confirmation, empty prerequisites); verify all tests pass and manually that a loan is created by tapping only (no keyboard) and a loaned drone still gives the backend's 409 message (#57)
- [ ] 3.3 Active loans and history: dark table styling, Return button and status messages in the new style, phone layout kept (design §5); verify `active-loans.spec.ts` and `loan-history.spec.ts` pass unchanged and screenshots at 1280 px and 390 px (#58)

## 4. Integration

- [ ] 4.1 Run the assignment's example end to end again through the new UI (as in `docs/end-to-end-check.md`) and update that document and the README if anything changed; verify `dotnet test` and `npx ng test --watch=false` pass and screenshots of all pages at desktop and phone width (#59)

## Workflow follow-up

- Create GitHub Issues for these tasks with `/openspec-to-github improve-frontend-design` and write the issue numbers back into this file.
- Implement each issue with `openspec-apply-with-github` on its own branch `feature/<issue-number>-<name>`.
- Archive the change with `openspec-archive-with-github` after all tasks are implemented, tested, reviewed and merged (`skip_specs: true` – no main spec changes).
