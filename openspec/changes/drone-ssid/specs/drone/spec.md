# Spec Delta

## ADDED Requirements

### Requirement: Drone SSID (REQ-024)
The system SHALL allow setting an optional SSID for a Drone when it is created or edited, and SHALL list the SSID with each Drone. Leading and trailing whitespace SHALL be removed; an empty SSID SHALL count as no SSID. A set SSID SHALL be unique among all Drones, ignoring upper/lower case.

#### Scenario: Create a drone with SSID
- **WHEN** a Drone is created with name "TELLO-07" and SSID " TELLO-9FA21B "
- **THEN** the Drone is listed with the SSID "TELLO-9FA21B"

#### Scenario: Drone without SSID
- **WHEN** a Drone is created with name "TELLO-08" and no or an empty SSID
- **THEN** the Drone is listed without SSID

#### Scenario: Several drones without SSID
- **GIVEN** the Drone "TELLO-08" has no SSID
- **WHEN** a Drone "TELLO-09" is created without SSID
- **THEN** both Drones are listed without SSID

#### Scenario: SSID of another drone rejected
- **GIVEN** the Drone "TELLO-07" has the SSID "TELLO-9FA21B"
- **WHEN** a Drone is created or another Drone is edited with the SSID "tello-9fa21b"
- **THEN** the request is rejected as a conflict
- **AND** no Drone gets the SSID a second time

#### Scenario: Change the SSID
- **GIVEN** the Drone "TELLO-07" has no SSID
- **WHEN** "TELLO-07" is edited with the SSID "TELLO-9FA21B"
- **THEN** "TELLO-07" is listed with the SSID "TELLO-9FA21B"
