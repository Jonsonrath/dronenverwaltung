# Design

## Decisions

### 1. Data
`LoanItem.ReturnedQuantity` (int, nullable): `null` while the Loan is active, set at the return. Missing = Quantity − ReturnedQuantity, derived, not stored.

### 2. API
`POST /api/loans/{id}/return` with an optional body `{ "batteries": 3, "propellers": 8 }`. Without a body or a field, the loaned quantity is taken. A value below 0 or above the loaned quantity → 422 and the Loan stays active. The existing return rules (409 already returned, 404 unknown, concurrency token) stay unchanged.

### 3. Rule in the domain
`Loan.Return(returnDate, returnedBatteries, returnedPropellers)` validates the numbers before changing the status, so an invalid return changes nothing.

### 4. Frontend
The *Zurückgeben* button opens a sheet with two `QuantityStepper`s, prefilled with the loaned quantities and limited to them for convenience (the backend still decides). The history shows "3 von 4" and a mark when something is missing.

## Risks / Trade-offs

- [Old returned loans have no returned quantities] → shown as complete (null = not recorded, treated as nothing missing); the migration leaves them null.
