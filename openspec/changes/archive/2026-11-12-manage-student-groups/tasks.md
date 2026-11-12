# Tasks

The change is small and its parts only work together (an API without buttons, buttons without an API), so the three tasks share one GitHub Issue, one branch and one pull request.

## 1. Backend

- [x] 1.1 Rename (REQ-011): `StudentGroup.Rename`, `StudentGroupService.RenameAsync`, `PUT /api/student-groups/{id}` with 200 / 404 / 409 / 422 (design §1, §2); verify with tests for every scenario of REQ-011 named `REQ_011_...` (#72)
- [x] 1.2 Delete (REQ-012): `StudentGroupService.DeleteAsync`, `DELETE /api/student-groups/{id}` with 204 / 404 / 409 (design §1, §3); verify with tests for every scenario of REQ-012 named `REQ_012_...`, including that the loan and the history entry still exist after a rejected delete (#72)

## 2. Frontend

- [x] 2.1 Student groups page: `StudentGroupApi.rename` / `remove`, *Umbenennen* and *Löschen* per group, each in a sheet (design §4); verify with Vitest tests for both requests, the reloaded list, the confirmation and the shown backend messages (409 for a duplicate name, 409 for a group with loans), `npx ng build`, and one run through the real UI (#72)

## Workflow follow-up

- Archive the change with `openspec-archive-with-github` after it is implemented, tested, reviewed and merged; the delta spec adds REQ-011 and REQ-012 to `openspec/specs/student-group/spec.md`.
