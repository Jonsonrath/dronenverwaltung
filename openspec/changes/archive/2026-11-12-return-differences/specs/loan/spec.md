# Spec Delta

## ADDED Requirements

### Requirement: Record returned accessories (REQ-022)
When a Loan is returned, the system SHALL record the number of returned batteries and returned propellers. A number that is not given SHALL count as the loaned quantity. Each returned number SHALL be a whole number from 0 to the loaned quantity; otherwise the return SHALL be rejected as invalid input and the Loan SHALL stay active. The loan history SHALL list the returned quantities and the missing quantities (loaned minus returned).

#### Scenario: Complete return
- **GIVEN** "TELLO-07" is loaned to "Group 3" with 4 batteries and 8 propellers
- **WHEN** the Loan is returned without returned quantities
- **THEN** the Loan is listed in the history with 4 of 4 batteries and 8 of 8 propellers returned and nothing missing

#### Scenario: Missing battery
- **GIVEN** "TELLO-07" is loaned to "Group 3" with 4 batteries and 8 propellers
- **WHEN** the Loan is returned with 3 batteries and 8 propellers
- **THEN** the Loan is listed in the history with 1 battery missing and no propeller missing

#### Scenario: More returned than loaned
- **GIVEN** "TELLO-07" is loaned to "Group 3" with 4 batteries
- **WHEN** the Loan is returned with 5 batteries
- **THEN** the request is rejected as invalid input
- **AND** the Loan stays active

#### Scenario: Negative returned quantity
- **GIVEN** "TELLO-07" is loaned to "Group 3" with 8 propellers
- **WHEN** the Loan is returned with -1 propellers
- **THEN** the request is rejected as invalid input
- **AND** the Loan stays active
