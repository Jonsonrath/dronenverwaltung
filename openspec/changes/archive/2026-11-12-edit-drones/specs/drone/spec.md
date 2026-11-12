# Spec Delta

## ADDED Requirements

### Requirement: Edit drone (REQ-013)
The system SHALL allow changing the name and the description of an existing Drone. The new name SHALL NOT be empty or consist only of whitespace and SHALL be unique among all other Drones, ignoring upper/lower case (including letters such as umlauts) and leading/trailing whitespace. A missing description SHALL be stored as empty. Loans of the Drone SHALL stay assigned to it.

#### Scenario: Rename a drone
- **GIVEN** a Drone named "TELO-07" exists
- **WHEN** the Drone is renamed to "TELLO-07"
- **THEN** the Drone keeps its id and has the name "TELLO-07"

#### Scenario: Change the description
- **GIVEN** the Drone "TELLO-07" has the description "rechter Propellerschutz beschädigt"
- **WHEN** its description is changed to "Propellerschutz erneuert"
- **THEN** the Drone has the description "Propellerschutz erneuert" and keeps its name

#### Scenario: Loans show the new name
- **GIVEN** the Drone "TELO-07" has an active Loan
- **WHEN** the Drone is renamed to "TELLO-07"
- **THEN** the active Loan is listed with the Drone name "TELLO-07"
- **AND** the Drone is still Loaned

#### Scenario: Empty name rejected
- **GIVEN** a Drone named "TELLO-07" exists
- **WHEN** the Drone is renamed to an empty or whitespace-only name
- **THEN** the request is rejected as invalid input
- **AND** the Drone keeps the name "TELLO-07"

#### Scenario: Name of another drone rejected
- **GIVEN** the Drones "TELLO-02" and "TELLO-07" exist
- **WHEN** "TELLO-07" is renamed to " tello-02 "
- **THEN** the request is rejected as a conflict
- **AND** the Drone keeps the name "TELLO-07"

#### Scenario: Changing only the spelling of the own name
- **GIVEN** a Drone named "tello-07" exists
- **WHEN** the Drone is renamed to "TELLO-07"
- **THEN** the Drone has the name "TELLO-07"

#### Scenario: Unknown drone
- **WHEN** a Drone that does not exist is edited
- **THEN** the request is rejected as not found
