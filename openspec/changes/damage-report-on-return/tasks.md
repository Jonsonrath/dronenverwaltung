# Tasks

Each task is one GitHub Issue, one branch and one pull request. Implement only the selected task. Start after `support-damaged-drones` is merged.

## 1. Backend

- [x] 1.1 `Loan.DamageReport`, migration `AddLoanDamageReport`, `ReturnAsync` sets the report and damages the drone in one transaction (design §1, §3); verify with service tests `REQ_023_...` (#108)
- [ ] 1.2 `damageReport` in the return body and in loan responses (design §2); verify with API tests for every scenario of REQ-023 (#109)

## 2. Frontend

- [ ] 2.1 Field *Schaden (optional)* in the return sheet, confirmation text, history column (design §4); verify with Vitest tests for the request body and the shown report, `npx ng build`, and one run through the real UI (returned with damage → drone shows *Defekt*, loan form gets 409) (#110)

## 3. Docs

- [ ] 3.1 `docs/requirements.md` REQ-023 and the README; verify `openspec validate damage-report-on-return --strict` (#111)

## Workflow follow-up

- After the specs are reviewed: create GitHub Issues with `/openspec-to-github damage-report-on-return` and write the issue numbers back into this file.
- Implement each issue with `openspec-apply-with-github` on its own branch `feature/<issue-number>-<name>`, one task per agent run.
- Archive with `openspec-archive-with-github` after all tasks are implemented, tested, reviewed and merged.
