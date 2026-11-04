# Spec Delta

## Purpose

Keeps the inventory of drones that can be loaned to student groups and shows for each Drone whether it is currently available or loaned.

## ADDED Requirements

### Requirement: Create drone (REQ-001)
The system SHALL allow creating a Drone with a name and an optional free-text description. Each Drone SHALL receive a unique id. The name SHALL NOT be empty or consist only of whitespace. The availability of a Drone is never entered; a newly created Drone is available.

#### Scenario: Create a drone
- **GIVEN** no Drone named "TELLO-07" exists
- **WHEN** a Drone is created with name "TELLO-07" and description "SSID: TELLO-9FA21B, rechter Propellerschutz beschädigt"
- **THEN** the Drone is stored with a unique id, the name and the description
- **AND** the Drone is Available

#### Scenario: Create a drone without description
- **WHEN** a Drone is created with name "TELLO-08" and no description
- **THEN** the Drone is stored with an empty description

#### Scenario: Empty name rejected
- **WHEN** a Drone is created with an empty or whitespace-only name
- **THEN** the request is rejected as invalid input
- **AND** no Drone is created

### Requirement: List drones with availability (REQ-002)
The system SHALL list all Drones with id, name, description and status. The status SHALL be Available when the Drone has no active Loan and Loaned when it has an active Loan. The status SHALL be derived from the Loans and never set directly.

#### Scenario: Show status of drones
- **GIVEN** "TELLO-02" has an active Loan
- **AND** "TELLO-07" has no active Loan
- **WHEN** the Drone list is requested
- **THEN** "TELLO-02" is listed with status Loaned
- **AND** "TELLO-07" is listed with status Available

#### Scenario: Drone with only returned loans is available
- **GIVEN** "TELLO-07" had a Loan that has been returned
- **AND** "TELLO-07" has no active Loan
- **WHEN** the Drone list is requested
- **THEN** "TELLO-07" is listed with status Available

#### Scenario: No drones
- **GIVEN** no Drone exists
- **WHEN** the Drone list is requested
- **THEN** an empty list is returned
