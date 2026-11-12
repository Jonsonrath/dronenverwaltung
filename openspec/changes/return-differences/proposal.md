# Proposal

## Why

Change request 5 of the assignment: groups bring back fewer batteries or propellers than they took, and today nobody notices – a return just closes the loan. The return should record what came back, and the history should show what is missing.

Part of the feature backlog planned on 12.11.2026 (one OpenSpec change per feature). Builds on the changes `improve-frontend-design` and `manage-student-groups` (pull requests #71, #73).

## What Changes

- **Record returned accessories (REQ-022):** when a Loan is returned, the number of returned batteries and propellers can be entered. Without numbers, everything counts as returned. Each number is between 0 and the loaned quantity.
- **History:** returned Loans show the returned quantities and, if something is missing, how much.
- **UI:** *Zurückgeben* opens a sheet with two steppers prefilled with the loaned quantities; the history marks missing accessories.

### Out of Scope

- Tracking which group still owes accessories after the return, reminders
- Individual batteries (change request 4)
- Damage of the drone at the return (change `damage-report-on-return`)

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `loan`: new requirement "Record returned accessories (REQ-022)".

## Impact

- **Backend:** `LoanItem.ReturnedQuantity` (nullable int, migration `AddReturnedQuantities`), `Loan.Return` takes the returned quantities, `POST /api/loans/{id}/return` accepts an optional body; loan responses get `returnedBatteries` and `returnedPropellers`.
- **Frontend:** return sheet on the active loans page, history columns.
- **Rules:** "return differences" leaves the out-of-scope list in AGENTS.md and `openspec/config.yaml`.
