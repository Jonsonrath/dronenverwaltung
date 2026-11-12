# Tasks

Each task is one GitHub Issue, one branch and one pull request. Implement only the selected task. Start after `manage-student-groups` is archived.

## 1. Backend

- [ ] 1.1 Entity `Student`, mapping with unique index (StudentGroupId, NormalizedName), foreign key Restrict, migration `AddStudents` (design §1); verify with domain tests `REQ_016_...` and mapping tests (#90)
- [ ] 1.2 Add student and members in the group list: `POST /api/student-groups/{groupId}/students`, `students` in `GET /api/student-groups` (design §2); verify with API tests for every scenario of REQ-016 and REQ-017 (#91)
- [ ] 1.3 Move and remove: `PUT /api/students/{id}/group`, `DELETE /api/students/{id}`; verify with API tests for every scenario of REQ-018 and REQ-019 (#92)
- [ ] 1.4 Group delete rejects groups with students (design §3, MODIFIED REQ-012); verify with a test for "Student group with students" and all earlier REQ-012 tests unchanged (#93)

## 2. Frontend

- [ ] 2.1 `StudentApi` and members on the group plates with *Schüler hinzufügen* (design §4); verify with Vitest tests for the request, the reloaded list and the 409 / 422 messages (#94)
- [ ] 2.2 *Verschieben* and *Entfernen* per student in a sheet; verify with Vitest tests, `npx ng build`, and one run through the real UI incl. deleting a group with students (409 shown) (#95)

## 3. Rules and docs

- [ ] 3.1 AGENTS.md and `openspec/config.yaml`: add `Student` to the domain terms; `docs/requirements.md` REQ-016 … REQ-019 and the modified REQ-012; verify `openspec validate student-group-members --strict` (#96)

## Workflow follow-up

- After the specs are reviewed: create GitHub Issues with `/openspec-to-github student-group-members` and write the issue numbers back into this file.
- Implement each issue with `openspec-apply-with-github` on its own branch `feature/<issue-number>-<name>`, one task per agent run.
- Archive with `openspec-archive-with-github` after all tasks are implemented, tested, reviewed and merged.
