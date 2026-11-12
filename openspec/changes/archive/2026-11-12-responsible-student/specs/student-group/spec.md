# Spec Delta

## ADDED Requirements

### Requirement: Set responsible student (REQ-020)
The system SHALL allow marking one Student of a StudentGroup as its responsible Student, changing it to another Student of the same group, and clearing it. A StudentGroup SHALL have at most one responsible Student. Only a Student of the StudentGroup SHALL be marked as its responsible Student. When the responsible Student is moved to another group or removed, the StudentGroup SHALL have no responsible Student.

#### Scenario: Mark a responsible student
- **GIVEN** "Anna Berger" and "Ben Huber" are Students of "Group 3"
- **WHEN** "Anna Berger" is marked as responsible for "Group 3"
- **THEN** "Group 3" is listed with the responsible Student "Anna Berger"

#### Scenario: Change the responsible student
- **GIVEN** "Anna Berger" is responsible for "Group 3"
- **WHEN** "Ben Huber" is marked as responsible for "Group 3"
- **THEN** "Ben Huber" is the only responsible Student of "Group 3"

#### Scenario: Clear the responsible student
- **GIVEN** "Anna Berger" is responsible for "Group 3"
- **WHEN** the responsible Student of "Group 3" is cleared
- **THEN** "Group 3" has no responsible Student

#### Scenario: Student of another group rejected
- **GIVEN** "Clara Wolf" is a Student of "Group 1"
- **WHEN** "Clara Wolf" is marked as responsible for "Group 3"
- **THEN** the request is rejected as invalid input
- **AND** the responsible Student of "Group 3" is unchanged

#### Scenario: Responsible student moved or removed
- **GIVEN** "Anna Berger" is responsible for "Group 3"
- **WHEN** "Anna Berger" is moved to "Group 1" or removed
- **THEN** "Group 3" has no responsible Student

#### Scenario: Unknown student group or student
- **WHEN** a responsible Student is set for a StudentGroup that does not exist, or with a Student that does not exist
- **THEN** the request is rejected as not found
