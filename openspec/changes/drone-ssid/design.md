# Design

## Decisions

### 1. Data
`Drone.Ssid` (nullable text, trimmed) and `NormalizedSsid` (upper case) with a unique index filtered on `NormalizedSsid IS NOT NULL`, so many drones can have no SSID. Empty or whitespace input is stored as `null`.

### 2. Rules
Same pattern as the drone name: the domain normalises, the service checks other drones, the unique index is the last guard (unique violation → 409). No format check – real SSIDs differ between drone models.

### 3. API
`ssid` (optional) in `POST /api/drones` and `PUT /api/drones/{id}`, and in every drone response.

### 4. Frontend
Field *SSID (optional)* in both sheets; the bay shows "SSID TELLO-9FA21B" under the name.
