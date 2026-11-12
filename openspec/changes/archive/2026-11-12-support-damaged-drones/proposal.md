# Proposal

## Why

Change request 1 of the assignment: a damaged drone must not be loaned. Today a broken drone is Available and can be handed out; the damage is at best a line in the description that nobody checks.

Part of the feature backlog planned on 12.11.2026 (one OpenSpec change per feature). Builds on the changes `improve-frontend-design` and `manage-student-groups` (pull requests #71, #73).

## What Changes

- **Report damage (REQ-014):** a Drone can be marked as damaged with a short reason ("Propeller gebrochen"). A loaned Drone can be marked as well; it stays loaned and is damaged after the return.
- **Repair (REQ-015):** a damaged Drone can be marked as repaired; the reason is removed.
- **Status (MODIFIED REQ-002):** a third status *Damaged* – Loaned while there is an active Loan, otherwise Damaged if damaged, otherwise Available. The damage reason is listed with the Drone.
- **Loan (MODIFIED REQ-004):** only available and undamaged Drones can be loaned; a damaged Drone is rejected as a conflict.
- **UI:** *Defekt melden* / *Repariert* on each drone bay, status word *Defekt* with its own look; the loan form lists damaged drones apart (still selectable, the backend decides).

### Out of Scope

- A damage history (who reported what when) – only the current damage is kept
- Damage reported during the return (change `damage-report-on-return`)
- Damage of batteries or propellers

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `drone`: new requirements "Report drone damage (REQ-014)", "Repair drone (REQ-015)"; modified "List drones with availability (REQ-002)".
- `loan`: modified "Loan drone (REQ-004)".

## Impact

- **Backend:** `Drone.DamageReason` (nullable column, migration `AddDroneDamage`), `Drone.ReportDamage` / `Repair`, status derivation, `POST /api/drones/{id}/damage` and `DELETE /api/drones/{id}/damage`, check in `LoanService.CreateAsync`.
- **Frontend:** drone model (`status: 'Available' | 'Loaned' | 'Damaged'`, `damageReason`), drones page, loan form.
- **Rules:** AGENTS.md and `openspec/config.yaml` get the damage rule; "damage management" leaves the out-of-scope list.
