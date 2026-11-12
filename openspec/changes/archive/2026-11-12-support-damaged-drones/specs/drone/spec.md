# Spec Delta

## ADDED Requirements

### Requirement: Report drone damage (REQ-014)
The system SHALL allow marking an existing Drone as damaged with a reason. The reason SHALL NOT be empty or consist only of whitespace. Reporting damage for a Drone that is already damaged SHALL replace the reason. A Drone with an active Loan MAY be marked as damaged; its Loan stays unchanged.

#### Scenario: Mark an available drone as damaged
- **GIVEN** the Drone "TELLO-07" is Available
- **WHEN** "TELLO-07" is marked as damaged with the reason "Propeller gebrochen"
- **THEN** "TELLO-07" has status Damaged and the damage reason "Propeller gebrochen"

#### Scenario: Mark a loaned drone as damaged
- **GIVEN** "TELLO-07" has an active Loan for "Group 3"
- **WHEN** "TELLO-07" is marked as damaged with the reason "Kamera ausgefallen"
- **THEN** "TELLO-07" has status Loaned and the damage reason "Kamera ausgefallen"
- **AND** the active Loan for "Group 3" is unchanged

#### Scenario: Damaged drone after its return
- **GIVEN** "TELLO-07" has an active Loan and is marked as damaged
- **WHEN** the Loan is returned
- **THEN** "TELLO-07" has status Damaged

#### Scenario: New reason replaces the old one
- **GIVEN** "TELLO-07" is damaged with the reason "Propeller gebrochen"
- **WHEN** "TELLO-07" is marked as damaged with the reason "Akkufach locker"
- **THEN** "TELLO-07" has the damage reason "Akkufach locker"

#### Scenario: Empty reason rejected
- **GIVEN** the Drone "TELLO-07" is Available
- **WHEN** "TELLO-07" is marked as damaged with an empty or whitespace-only reason
- **THEN** the request is rejected as invalid input
- **AND** "TELLO-07" stays Available

#### Scenario: Unknown drone
- **WHEN** a Drone that does not exist is marked as damaged
- **THEN** the request is rejected as not found

### Requirement: Repair drone (REQ-015)
The system SHALL allow marking a damaged Drone as repaired. The damage reason SHALL be removed. Repairing a Drone that is not damaged SHALL be rejected as a conflict.

#### Scenario: Repair a damaged drone
- **GIVEN** "TELLO-07" is damaged and has no active Loan
- **WHEN** "TELLO-07" is marked as repaired
- **THEN** "TELLO-07" is Available and has no damage reason

#### Scenario: Repair a loaned damaged drone
- **GIVEN** "TELLO-07" is damaged and has an active Loan
- **WHEN** "TELLO-07" is marked as repaired
- **THEN** "TELLO-07" is Loaned and has no damage reason

#### Scenario: Drone not damaged
- **GIVEN** "TELLO-07" is not damaged
- **WHEN** "TELLO-07" is marked as repaired
- **THEN** the request is rejected as a conflict

#### Scenario: Unknown drone
- **WHEN** a Drone that does not exist is marked as repaired
- **THEN** the request is rejected as not found

## MODIFIED Requirements

### Requirement: List drones with availability (REQ-002)
The system SHALL list all Drones with id, name, description, status and, for a damaged Drone, the damage reason. The status SHALL be Loaned when the Drone has an active Loan, Damaged when it has no active Loan and is damaged, and Available otherwise. The status SHALL be derived from the Loans and the damage of the Drone and never set directly.

#### Scenario: Show status of drones
- **GIVEN** "TELLO-02" has an active Loan
- **AND** "TELLO-07" has no active Loan and is not damaged
- **WHEN** the Drone list is requested
- **THEN** "TELLO-02" is listed with status Loaned
- **AND** "TELLO-07" is listed with status Available

#### Scenario: Drone with only returned loans is available
- **GIVEN** "TELLO-07" had a Loan that has been returned
- **AND** "TELLO-07" has no active Loan and is not damaged
- **WHEN** the Drone list is requested
- **THEN** "TELLO-07" is listed with status Available

#### Scenario: Damaged drone
- **GIVEN** "TELLO-11" has no active Loan and is damaged with the reason "Propeller gebrochen"
- **WHEN** the Drone list is requested
- **THEN** "TELLO-11" is listed with status Damaged and the damage reason "Propeller gebrochen"

#### Scenario: Loaned drone that is damaged
- **GIVEN** "TELLO-02" has an active Loan and is damaged with the reason "Kamera ausgefallen"
- **WHEN** the Drone list is requested
- **THEN** "TELLO-02" is listed with status Loaned and the damage reason "Kamera ausgefallen"

#### Scenario: No drones
- **GIVEN** no Drone exists
- **WHEN** the Drone list is requested
- **THEN** an empty list is returned
