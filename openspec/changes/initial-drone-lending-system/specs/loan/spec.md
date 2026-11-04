# Spec Delta

## Purpose

Records which StudentGroup has loaned which Drone together with how many batteries and propellers, when it was loaned and returned, and keeps every Loan as history.

## ADDED Requirements

### Requirement: Loan drone (REQ-004)
The system SHALL allow loaning an available Drone to an existing StudentGroup. The new Loan SHALL have status Active, the current date and time as LoanDate set by the system, and no ReturnDate. Afterwards the Drone SHALL be Loaned.

#### Scenario: Loan an available drone
- **GIVEN** the Drone "TELLO-07" is Available
- **AND** the StudentGroup "Group 3" exists
- **WHEN** "TELLO-07" is loaned to "Group 3"
- **THEN** an active Loan for "TELLO-07" and "Group 3" is created with the current date as LoanDate and no ReturnDate
- **AND** "TELLO-07" is Loaned

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

### Requirement: No double loan (REQ-005)
A Drone SHALL have at most one active Loan. A Loan request for a Drone that already has an active Loan SHALL be rejected as a conflict, and the existing Loan SHALL stay unchanged.

#### Scenario: Loaned drone cannot be loaned again
- **GIVEN** "TELLO-07" has an active Loan for "Group 3"
- **AND** the StudentGroup "Group 1" exists
- **WHEN** "TELLO-07" is loaned to "Group 1"
- **THEN** the request is rejected as a conflict
- **AND** no new Loan is created
- **AND** the active Loan for "Group 3" is unchanged

#### Scenario: Simultaneous loan requests for the same drone
- **GIVEN** "TELLO-07" is Available
- **AND** the StudentGroups "Group 1" and "Group 3" exist
- **WHEN** "TELLO-07" is loaned to "Group 1" and to "Group 3" at the same time
- **THEN** exactly one request succeeds and creates an active Loan
- **AND** the other request is rejected as a conflict

#### Scenario: Drone can be loaned again after return
- **GIVEN** "TELLO-07" had a Loan for "Group 3" that has been returned
- **WHEN** "TELLO-07" is loaned to "Group 1"
- **THEN** a new active Loan for "Group 1" is created

### Requirement: Loan accessories (REQ-006)
A Loan SHALL record the number of batteries and the number of propellers as LoanItems of type Battery and Propeller. Quantities SHALL be whole numbers greater than or equal to 0; a missing quantity counts as 0. Accessories are quantities only, never individual items with their own id.

#### Scenario: Loan with accessories
- **GIVEN** "TELLO-07" is Available and "Group 3" exists
- **WHEN** "TELLO-07" is loaned to "Group 3" with 4 batteries and 8 propellers
- **THEN** the Loan contains 4 batteries and 8 propellers

#### Scenario: Loan without accessories
- **GIVEN** "TELLO-07" is Available and "Group 3" exists
- **WHEN** "TELLO-07" is loaned to "Group 3" without batteries and propellers
- **THEN** the Loan contains 0 batteries and 0 propellers

#### Scenario: Negative quantity rejected
- **GIVEN** "TELLO-07" is Available and "Group 3" exists
- **WHEN** "TELLO-07" is loaned to "Group 3" with -1 batteries
- **THEN** the request is rejected as invalid input
- **AND** no Loan is created
- **AND** "TELLO-07" stays Available

### Requirement: Show active loans (REQ-007)
The system SHALL list all active Loans with Loan id, StudentGroup name, Drone name, number of batteries, number of propellers and LoanDate. Returned Loans SHALL NOT be listed.

#### Scenario: Show active loans
- **GIVEN** "Group 1" has loaned "TELLO-02" with 3 batteries and 4 propellers on 19.08.2026
- **AND** "Group 3" has loaned "TELLO-07" with 4 batteries and 8 propellers on 19.08.2026
- **WHEN** the active Loans are requested
- **THEN** both Loans are listed with group, drone, batteries, propellers and LoanDate as given

#### Scenario: Returned loans are not active
- **GIVEN** the Loan of "TELLO-07" has been returned
- **WHEN** the active Loans are requested
- **THEN** the Loan of "TELLO-07" is not listed

### Requirement: Return loan (REQ-008)
The system SHALL allow returning an active Loan. Returning SHALL change the Loan status from Active to Returned, and the Drone SHALL become Available again. Returning a Loan that is already returned SHALL be rejected as a conflict; returning a Loan that does not exist SHALL be rejected as not found.

#### Scenario: Return a loan
- **GIVEN** "TELLO-07" is loaned to "Group 3"
- **WHEN** the Loan is returned
- **THEN** the Loan has status Returned
- **AND** "TELLO-07" is Available again

#### Scenario: Loan already returned
- **GIVEN** the Loan of "TELLO-07" has been returned
- **WHEN** the same Loan is returned again
- **THEN** the request is rejected as a conflict
- **AND** the Loan and its ReturnDate are unchanged

#### Scenario: Unknown loan
- **WHEN** a Loan that does not exist is returned
- **THEN** the request is rejected as not found

### Requirement: Store return date (REQ-009)
When a Loan is returned, the system SHALL store the current date and time as ReturnDate. LoanDate, StudentGroup, Drone and accessory quantities SHALL stay unchanged.

#### Scenario: Return date is stored
- **GIVEN** an active Loan of "TELLO-07" with 4 batteries and 8 propellers and no ReturnDate
- **WHEN** the Loan is returned
- **THEN** the ReturnDate is set to the current date
- **AND** LoanDate, StudentGroup, Drone, 4 batteries and 8 propellers are unchanged

### Requirement: Loan history (REQ-010)
Returned Loans SHALL never be deleted. The system SHALL list all returned Loans with StudentGroup name, Drone name, number of batteries, number of propellers, LoanDate and ReturnDate. Loans SHALL be persistent and still be available after the application is restarted.

#### Scenario: Returned loan stays in history
- **GIVEN** "TELLO-07" was loaned to "Group 3" and returned
- **WHEN** the loan history is requested
- **THEN** the Loan is listed with "Group 3", "TELLO-07", its accessories, LoanDate and ReturnDate

#### Scenario: Active loans are not in history
- **GIVEN** "TELLO-02" has an active Loan
- **WHEN** the loan history is requested
- **THEN** the Loan of "TELLO-02" is not listed

#### Scenario: Loans survive a restart
- **GIVEN** a Loan was created and returned
- **WHEN** the application is stopped and started again
- **THEN** the Loan is still listed in the loan history
