# Proposal

## Why

Student groups can only be created. A typo in a name stays forever, and a group that was added by mistake cannot be removed, so the list on the student groups page and in the loan form only grows. The team asked for renaming and deleting on the student groups page.

## What Changes

- **Rename a student group:** the name of an existing StudentGroup can be changed. The same rules as for creating apply (not empty, unique ignoring case and leading/trailing whitespace). Loans keep pointing to the group, so active loans and the history show the new name.
- **Delete a student group:** a StudentGroup without any Loan can be deleted. A StudentGroup that has a Loan – active or returned – cannot be deleted, because returned loans are never deleted and the history must stay complete.
- **UI:** each group on the student groups page gets *Umbenennen* and *Löschen*; both open a sheet, and the backend's answers (409, 422, 404) are shown there.

### Out of Scope

- Renaming or deleting drones
- Deleting a group together with its loans, archiving or hiding groups
- Merging two groups

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `student-group`: two new requirements – "Rename student group (REQ-011)" and "Delete student group (REQ-012)".

## Impact

- **Backend:** `StudentGroup.Rename`, `StudentGroupService.RenameAsync` / `DeleteAsync`, `PUT /api/student-groups/{id}` and `DELETE /api/student-groups/{id}`; no schema change, no migration (the foreign key Loan → StudentGroup already restricts deletes).
- **Frontend:** `StudentGroupApi.rename` / `remove`, the student groups page (two actions per group, two sheets).
- **Tests:** one test per scenario (`dotnet test`), frontend tests for the calls and the shown backend messages (`ng test`).
- **Docs:** `docs/requirements.md` (REQ-011, REQ-012), README table.
