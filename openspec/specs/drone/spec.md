# drone Specification

## Purpose
Keeps the inventory of drones that can be loaned to student groups and shows for each Drone whether it is currently available or loaned.

## Requirements

### Requirement: Create drone (REQ-001)
The system SHALL allow creating a Drone with a name and an optional free-text description. Each Drone SHALL receive a unique id. The name SHALL NOT be empty or consist only of whitespace and SHALL be unique among all Drones, ignoring upper/lower case (including letters such as umlauts) and leading/trailing whitespace. The availability of a Drone is never entered; a newly created Drone is available.

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

#### Scenario: Duplicate name rejected
- **GIVEN** a Drone named "TELLO-07" exists
- **WHEN** a Drone is created with name "tello-07"
- **THEN** the request is rejected as a conflict
- **AND** no second Drone is created

#### Scenario: Duplicate name with umlauts rejected
- **GIVEN** a Drone named "Übungsdrohne" exists
- **WHEN** a Drone is created with name "ÜBUNGSDROHNE"
- **THEN** the request is rejected as a conflict

### Requirement: List drones with availability (REQ-002)
The system SHALL list all Drones with id, name, description, status and, for a damaged Drone, the damage reason. The status SHALL be Loaned when the Drone has an active Loan, Damaged when it has no active Loan and is damaged, and Available otherwise. The status SHALL be derived from the Loans and the damage of the Drone and never set directly.

#### Scenario: Show status of drones
- **GIVEN** "TELLO-02" has an active Loan
- **AND** "TELLO-07" has no active Loan and is not damaged
- **WHEN** the Drone list is requested
- **THEN** "TELLO-02" is listed with status Loaned
- **AND** "TELLO-07" is listed with status Available

#### Scenario: Drone with only returned loans is available
- **GIVEN** "TELLO-07" had a Loan that has been returned
- **AND** "TELLO-07" has no active Loan and is not damaged
- **WHEN** the Drone list is requested
- **THEN** "TELLO-07" is listed with status Available

#### Scenario: Damaged drone
- **GIVEN** "TELLO-11" has no active Loan and is damaged with the reason "Propeller gebrochen"
- **WHEN** the Drone list is requested
- **THEN** "TELLO-11" is listed with status Damaged and the damage reason "Propeller gebrochen"

#### Scenario: Loaned drone that is damaged
- **GIVEN** "TELLO-02" has an active Loan and is damaged with the reason "Kamera ausgefallen"
- **WHEN** the Drone list is requested
- **THEN** "TELLO-02" is listed with status Loaned and the damage reason "Kamera ausgefallen"

#### Scenario: No drones
- **GIVEN** no Drone exists
- **WHEN** the Drone list is requested
- **THEN** an empty list is returned

### Requirement: Edit drone (REQ-013)
The system SHALL allow changing the name and the description of an existing Drone. The new name SHALL NOT be empty or consist only of whitespace and SHALL be unique among all other Drones, ignoring upper/lower case (including letters such as umlauts) and leading/trailing whitespace. A missing description SHALL be stored as empty. Loans of the Drone SHALL stay assigned to it.

#### Scenario: Rename a drone
- **GIVEN** a Drone named "TELO-07" exists
- **WHEN** the Drone is renamed to "TELLO-07"
- **THEN** the Drone keeps its id and has the name "TELLO-07"

#### Scenario: Change the description
- **GIVEN** the Drone "TELLO-07" has the description "rechter Propellerschutz beschädigt"
- **WHEN** its description is changed to "Propellerschutz erneuert"
- **THEN** the Drone has the description "Propellerschutz erneuert" and keeps its name

#### Scenario: Loans show the new name
- **GIVEN** the Drone "TELO-07" has an active Loan
- **WHEN** the Drone is renamed to "TELLO-07"
- **THEN** the active Loan is listed with the Drone name "TELLO-07"
- **AND** the Drone is still Loaned

#### Scenario: Empty name rejected
- **GIVEN** a Drone named "TELLO-07" exists
- **WHEN** the Drone is renamed to an empty or whitespace-only name
- **THEN** the request is rejected as invalid input
- **AND** the Drone keeps the name "TELLO-07"

#### Scenario: Name of another drone rejected
- **GIVEN** the Drones "TELLO-02" and "TELLO-07" exist
- **WHEN** "TELLO-07" is renamed to " tello-02 "
- **THEN** the request is rejected as a conflict
- **AND** the Drone keeps the name "TELLO-07"

#### Scenario: Changing only the spelling of the own name
- **GIVEN** a Drone named "tello-07" exists
- **WHEN** the Drone is renamed to "TELLO-07"
- **THEN** the Drone has the name "TELLO-07"

