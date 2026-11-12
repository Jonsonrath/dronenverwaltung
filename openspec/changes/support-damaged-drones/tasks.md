# Tasks

Each task is one GitHub Issue, one branch and one pull request. Implement only the selected task.

## 1. Backend

- [x] 1.1 `Drone.DamageReason`, `ReportDamage` / `Repair` in the domain and migration `AddDroneDamage` (design §1); verify with domain tests `REQ_014_...` / `REQ_015_...` and a mapping test that the column is nullable (#83)
- [x] 1.2 Status derivation Loaned / Damaged / Available in one place and `damageReason` in the drone list (design §2, MODIFIED REQ-002); verify with tests for every scenario of REQ-002 (#84)
- [x] 1.3 `POST` / `DELETE /api/drones/{id}/damage` (design §3); verify with API tests for every scenario of REQ-014 and REQ-015 (#85)
- [x] 1.4 Reject loans of damaged drones in `LoanService.CreateAsync` with 409 (design §4, MODIFIED REQ-004); verify with tests for "Damaged drone cannot be loaned" and "Repaired drone can be loaned" (#86)

## 2. Frontend

- [x] 2.1 Drones page: status *Defekt*, reason in the bay, *Defekt melden* (sheet with reason) and *Repariert* (design §5); verify with Vitest tests for both requests and the shown 404 / 409 / 422 messages, `npx ng build` (#87)
- [x] 2.2 Loan form: damaged drones in their own collapsed group, still selectable; verify that the backend's 409 for a damaged drone is shown, and one run through the real UI (#88)

## 3. Rules and docs

- [ ] 3.1 AGENTS.md and `openspec/config.yaml`: add the damage rule and remove "damage management" from out of scope (both skill copies unchanged); `docs/requirements.md` REQ-014, REQ-015 and the modified REQ-002 / REQ-004; verify `openspec validate support-damaged-drones --strict` (#89)

## Workflow follow-up

- After the specs are reviewed: create GitHub Issues with `/openspec-to-github support-damaged-drones` and write the issue numbers back into this file.
- Implement each issue with `openspec-apply-with-github` on its own branch `feature/<issue-number>-<name>`, one task per agent run.
- Archive with `openspec-archive-with-github` after all tasks are implemented, tested, reviewed and merged.
