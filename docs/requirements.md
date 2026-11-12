# Requirements – Drone Lending System (Drohnenverwaltung)

> Status: **Reviewed** – team decisions recorded below (04.11.2026).
> Input for the OpenSpec change `initial-drone-lending-system`.

## Problem

Drones and accessories are loaned to student groups. Today this is tracked on a sheet of paper on the cabinet door.
The application shall answer:

- Which drones exist and which are currently available?
- Which group has loaned which drone?
- How many batteries and propellers were handed out?
- When was something loaned, when was it returned?
- Which loans existed in the past?

## Domain Terms

| Term | Meaning |
|---|---|
| `Drone` | An aircraft that can be loaned. Id, Name, Description, DamageReason (empty = not damaged). |
| `StudentGroup` | A group of students that loans and is responsible. Id, Name. |
| `Loan` | A loan of one drone to one student group. LoanDate, ReturnDate, Status (Active \| Returned). |
| `LoanItem` | An accessory position of a loan. Type (Battery \| Propeller), Quantity. |

A drone is **Loaned** if it has an active loan, otherwise **Damaged** if a damage is reported, otherwise **Available**. This status is derived, never entered.

## Requirements

### REQ-001 Create drone
The system SHALL allow creating a drone with a name and an optional free-text description.

**Scenario: Create a drone**
- GIVEN no drone named "TELLO-07" exists
- WHEN a drone is created with name "TELLO-07" and description "SSID: TELLO-9FA21B, rechter Propellerschutz beschädigt"
- THEN the drone is stored with a unique id
- AND the drone is available

**Scenario: Name is required**
- WHEN a drone is created with an empty name
- THEN the drone is rejected

**Scenario: Name is unique**
- GIVEN a drone named "TELLO-07" exists
- WHEN a drone is created with name "tello-07"
- THEN the drone is rejected

### REQ-002 List drones
The system SHALL list all drones with name, status (Available | Loaned | Damaged), description and, for a damaged drone, the damage reason. *Modified by the OpenSpec change `support-damaged-drones` (12.11.2026).*

**Scenario: Show status of drones**
- GIVEN "TELLO-02" has an active loan
- AND "TELLO-07" has no active loan and is not damaged
- WHEN the drone list is shown
- THEN "TELLO-02" is shown as Loaned
- AND "TELLO-07" is shown as Available

**Scenario: Damaged drone**
- GIVEN "TELLO-11" has no active loan and is damaged with the reason "Propeller gebrochen"
- WHEN the drone list is shown
- THEN "TELLO-11" is shown as Damaged with the reason "Propeller gebrochen"

**Scenario: Loaned drone that is damaged**
- GIVEN "TELLO-02" has an active loan and is damaged with the reason "Kamera ausgefallen"
- WHEN the drone list is shown
- THEN "TELLO-02" is shown as Loaned with the reason "Kamera ausgefallen"

### REQ-003 Create student group
The system SHALL allow creating a student group with a name.

**Scenario: Create a student group**
- WHEN a student group is created with name "Group 3"
- THEN the student group is stored with a unique id

**Scenario: Name is required**
- WHEN a student group is created with an empty name
- THEN the student group is rejected

**Scenario: Name is unique**
- GIVEN a student group named "Group 3" exists
- WHEN a student group is created with name "group 3"
- THEN the student group is rejected

### REQ-004 Loan drone
An available and undamaged drone SHALL be loaned to an existing student group. The loan date SHALL be set by the system. A loan of a damaged drone SHALL be rejected as a conflict. *Modified by the OpenSpec change `support-damaged-drones` (12.11.2026).*

**Scenario: Loan an available drone**
- GIVEN the drone "TELLO-07" is available
- AND the student group "Group 3" exists
- WHEN "TELLO-07" is loaned to "Group 3"
- THEN an active loan is created with the current date as LoanDate
- AND "TELLO-07" is Loaned

**Scenario: Damaged drone cannot be loaned**
- GIVEN the drone "TELLO-07" is damaged and has no active loan
- WHEN "TELLO-07" is loaned to "Group 3"
- THEN the loan is rejected as a conflict
- AND "TELLO-07" stays Damaged

**Scenario: Repaired drone can be loaned**
- GIVEN the drone "TELLO-07" was damaged and has been repaired
- WHEN "TELLO-07" is loaned to "Group 3"
- THEN an active loan is created

**Scenario: Unknown drone or group**
- WHEN a loan is requested for a drone or student group that does not exist
- THEN the loan is rejected

### REQ-005 No double loan
A drone SHALL have at most one active loan.

