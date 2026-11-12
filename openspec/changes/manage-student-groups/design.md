# Design

## Context

See proposal.md – Why. Today: `StudentGroup` (Id, Name, NormalizedName with a unique index), `StudentGroupService.CreateAsync` / `ListAsync`, `GET` and `POST /api/student-groups`. `Loan.StudentGroupId` is a foreign key with `DeleteBehavior.Restrict`. The frontend shows backend states and messages only (AGENTS.md).

## Goals / Non-Goals

**Goals:** fix a wrong group name; remove a group that was never used; keep every loan and the history intact.

**Non-Goals:** soft delete, cascading deletes, changing ids.

## Decisions

### 1. API

| Request | Success | Errors |
|---|---|---|
| `PUT /api/student-groups/{id}` with `{ "name": "…" }` | 200 with the group | 404 unknown group, 422 blank name, 409 another group has the name |
| `DELETE /api/student-groups/{id}` | 204 | 404 unknown group, 409 the group has loans |

`PUT` (not `PATCH`): the name is the whole editable state of a group.

### 2. Rename
- The rule lives in the domain: `StudentGroup.Rename(name)` uses `EntityName.Require` / `Normalize` like the constructor.
- Uniqueness is checked against *other* groups, so changing only the spelling of the own name ("group 3" → "Group 3") is allowed. As for creating, the unique index is the last guard: a unique constraint violation on save becomes the same 409.
- Loans reference the group by id, so nothing else has to change; lists show the new name.

### 3. Delete
- Allowed only if no Loan references the group – neither active nor returned. Otherwise 409 with a message that names the reason. *Alternative:* delete the loans with the group – rejected, "Returned loans are never deleted (history is kept)". *Alternative:* soft delete – more than the team asked for.
- The service checks `Loans.Any(l => l.StudentGroupId == id)`; a loan created between the check and the delete is caught by the foreign key and mapped to the same 409.

### 4. Frontend
- Two quiet buttons per group plate: *Umbenennen*, *Löschen*. Each opens a sheet (the `<dialog>` pattern of the add forms): rename with the name field prefilled, delete as a confirmation that names the group.
- The frontend does not decide whether a group can be deleted: the button is always there, the backend's 409 message is shown in the sheet.
- After success the sheet closes, the list is loaded again and a confirmation is shown on the page.

## Risks / Trade-offs

- [A group with a returned loan can never be deleted] → intended; the message says why. Renaming is the way to fix such a group.
- [Two people rename at the same time] → last write wins; no concurrency token for a name.

## Migration Plan

No schema change. Backend and frontend ship together in one pull request.

## Open Questions

- None.
