# Tasks

Each task is one GitHub Issue, one branch and one pull request. Implement only the selected task.

## 1. Backend

- [x] 1.1 `Drone.Edit` and `DroneService.UpdateAsync` with the uniqueness check against other drones and the unique-violation fallback (design §2); verify with domain and service tests named `REQ_013_...` (#79)
- [ ] 1.2 `PUT /api/drones/{id}` with 200 / 404 / 409 / 422 (design §1); verify with an API test for every scenario of REQ-013 (#80)

## 2. Frontend

- [ ] 2.1 `DroneApi.update` and *Bearbeiten* with a sheet on the drones page (design §4); verify with Vitest tests for the request, the reloaded list and the shown 409 / 422 messages, `npx ng build`, and one run through the real UI (#81)

## 3. Docs

- [ ] 3.1 Add REQ-013 to `docs/requirements.md` and the README; verify the README table and `openspec validate edit-drones --strict` (#82)

## Workflow follow-up

- After the specs are reviewed: create GitHub Issues with `/openspec-to-github edit-drones` and write the issue numbers back into this file.
- Implement each issue with `openspec-apply-with-github` on its own branch `feature/<issue-number>-<name>`, one task per agent run.
- Archive with `openspec-archive-with-github` after all tasks are implemented, tested, reviewed and merged.
