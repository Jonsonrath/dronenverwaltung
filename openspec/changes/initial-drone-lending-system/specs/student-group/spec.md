# Spec Delta

## Purpose

Keeps the student groups that loan drones and are responsible for them, so that every Loan can be assigned to a StudentGroup.

## ADDED Requirements

### Requirement: Create student group (REQ-003)
The system SHALL allow creating a StudentGroup with a name. Each StudentGroup SHALL receive a unique id. The name SHALL NOT be empty or consist only of whitespace.

#### Scenario: Create a student group
- **WHEN** a StudentGroup is created with name "Group 3"
- **THEN** the StudentGroup is stored with a unique id and the name "Group 3"

#### Scenario: Empty name rejected
- **WHEN** a StudentGroup is created with an empty or whitespace-only name
- **THEN** the request is rejected as invalid input
- **AND** no StudentGroup is created

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
