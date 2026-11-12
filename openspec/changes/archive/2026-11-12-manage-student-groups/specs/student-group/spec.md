# Spec Delta

## ADDED Requirements

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
The system SHALL allow deleting a StudentGroup that has no Loan. A StudentGroup that has at least one Loan, active or returned, SHALL NOT be deleted, so that no Loan and no part of the loan history is lost.

#### Scenario: Delete a student group without loans
- **GIVEN** a StudentGroup "Group 86" without any Loan exists
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

#### Scenario: Unknown student group
- **WHEN** a StudentGroup that does not exist is deleted
- **THEN** the request is rejected as not found
