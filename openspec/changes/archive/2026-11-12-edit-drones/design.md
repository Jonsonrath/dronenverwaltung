# Design

## Context

`Drone` has Id, Name, NormalizedName (unique index) and Description; only `POST` and `GET /api/drones` exist. The pattern of `manage-student-groups` (rename a group) fits here as well.

## Decisions

### 1. API
`PUT /api/drones/{id}` with `{ "name": "…", "description": "…" }` → 200 with the drone (incl. derived status); 404 unknown drone, 422 blank name, 409 name of another drone. A missing description is stored as empty, as when creating.

### 2. Rules in the domain
`Drone.Edit(name, description)` reuses `EntityName.Require` / `Normalize`. Uniqueness is checked against *other* drones, so changing only the spelling of the own name is allowed; the unique index stays the last guard (unique violation → 409).

### 3. Editing a loaned drone
Allowed: the name is master data, not loan state. Loans reference the drone by id and show the current name.

### 4. Frontend
*Bearbeiten* (quiet button) next to *Ausleihen* / *Zurückgeben* in each bay; the sheet is the same `<dialog>` pattern as *Drohne hinzufügen*.

## Risks / Trade-offs

- [The history shows the new name, not the name at loan time] → accepted; a drone is the same physical device. A name history is out of scope.
