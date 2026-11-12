# Spec Delta

## ADDED Requirements

### Requirement: Report damage on return (REQ-023)
When a Loan is returned, the system SHALL accept an optional damage report. If a report is given, the Loan SHALL keep it and the Drone SHALL be damaged with the report as its damage reason. A report that is empty or consists only of whitespace SHALL count as no report. The loan history SHALL list the damage report of each returned Loan.

#### Scenario: Return with damage report
- **GIVEN** "TELLO-07" is loaned to "Group 3"
- **WHEN** the Loan is returned with the damage report "Propeller gebrochen"
- **THEN** the Loan is listed in the history with the damage report "Propeller gebrochen"
- **AND** "TELLO-07" has status Damaged with the damage reason "Propeller gebrochen"

#### Scenario: Return without damage report
- **GIVEN** "TELLO-07" is loaned to "Group 3" and is not damaged
- **WHEN** the Loan is returned without a damage report
- **THEN** the Loan has no damage report
- **AND** "TELLO-07" is Available

#### Scenario: Whitespace-only report
- **GIVEN** "TELLO-07" is loaned to "Group 3" and is not damaged
- **WHEN** the Loan is returned with the damage report "   "
- **THEN** the Loan has no damage report
- **AND** "TELLO-07" is Available

#### Scenario: Damaged drone cannot be loaned after the return
- **GIVEN** the Loan of "TELLO-07" was returned with the damage report "Propeller gebrochen"
- **WHEN** "TELLO-07" is loaned to "Group 1"
- **THEN** the request is rejected as a conflict
