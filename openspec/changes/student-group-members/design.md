# Design

## Context

`StudentGroup` (Id, Name, NormalizedName). `manage-student-groups` added rename and delete (delete only without loans). The ubiquitous language has no term for a single student yet.

## Decisions

### 1. Model
`Student` (Id, Name, NormalizedName, Class, StudentGroupId) – an entity of its own, not part of the StudentGroup aggregate, because it is moved between groups. Unique index (StudentGroupId, NormalizedName). Class is free text, optional ("4AHIF"). Foreign key to `StudentGroups` with Restrict.

### 2. API
| Request | Success | Errors |
|---|---|---|
| `GET /api/student-groups` | each group now has `students: [{ id, name, class }]` | – |
| `POST /api/student-groups/{groupId}/students` `{ name, class }` | 201 | 404 group, 422 blank name, 409 name already in the group |
| `PUT /api/students/{id}/group` `{ studentGroupId }` | 200 | 404 student or group, 409 name already in the target group |
| `DELETE /api/students/{id}` | 204 | 404 |

The members are part of the group list, so the page needs no extra request per group.

### 3. Deleting a group (MODIFIED REQ-012)
`StudentGroupService.DeleteAsync` checks loans first (409 as before), then students (409 "Die Gruppe hat noch Schüler"). The foreign key is the last guard, mapped with `IsForeignKeyViolation`.

### 4. Frontend
Members as a short list on each group plate (name, class); *Schüler hinzufügen* per group; *Verschieben* (group chips) and *Entfernen* per student in a sheet.

## Risks / Trade-offs

- [Same name in two groups] → allowed on purpose; uniqueness only within a group.
- [Deleting a group needs extra steps] → intended; no student disappears silently.