#### Scenario: Unknown drone
- **WHEN** a Drone that does not exist is edited
- **THEN** the request is rejected as not found

### Requirement: Report drone damage (REQ-014)
The system SHALL allow marking an existing Drone as damaged with a reason. The reason SHALL NOT be empty or consist only of whitespace. Reporting damage for a Drone that is already damaged SHALL replace the reason. A Drone with an active Loan MAY be marked as damaged; its Loan stays unchanged.

#### Scenario: Mark an available drone as damaged
- **GIVEN** the Drone "TELLO-07" is Available
- **WHEN** "TELLO-07" is marked as damaged with the reason "Propeller gebrochen"
- **THEN** "TELLO-07" has status Damaged and the damage reason "Propeller gebrochen"

#### Scenario: Mark a loaned drone as damaged
- **GIVEN** "TELLO-07" has an active Loan for "Group 3"
- **WHEN** "TELLO-07" is marked as damaged with the reason "Kamera ausgefallen"
- **THEN** "TELLO-07" has status Loaned and the damage reason "Kamera ausgefallen"
- **AND** the active Loan for "Group 3" is unchanged

#### Scenario: Damaged drone after its return
- **GIVEN** "TELLO-07" has an active Loan and is marked as damaged
- **WHEN** the Loan is returned
- **THEN** "TELLO-07" has status Damaged

#### Scenario: New reason replaces the old one
- **GIVEN** "TELLO-07" is damaged with the reason "Propeller gebrochen"
- **WHEN** "TELLO-07" is marked as damaged with the reason "Akkufach locker"
- **THEN** "TELLO-07" has the damage reason "Akkufach locker"

#### Scenario: Empty reason rejected
- **GIVEN** the Drone "TELLO-07" is Available
- **WHEN** "TELLO-07" is marked as damaged with an empty or whitespace-only reason
- **THEN** the request is rejected as invalid input
- **AND** "TELLO-07" stays Available

#### Scenario: Unknown drone
- **WHEN** a Drone that does not exist is marked as damaged
- **THEN** the request is rejected as not found

### Requirement: Repair drone (REQ-015)
The system SHALL allow marking a damaged Drone as repaired. The damage reason SHALL be removed. Repairing a Drone that is not damaged SHALL be rejected as a conflict.

#### Scenario: Repair a damaged drone
- **GIVEN** "TELLO-07" is damaged and has no active Loan
- **WHEN** "TELLO-07" is marked as repaired
- **THEN** "TELLO-07" is Available and has no damage reason

#### Scenario: Repair a loaned damaged drone
- **GIVEN** "TELLO-07" is damaged and has an active Loan
- **WHEN** "TELLO-07" is marked as repaired
- **THEN** "TELLO-07" is Loaned and has no damage reason

#### Scenario: Drone not damaged
- **GIVEN** "TELLO-07" is not damaged
- **WHEN** "TELLO-07" is marked as repaired
- **THEN** the request is rejected as a conflict

#### Scenario: Unknown drone
- **WHEN** a Drone that does not exist is marked as repaired
- **THEN** the request is rejected as not found

### Requirement: Drone SSID (REQ-024)
The system SHALL allow setting an optional SSID for a Drone when it is created or edited, and SHALL list the SSID with each Drone. Leading and trailing whitespace SHALL be removed; an empty SSID SHALL count as no SSID. A set SSID SHALL be unique among all Drones, ignoring upper/lower case.

#### Scenario: Create a drone with SSID
- **WHEN** a Drone is created with name "TELLO-07" and SSID " TELLO-9FA21B "
- **THEN** the Drone is listed with the SSID "TELLO-9FA21B"

#### Scenario: Drone without SSID
- **WHEN** a Drone is created with name "TELLO-08" and no or an empty SSID
- **THEN** the Drone is listed without SSID

#### Scenario: Several drones without SSID
- **GIVEN** the Drone "TELLO-08" has no SSID
- **WHEN** a Drone "TELLO-09" is created without SSID
- **THEN** both Drones are listed without SSID

#### Scenario: SSID of another drone rejected
- **GIVEN** the Drone "TELLO-07" has the SSID "TELLO-9FA21B"
- **WHEN** a Drone is created or another Drone is edited with the SSID "tello-9fa21b"
- **THEN** the request is rejected as a conflict
- **AND** no Drone gets the SSID a second time

#### Scenario: Change the SSID
- **GIVEN** the Drone "TELLO-07" has no SSID
- **WHEN** "TELLO-07" is edited with the SSID "TELLO-9FA21B"
- **THEN** "TELLO-07" is listed with the SSID "TELLO-9FA21B"
