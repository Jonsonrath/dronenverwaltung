# Proposal

## Why

Change request 7 of the assignment: to connect to a Tello drone you need its Wi-Fi SSID. Today it is hidden in the free-text description ("SSID: TELLO-9FA21B, rechter Propellerschutz beschädigt"), cannot be searched and is not checked for duplicates.

Part of the feature backlog planned on 12.11.2026 (one OpenSpec change per feature). Builds on the changes `improve-frontend-design` and `manage-student-groups` (pull requests #71, #73). Requires `edit-drones` for changing the SSID of an existing drone.

## What Changes

- **SSID as its own field (REQ-024):** a Drone has an optional SSID, set when creating or editing. If set, it is unique among all Drones (ignoring case and surrounding whitespace). The Drone list shows it.
- **UI:** an SSID field in *Drohne hinzufügen* and *Bearbeiten*; the drone bay shows the SSID under the name.

### Out of Scope

- Moving existing SSIDs out of the descriptions automatically – done by hand with *Bearbeiten*
- Connecting to the drone, Wi-Fi passwords

## Capabilities

### New Capabilities
<!-- none -->

### Modified Capabilities
- `drone`: new requirement "Drone SSID (REQ-024)".

## Impact

- **Backend:** `Drone.Ssid` / `NormalizedSsid` with a filtered unique index (only when set), migration `AddDroneSsid`; `ssid` in create, edit and list.
- **Frontend:** drone model, both forms, bay.
