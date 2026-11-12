# Proposal

## Why

Change request 2 of the assignment: when a drone comes back damaged or incomplete, the teacher needs one person to talk to, not a whole group. Each group gets a responsible student, and every loan remembers who was responsible when it was handed out.

Part of the feature backlog planned on 12.11.2026 (one OpenSpec change per feature). Builds on the changes `improve-frontend-design` and `manage-student-groups` (pull requests #71, #73). Requires `student-group-members` (students must exist first).

## What Changes

- **Set the responsible student (REQ-020):** one Student of the group can be marked as responsible; it can be changed or cleared. Moving or removing that Student clears it.
- **Responsible student on loans (REQ-021):** a new Loan stores the name of the group's responsible Student at loan time. Active loans and the history show it. Later changes to the group do not change existing Loans.
- **UI:** a mark on the responsible student on the group plate with *Verantwortlich machen*; the loan form shows who will be responsible; active loans and history get a column.

### Out of Scope

- More than one responsible student per group
- Making the responsible student mandatory for a loan (a group without one can still loan)

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `student-group`: new requirement "Set responsible student (REQ-020)".
- `loan`: new requirement "Responsible student on loans (REQ-021)".

## Impact

- **Backend:** `StudentGroup.ResponsibleStudentId` (nullable, foreign key to Students, Set Null), `Loan.ResponsibleStudentName` (nullable text), migration `AddResponsibleStudent`; `PUT` / `DELETE /api/student-groups/{id}/responsible`; loan responses get `responsibleStudentName`.
- **Frontend:** group plate, loan form summary, active loans and history tables.
