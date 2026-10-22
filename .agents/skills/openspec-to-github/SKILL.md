---
name: openspec-to-github
description: Create and synchronize GitHub Issues from OpenSpec requirements and tasks.
---

# OpenSpec to GitHub

Use this skill to turn the tasks of an OpenSpec change into GitHub Issues and put them on the GitHub Project board.
Always use this Github project: `@Jonsonrath's Dronenverwaltung` (project number 4)

## Workflow

1. Determine the current OpenSpec change (argument, or the only active change in `openspec/changes/`).
2. Read `proposal.md`, `specs/**/*.md` and `tasks.md` of that change.
3. Check for duplicates with `gh issue list --limit 100 --state all`. Running this skill twice MUST NOT create duplicate issues. A task that already has an issue number in `tasks.md` is skipped.
4. Create one issue per task. Put the change name, the task number and the related REQ-IDs / spec scenarios in the body.
5. Add each issue to the GitHub Project (status "Backlog").
6. Write the issue number back into `tasks.md` (for example `- [ ] 2.3 ... (#16)`).
7. Do not delete or close issues. Do not tick off tasks.

## GitHub tools

Prefer the GitHub CLI `gh`.

```bash
gh issue list --limit 100 --state all
gh issue create --title "<task title>" --body "OpenSpec change: <change-name>
Task: <task number>
Requirements: <REQ-IDs>"
gh project item-add 4 --owner Jonsonrath --url <issue-url>
```
