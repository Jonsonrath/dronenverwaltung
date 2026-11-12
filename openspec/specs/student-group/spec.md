# student-group Specification

## Purpose
Keeps the student groups that loan drones and are responsible for them, so that every Loan can be assigned to a StudentGroup.

## Requirements

### Requirement: Create student group (REQ-003)
The system SHALL allow creating a StudentGroup with a name. Each StudentGroup SHALL receive a unique id. The name SHALL NOT be empty or consist only of whitespace and SHALL be unique among all StudentGroups, ignoring upper/lower case (including letters such as umlauts) and leading/trailing whitespace.

#### Scenario: Create a student group
- **WHEN** a StudentGroup is created with name "Group 3"
- **THEN** the StudentGroup is stored with a unique id and the name "Group 3"

#### Scenario: Empty name rejected
- **WHEN** a StudentGroup is created with an empty or whitespace-only name
- **THEN** the request is rejected as invalid input
- **AND** no StudentGroup is created

#### Scenario: Duplicate name rejected
- **GIVEN** a StudentGroup named "Group 3" exists
- **WHEN** a StudentGroup is created with name " group 3 "
- **THEN** the request is rejected as a conflict
- **AND** no second StudentGroup is created

### Requirement: List student groups (REQ-003)
The system SHALL list all StudentGroups with id and name, so that a StudentGroup can be selected when a Drone is loaned.

#### Scenario: List student groups
- **GIVEN** the StudentGroups "Group 1" and "Group 3" exist
- **WHEN** the StudentGroup list is requested
- **THEN** both StudentGroups are listed with id and name

#### Scenario: No student groups
- **GIVEN** no StudentGroup exists
- **WHEN** the StudentGroup list is requested
- **THEN** an empty list is returned

### Requirement: Rename student group (REQ-011)
The system SHALL allow changing the name of an existing StudentGroup. The new name SHALL NOT be empty or consist only of whitespace and SHALL be unique among all other StudentGroups, ignoring upper/lower case (including letters such as umlauts) and leading/trailing whitespace. Loans of the StudentGroup SHALL stay assigned to it.

#### Scenario: Rename a student group
- **GIVEN** a StudentGroup named "Grupe 3" exists
- **WHEN** the StudentGroup is renamed to "Group 3"
- **THEN** the StudentGroup keeps its id and has the name "Group 3"

#### Scenario: Loans show the new name
- **GIVEN** the StudentGroup "Grupe 3" has an active Loan
- **WHEN** the StudentGroup is renamed to "Group 3"
- **THEN** the active Loan is listed with the StudentGroup name "Group 3"

#### Scenario: Empty name rejected
- **GIVEN** a StudentGroup named "Group 3" exists
- **WHEN** the StudentGroup is renamed to an empty or whitespace-only name
- **THEN** the request is rejected as invalid input
- **AND** the StudentGroup keeps the name "Group 3"

#### Scenario: Name of another group rejected
- **GIVEN** the StudentGroups "Group 1" and "Group 3" exist
- **WHEN** "Group 3" is renamed to " group 1 "
- **THEN** the request is rejected as a conflict
- **AND** the StudentGroup keeps the name "Group 3"

#### Scenario: Changing only the spelling of the own name
- **GIVEN** a StudentGroup named "group 3" exists
- **WHEN** the StudentGroup is renamed to "Group 3"
- **THEN** the StudentGroup has the name "Group 3"

#### Scenario: Unknown student group
- **WHEN** a StudentGroup that does not exist is renamed
- **THEN** the request is rejected as not found

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
