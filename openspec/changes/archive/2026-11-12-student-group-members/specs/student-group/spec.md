# Spec Delta

## ADDED Requirements

### Requirement: Add student to group (REQ-016)
The system SHALL allow adding a Student with a name and an optional class to an existing StudentGroup. Each Student SHALL receive a unique id and belong to exactly one StudentGroup. The name SHALL NOT be empty or consist only of whitespace and SHALL be unique within the StudentGroup, ignoring upper/lower case (including letters such as umlauts) and leading/trailing whitespace.

#### Scenario: Add a student
- **GIVEN** the StudentGroup "Group 3" exists
- **WHEN** the Student "Anna Berger" with class "4AHIF" is added to "Group 3"
- **THEN** the Student is stored with a unique id, the name, the class and the group "Group 3"

#### Scenario: Add a student without class
- **GIVEN** the StudentGroup "Group 3" exists
- **WHEN** the Student "Ben Huber" is added to "Group 3" without a class
- **THEN** the Student is stored with an empty class

#### Scenario: Empty name rejected
- **GIVEN** the StudentGroup "Group 3" exists
- **WHEN** a Student with an empty or whitespace-only name is added to "Group 3"
- **THEN** the request is rejected as invalid input
- **AND** no Student is created

#### Scenario: Name already in the group
- **GIVEN** "Group 3" has the Student "Anna Berger"
- **WHEN** the Student " anna berger " is added to "Group 3"
- **THEN** the request is rejected as a conflict

#### Scenario: Same name in another group
- **GIVEN** "Group 3" has the Student "Anna Berger"
- **AND** the StudentGroup "Group 1" exists
- **WHEN** the Student "Anna Berger" is added to "Group 1"
- **THEN** the Student is stored in "Group 1"

#### Scenario: Unknown student group
- **WHEN** a Student is added to a StudentGroup that does not exist
- **THEN** the request is rejected as not found

### Requirement: List group members (REQ-017)
The system SHALL list the Students of every StudentGroup with id, name and class, ordered by name, as part of the StudentGroup list.

#### Scenario: Group with students
- **GIVEN** "Group 3" has the Students "Ben Huber" and "Anna Berger"
- **WHEN** the StudentGroup list is requested
- **THEN** "Group 3" is listed with "Anna Berger" and "Ben Huber" in this order

#### Scenario: Group without students
- **GIVEN** the StudentGroup "Group 86" has no Students
- **WHEN** the StudentGroup list is requested
- **THEN** "Group 86" is listed with an empty list of Students

### Requirement: Move student (REQ-018)
The system SHALL allow moving a Student to another existing StudentGroup. The name SHALL stay unique within the target StudentGroup.

#### Scenario: Move a student
- **GIVEN** "Anna Berger" is a Student of "Group 3"
- **AND** the StudentGroup "Group 1" exists
- **WHEN** "Anna Berger" is moved to "Group 1"
- **THEN** "Anna Berger" is a Student of "Group 1" and no longer of "Group 3"

#### Scenario: Name already in the target group
- **GIVEN** "Anna Berger" is a Student of "Group 3"
- **AND** "Group 1" already has a Student "Anna Berger"
- **WHEN** "Anna Berger" of "Group 3" is moved to "Group 1"
- **THEN** the request is rejected as a conflict
- **AND** the Student stays in "Group 3"

#### Scenario: Unknown student or group
- **WHEN** a Student that does not exist is moved, or a Student is moved to a StudentGroup that does not exist
- **THEN** the request is rejected as not found

### Requirement: Remove student (REQ-019)
The system SHALL allow removing a Student. Loans are not affected.

#### Scenario: Remove a student
- **GIVEN** "Ben Huber" is a Student of "Group 3"
- **WHEN** "Ben Huber" is removed
- **THEN** "Group 3" is listed without "Ben Huber"

#### Scenario: Unknown student
- **WHEN** a Student that does not exist is removed
- **THEN** the request is rejected as not found

## MODIFIED Requirements

### Requirement: Delete student group (REQ-012)
The system SHALL allow deleting a StudentGroup that has no Loan and no Student. A StudentGroup that has at least one Loan, active or returned, SHALL NOT be deleted, so that no Loan and no part of the loan history is lost. A StudentGroup that still has Students SHALL NOT be deleted; they have to be moved or removed first.

#### Scenario: Delete a student group without loans
- **GIVEN** a StudentGroup "Group 86" without any Loan and without Students exists
- **WHEN** the StudentGroup is deleted
- **THEN** the StudentGroup is no longer listed

#### Scenario: Student group with an active loan
- **GIVEN** the StudentGroup "Group 3" has an active Loan
- **WHEN** the StudentGroup is deleted
- **THEN** the request is rejected as a conflict
- **AND** the StudentGroup and its Loan still exist

#### Scenario: Student group with a returned loan
- **GIVEN** the StudentGroup "Group 3" has only a returned Loan
- **WHEN** the StudentGroup is deleted
- **THEN** the request is rejected as a conflict
- **AND** the returned Loan is still in the loan history

#### Scenario: Student group with students
- **GIVEN** the StudentGroup "Group 86" has no Loan and the Student "Ben Huber"
- **WHEN** the StudentGroup is deleted
- **THEN** the request is rejected as a conflict
- **AND** "Group 86" and "Ben Huber" still exist

#### Scenario: Unknown student group
- **WHEN** a StudentGroup that does not exist is deleted
- **THEN** the request is rejected as not found