**Scenario: Loaned drone cannot be loaned again**
- GIVEN "TELLO-07" already has an active loan
- WHEN another group tries to loan it
- THEN the loan is rejected
- AND the existing loan is unchanged

### REQ-006 Loan accessories
A loan SHALL record quantities of batteries and propellers. Allowed types are Battery and Propeller. Quantities SHALL be whole numbers >= 0. Accessories are tracked as quantities, not as individual items.

**Scenario: Loan with accessories**
- GIVEN "TELLO-07" is available and "Group 3" exists
- WHEN "TELLO-07" is loaned to "Group 3" with 4 batteries and 8 propellers
- THEN the loan contains 4 batteries and 8 propellers

**Scenario: Negative quantity rejected**
- WHEN a loan is requested with -1 batteries
- THEN the loan is rejected
- AND no loan is created

### REQ-007 Show active loans
The system SHALL list all active loans with group, drone, number of batteries, number of propellers and loan date.

**Scenario: Show active loans**
- GIVEN "Group 1" has loaned "TELLO-02" with 3 batteries and 4 propellers on 19.08.2026
- AND "Group 3" has loaned "TELLO-07" with 4 batteries and 8 propellers on 19.08.2026
- WHEN the active loans are shown
- THEN both loans are listed with these values
- AND returned loans are not listed

### REQ-008 Return loan
An active loan SHALL be returnable. Returning sets the loan from Active to Returned and makes the drone available again.

**Scenario: Return a loan**
- GIVEN "TELLO-07" is loaned to "Group 3"
- WHEN the loan is returned
- THEN the loan is marked as Returned
- AND "TELLO-07" becomes available again

**Scenario: Loan already returned**
- GIVEN the loan is already returned
- WHEN it is returned again
- THEN the return is rejected

### REQ-009 Store return date
When a loan is returned, the system SHALL store the return date.

**Scenario: Return date is stored**
- GIVEN an active loan without a ReturnDate
- WHEN the loan is returned
- THEN the ReturnDate is set to the current date

### REQ-010 Show loan history
Returned loans SHALL never be deleted. The system SHALL list past (returned) loans with group, drone, accessories, loan date and return date.

**Scenario: Returned loan stays in history**
- GIVEN "TELLO-07" was loaned to "Group 3" and returned
- WHEN the loan history is shown
- THEN the loan is listed with its LoanDate and ReturnDate

### REQ-011 Rename student group
*Added with the OpenSpec change `manage-student-groups` (11.11.2026).* The system SHALL allow changing the name of a student group. The same rules as for a new group apply (not empty, unique). Loans stay assigned to the group.

**Scenario: Rename a student group**
- GIVEN a student group "Grupe 3" exists
- WHEN it is renamed to "Group 3"
- THEN it keeps its id and its loans and has the name "Group 3"

**Scenario: Name of another group**
- GIVEN the student groups "Group 1" and "Group 3" exist
- WHEN "Group 3" is renamed to "group 1"
- THEN the request is rejected as a conflict

### REQ-012 Delete student group
*Added with the OpenSpec change `manage-student-groups` (11.11.2026).* The system SHALL allow deleting a student group that has no loan. A group with an active or returned loan SHALL NOT be deleted, so that the history stays complete.

**Scenario: Group without loans**
- GIVEN a student group "Group 86" without any loan exists
- WHEN it is deleted
- THEN it is no longer listed

**Scenario: Group with a loan**
- GIVEN "Group 3" has an active or returned loan
- WHEN it is deleted
- THEN the request is rejected as a conflict and the loan stays

### REQ-013 Edit drone
*Added with the OpenSpec change `edit-drones` (12.11.2026).* The system SHALL allow changing the name and the description of a drone. The same rules as for a new drone apply (not empty, unique). Loans stay assigned to the drone.

**Scenario: Rename a drone**
- GIVEN a drone "TELO-07" exists
- WHEN it is renamed to "TELLO-07"
- THEN it keeps its id and its loans and has the name "TELLO-07"

**Scenario: Name of another drone**
- GIVEN the drones "TELLO-02" and "TELLO-07" exist
- WHEN "TELLO-07" is renamed to "tello-02"
- THEN the request is rejected as a conflict

### REQ-014 Report drone damage
*Added with the OpenSpec change `support-damaged-drones` (12.11.2026), change request 1.* The system SHALL allow marking a drone as damaged with a reason. The reason SHALL NOT be empty. Reporting again SHALL replace the reason. A loaned drone MAY be marked as damaged; its loan stays unchanged.

