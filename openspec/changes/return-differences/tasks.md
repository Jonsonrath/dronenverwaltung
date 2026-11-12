# Tasks

Each task is one GitHub Issue, one branch and one pull request. Implement only the selected task.

## 1. Backend

- [x] 1.1 `LoanItem.ReturnedQuantity`, `Loan.Return` with returned quantities and validation, migration `AddReturnedQuantities` (design §1, §3); verify with domain tests `REQ_022_...` and that all REQ-008 / REQ-009 tests still pass (#103)
- [x] 1.2 Optional body for `POST /api/loans/{id}/return`, `returnedBatteries` / `returnedPropellers` in loan responses (design §2); verify with API tests for every scenario of REQ-022 (#104)

## 2. Frontend

- [x] 2.1 Return sheet with two steppers on the active loans page (design §4); verify with Vitest tests for the request body, the 422 message and the confirmation (#105)
- [ ] 2.2 History shows returned and missing accessories; verify with Vitest tests, `npx ng build`, and one run through the real UI (#106)

## 3. Rules and docs

- [ ] 3.1 AGENTS.md and `openspec/config.yaml`: remove "return differences" from out of scope; `docs/requirements.md` REQ-022; verify `openspec validate return-differences --strict` (#107)

## Workflow follow-up

- After the specs are reviewed: create GitHub Issues with `/openspec-to-github return-differences` and write the issue numbers back into this file.
- Implement each issue with `openspec-apply-with-github` on its own branch `feature/<issue-number>-<name>`, one task per agent run.
- Archive with `openspec-archive-with-github` after all tasks are implemented, tested, reviewed and merged.
