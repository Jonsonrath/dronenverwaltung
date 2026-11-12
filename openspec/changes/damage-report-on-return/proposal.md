# Proposal

## Why

Change request 6 of the assignment: most damage is noticed when a drone comes back. Reporting it in the return itself means the damage is linked to the loan (who had the drone) and the drone is blocked at once.

Part of the feature backlog planned on 12.11.2026 (one OpenSpec change per feature). Builds on the changes `improve-frontend-design` and `manage-student-groups` (pull requests #71, #73). Requires `support-damaged-drones` (damaged drones exist) and is easiest after `return-differences` (the return sheet exists).

## What Changes

- **Report damage on return (REQ-023):** a return can carry a damage report. The Loan keeps the report, and the Drone becomes damaged with the report as its reason – it cannot be loaned until it is repaired.
- **History:** returned Loans show their damage report.
- **UI:** an optional field *Schaden* in the return sheet; the history marks loans with a damage report.

### Out of Scope

- Photos, cost, who pays
- Damage of accessories

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `loan`: new requirement "Report damage on return (REQ-023)".

## Impact

- **Backend:** `Loan.DamageReport` (nullable text, migration `AddLoanDamageReport`), return body field `damageReport`, the return sets `Drone.DamageReason` in the same transaction.
- **Frontend:** return sheet field, history column.
