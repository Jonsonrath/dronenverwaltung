# Tasks

Each task is one GitHub Issue, one branch and one pull request. Implement only the selected task. Start after `edit-drones` is merged.

## 1. Backend

- [x] 1.1 `Drone.Ssid` / `NormalizedSsid`, filtered unique index, migration `AddDroneSsid` (design §1, §2); verify with domain and mapping tests `REQ_024_...` (two drones without SSID allowed) (#112)
- [ ] 1.2 `ssid` in create, edit and list (design §3); verify with API tests for every scenario of REQ-024 (#113)

## 2. Frontend

- [ ] 2.1 SSID field in both drone sheets and SSID in the bay (design §4); verify with Vitest tests for the request bodies and the 409 message, `npx ng build`, and one run through the real UI (#114)

## 3. Docs

- [ ] 3.1 `docs/requirements.md` REQ-024 and the README; verify `openspec validate drone-ssid --strict` (#115)

## Workflow follow-up

- After the specs are reviewed: create GitHub Issues with `/openspec-to-github drone-ssid` and write the issue numbers back into this file.
- Implement each issue with `openspec-apply-with-github` on its own branch `feature/<issue-number>-<name>`, one task per agent run.
- Archive with `openspec-archive-with-github` after all tasks are implemented, tested, reviewed and merged.
