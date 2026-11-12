# Design

## Decisions

### 1. Where it is stored
- `StudentGroup.ResponsibleStudentId` – nullable foreign key to `Students` with *Set Null*: removing the student clears it in the database too.
- `Loan.ResponsibleStudentName` – a copy of the name at loan time. A copy, not a foreign key: the history must keep the name even after the student moved or was removed (returned loans are never changed).

### 2. API
- `PUT /api/student-groups/{id}/responsible` `{ studentId }` → 200; 404 group or student, 422 the student is not a member of the group.
- `DELETE /api/student-groups/{id}/responsible` → 200 (also when none was set).
- Group list: `responsibleStudentId`. Loan list and responses: `responsibleStudentName` (null if none).

### 3. Moving a student
`StudentService.MoveAsync` clears the responsibility of the old group when the moved student was responsible (same transaction).

### 4. Frontend
A small "verantwortlich" tag next to the name on the plate; *Verantwortlich machen* per student. The loan summary bar reads "TELLO-07 für Group 3 (verantwortlich: Anna Berger)". Active loans and history get the column *Verantwortlich*.

## Risks / Trade-offs

- [The copied name gets outdated] → intended: it documents who was responsible at that time.