**Scenario: Mark an available drone as damaged**
- GIVEN "TELLO-07" is Available
- WHEN it is marked as damaged with the reason "Propeller gebrochen"
- THEN it is Damaged with the reason "Propeller gebrochen"

**Scenario: Mark a loaned drone as damaged**
- GIVEN "TELLO-07" has an active loan for "Group 3"
- WHEN it is marked as damaged with the reason "Kamera ausgefallen"
- THEN it stays Loaned with the reason "Kamera ausgefallen" and the loan is unchanged
- AND after the return it is Damaged

**Scenario: Empty reason**
- WHEN a drone is marked as damaged with an empty reason
- THEN the request is rejected as invalid input

### REQ-015 Repair drone
*Added with the OpenSpec change `support-damaged-drones` (12.11.2026), change request 1.* The system SHALL allow marking a damaged drone as repaired; the reason SHALL be removed. Repairing a drone that is not damaged SHALL be rejected as a conflict.

**Scenario: Repair a damaged drone**
- GIVEN "TELLO-07" is damaged and has no active loan
- WHEN it is marked as repaired
- THEN it is Available and has no damage reason

**Scenario: Drone not damaged**
- GIVEN "TELLO-07" is not damaged
- WHEN it is marked as repaired
- THEN the request is rejected as a conflict

### REQ-022 Record returned accessories
*Added with the OpenSpec change `return-differences` (12.11.2026, change request 5).* When a Loan is returned, the system SHALL record the number of returned batteries and returned propellers. A number that is not given SHALL count as the loaned quantity. Each returned number SHALL be a whole number from 0 to the loaned quantity; otherwise the return SHALL be rejected as invalid input and the Loan SHALL stay active. The loan history SHALL list the returned quantities and the missing quantities (loaned minus returned).

**Scenario: Complete return**
- GIVEN "TELLO-07" is loaned to "Group 3" with 4 batteries and 8 propellers
- WHEN the Loan is returned without returned quantities
- THEN the Loan is listed in the history with 4 of 4 batteries and 8 of 8 propellers returned and nothing missing

**Scenario: Missing battery**
- GIVEN "TELLO-07" is loaned to "Group 3" with 4 batteries and 8 propellers
- WHEN the Loan is returned with 3 batteries and 8 propellers
- THEN the Loan is listed in the history with 1 battery missing and no propeller missing

**Scenario: More returned than loaned**
- GIVEN "TELLO-07" is loaned to "Group 3" with 4 batteries
- WHEN the Loan is returned with 5 batteries
- THEN the request is rejected as invalid input
- AND the Loan stays active

**Scenario: Negative returned quantity**
- GIVEN "TELLO-07" is loaned to "Group 3" with 8 propellers
- WHEN the Loan is returned with -1 propellers
- THEN the request is rejected as invalid input
- AND the Loan stays active

Loans returned before this change have no returned quantities recorded and count as complete.

## Planned Requirements (draft, specs in review)

*Feature backlog of 12.11.2026.* REQ-013 – REQ-015 and REQ-022 are implemented (see above). Each line is its own OpenSpec change under `openspec/changes/`; the full requirement with SHALL and scenarios is in that change's delta spec. When a change is implemented, its requirements move into the list above.

| ID | Requirement | OpenSpec change | Assignment |
|---|---|---|---|
| REQ-016 | Add student to group | `student-group-members` | change request 3 |
| REQ-017 | List group members | `student-group-members` | change request 3 |
| REQ-018 | Move student | `student-group-members` | change request 3 |
| REQ-019 | Remove student | `student-group-members` | change request 3 |
| REQ-020 | Set responsible student | `responsible-student` | change request 2 |
| REQ-021 | Responsible student on loans | `responsible-student` | change request 2 |
| REQ-023 | Report damage on return | `damage-report-on-return` | change request 6 |
| REQ-024 | Drone SSID | `drone-ssid` | change request 7 |

Modified by these changes: REQ-012 (groups with students cannot be deleted). (REQ-002 and REQ-004 are already modified for damaged drones, see above.)

## Out of Scope

- Login, user accounts, roles
- Individual battery inventory (BAT-001 ...)
- Reservations in advance
- Statistics, reports, exports
- Editing or deleting drones (student groups: see REQ-011, REQ-012)

These follow later as separate change requests.

## Team Decisions

- Drone names and student group names must be unique (ignoring upper/lower case, also for umlauts, and surrounding spaces).
- A loan with 0 batteries and 0 propellers is allowed (quantities >= 0).
- LoanDate and ReturnDate are stored as date and time and shown as date.
