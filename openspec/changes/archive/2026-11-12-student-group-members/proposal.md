# Proposal

## Why

Change request 3 of the assignment: a StudentGroup is only a name today. At the cabinet nobody knows who belongs to "Group 3". The team asked to add students to groups.

Part of the feature backlog planned on 12.11.2026 (one OpenSpec change per feature). Builds on the changes `improve-frontend-design` and `manage-student-groups` (pull requests #71, #73). Requires `manage-student-groups` to be archived first, because this change modifies its requirement REQ-012.

## What Changes

- **New domain term `Student`** (Id, Name, Class, StudentGroupId). A Student belongs to exactly one StudentGroup.
- **Add a student (REQ-016)** to a group with name and an optional class; the name is unique within the group.
- **List the members (REQ-017)** of every group.
- **Move a student (REQ-018)** to another group.
- **Remove a student (REQ-019).**
- **Delete group (MODIFIED REQ-012):** a group that still has students cannot be deleted – they are moved or removed first.
- **UI:** the student groups page shows the members on each plate; adding, moving and removing in sheets.

### Out of Scope

- A student in several groups at the same time (decision of the team: one group per student)
- Student accounts, login, e-mail addresses
- The responsible student (change `responsible-student`)

## Capabilities

### New Capabilities
<!-- none: students are part of the student-group capability -->

### Modified Capabilities
- `student-group`: new requirements REQ-016 … REQ-019; modified "Delete student group (REQ-012)".

## Impact

- **Backend:** entity `Student`, table `Students` with foreign key to `StudentGroups` (Restrict), migration `AddStudents`, `StudentService`, endpoints under `/api/student-groups/{groupId}/students` and `/api/students/{id}`.
- **Frontend:** `StudentApi`, student groups page.
- **Rules:** AGENTS.md and `openspec/config.yaml` get the term `Student`.
