# Design

## Decisions

### 1. Data and transaction
`Loan.DamageReport` (nullable text). `LoanService.ReturnAsync` sets `Loan.DamageReport` and calls `Drone.ReportDamage(report)` in the same `SaveChanges`, so loan and drone never disagree. A report that is empty or only whitespace counts as no report (no error – the field is optional).

### 2. API
`POST /api/loans/{id}/return` body gets `damageReport` (optional, next to the returned quantities of `return-differences`). Loan responses get `damageReport`.

### 3. Already damaged drone
If the drone was already damaged (reported during the loan), a damage report on return replaces the reason, as REQ-014 says for every new report.

### 4. Frontend
An optional text field *Schaden (optional)* in the return sheet; after a return with a report the confirmation says "TELLO-07 zurückgegeben und als defekt markiert". The history shows the report in the row.
