# Tasks

Each task is one GitHub Issue, one branch and one pull request. Implement only the selected task. Start after `student-group-members` is merged.

## 1. Backend

- [x] 1.1 `StudentGroup.ResponsibleStudentId` (Set Null) and `Loan.ResponsibleStudentName`, migration `AddResponsibleStudent` (design §1); verify with domain and mapping tests `REQ_020_...` / `REQ_021_...` (#97)
- [x] 1.2 `PUT` / `DELETE /api/student-groups/{id}/responsible`, clearing on move and remove (design §2, §3); verify with API tests for every scenario of REQ-020 (#98)
- [x] 1.3 Copy the name when a loan is created and return `responsibleStudentName` in all loan responses; verify with API tests for every scenario of REQ-021 (#99)

## 2. Frontend

- [x] 2.1 Group plates: "verantwortlich" mark and *Verantwortlich machen* (design §4); verify with Vitest tests for the request and the 422 message (#100)
- [x] 2.2 Loan summary bar, active loans and history show the responsible student; verify with Vitest tests, `npx ng build`, and one run through the real UI (#101)

## 3. Docs

- [x] 3.1 `docs/requirements.md` REQ-020, REQ-021 and the README; verify `openspec validate responsible-student --strict` (#102)

## Workflow follow-up

- After the specs are reviewed: create GitHub Issues with `/openspec-to-github responsible-student` and write the issue numbers back into this file.
- Implement each issue with `openspec-apply-with-github` on its own branch `feature/<issue-number>-<name>`, one task per agent run.
- Archive with `openspec-archive-with-github` after all tasks are implemented, tested, reviewed and merged.
