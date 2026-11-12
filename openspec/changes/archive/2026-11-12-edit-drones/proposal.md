# Proposal

## Why

A drone can only be created. A typo in its name or an outdated description (a new SSID, a repaired propeller guard) stays forever, and the description is the only place for such notes. The team asked to rename drones and to change their description.

Part of the feature backlog planned on 12.11.2026 (one OpenSpec change per feature). Builds on the changes `improve-frontend-design` and `manage-student-groups` (pull requests #71, #73).

## What Changes

- **Edit a drone (REQ-013):** name and description of an existing Drone can be changed. The name follows the same rules as when creating (not empty, unique ignoring case and surrounding whitespace). Loans keep pointing to the Drone, so active loans and the history show the new name.
- **UI:** *Bearbeiten* on every drone bay opens a sheet with name and description prefilled; the backend's answers (404, 409, 422) are shown there.

### Out of Scope

- Deleting drones
- A separate SSID field (change `drone-ssid`)
- Damage state (change `support-damaged-drones`)

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `drone`: new requirement "Edit drone (REQ-013)".

## Impact

- **Backend:** `Drone.Edit`, `DroneService.UpdateAsync`, `PUT /api/drones/{id}`; no schema change.
- **Frontend:** `DroneApi.update`, drones page (button and sheet).
- **Docs:** `docs/requirements.md` (REQ-013), README table.
