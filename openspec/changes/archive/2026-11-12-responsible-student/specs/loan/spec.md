# Spec Delta

## ADDED Requirements

### Requirement: Responsible student on loans (REQ-021)
When a Loan is created, the system SHALL store the name of the StudentGroup's responsible Student at that time with the Loan. Active Loans and the loan history SHALL show this name. Later changes of the responsible Student, of the Student or of the StudentGroup SHALL NOT change the name stored with an existing Loan. A StudentGroup without a responsible Student SHALL still be able to loan a Drone; the Loan then has no responsible Student.

#### Scenario: Loan stores the responsible student
- **GIVEN** "Anna Berger" is responsible for "Group 3"
- **WHEN** "TELLO-07" is loaned to "Group 3"
- **THEN** the active Loan is listed with the responsible Student "Anna Berger"

#### Scenario: Group without responsible student
- **GIVEN** "Group 86" has no responsible Student
- **WHEN** "TELLO-02" is loaned to "Group 86"
- **THEN** an active Loan is created without a responsible Student

#### Scenario: History keeps the name at loan time
- **GIVEN** "TELLO-07" was loaned to "Group 3" while "Anna Berger" was responsible
- **AND** "Ben Huber" has become responsible and "Anna Berger" has been removed
- **WHEN** the Loan is returned and the loan history is requested
- **THEN** the Loan is listed with the responsible Student "Anna Berger"
