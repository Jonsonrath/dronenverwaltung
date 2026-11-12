# Spec Delta

## MODIFIED Requirements

### Requirement: Loan drone (REQ-004)
The system SHALL allow loaning an available and undamaged Drone to an existing StudentGroup. The new Loan SHALL have status Active, the current date and time as LoanDate set by the system, and no ReturnDate. Afterwards the Drone SHALL be Loaned. A Loan request for a damaged Drone SHALL be rejected as a conflict.

#### Scenario: Loan an available drone
- **GIVEN** the Drone "TELLO-07" is Available
- **AND** the StudentGroup "Group 3" exists
- **WHEN** "TELLO-07" is loaned to "Group 3"
- **THEN** an active Loan for "TELLO-07" and "Group 3" is created with the current date as LoanDate and no ReturnDate
- **AND** "TELLO-07" is Loaned

#### Scenario: Damaged drone cannot be loaned
- **GIVEN** the Drone "TELLO-07" is damaged and has no active Loan
- **AND** the StudentGroup "Group 3" exists
- **WHEN** "TELLO-07" is loaned to "Group 3"
- **THEN** the request is rejected as a conflict
- **AND** no Loan is created
- **AND** "TELLO-07" stays Damaged

#### Scenario: Repaired drone can be loaned
- **GIVEN** the Drone "TELLO-07" was damaged and has been repaired
- **WHEN** "TELLO-07" is loaned to "Group 3"
- **THEN** an active Loan for "TELLO-07" and "Group 3" is created

#### Scenario: Unknown drone rejected
- **GIVEN** the StudentGroup "Group 3" exists
- **WHEN** a Loan is requested for a Drone that does not exist
- **THEN** the request is rejected as not found
- **AND** no Loan is created

#### Scenario: Unknown student group rejected
- **GIVEN** the Drone "TELLO-07" is Available
- **WHEN** a Loan is requested for a StudentGroup that does not exist
- **THEN** the request is rejected as not found
- **AND** no Loan is created
- **AND** "TELLO-07" stays Available
