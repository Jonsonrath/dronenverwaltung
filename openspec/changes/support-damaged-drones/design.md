# Design

## Context

The drone status (Available | Loaned) is derived from the active Loan and never stored (AGENTS.md). Damage is different: it is a fact somebody reports, so it is stored – but the *status* stays derived from both facts.

## Decisions

### 1. Data
`Drone.DamageReason` (string, nullable). `null` = not damaged. No boolean next to it, so the two can never disagree. Migration `AddDroneDamage`.

### 2. Status derivation (one place, `DroneStatus`)
`Loaned` if the Drone has an active Loan; else `Damaged` if `DamageReason` is set; else `Available`. The API also returns `damageReason`, so a loaned damaged drone shows both facts.

### 3. API
- `POST /api/drones/{id}/damage` with `{ "reason": "…" }` → 200 with the drone; 404 unknown, 422 empty reason. Reporting again replaces the reason.
- `DELETE /api/drones/{id}/damage` → 200 with the drone; 404 unknown, 409 not damaged.
- `POST /api/loans` for a damaged drone → 409 "Die Drohne „…“ ist defekt: <reason>".

### 4. Loan check
`LoanService.CreateAsync` checks damage before the active-loan check. Order of the answers: unknown drone/group 404, damaged 409, already loaned 409.

### 5. Frontend
*Defekt* does not use red – red means a backend error. It gets a neutral muted pill with a crossed glyph; the bay shows the reason. The loan form lists damaged drones in their own collapsed group like loaned ones.

## Risks / Trade-offs

- [Two sources for one status] → avoided by storing only the reason and deriving the status in one function.
- [A damaged drone that is loaned shows "Ausgeliehen"] → the reason is shown next to it, and after the return it is Damaged.
