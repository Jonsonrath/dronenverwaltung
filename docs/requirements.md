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
| `Drone` | An aircraft that can be loaned. Id, Name, Description. |
| `StudentGroup` | A group of students that loans and is responsible. Id, Name. |
| `Loan` | A loan of one drone to one student group. LoanDate, ReturnDate, Status (Active \| Returned). |
| `LoanItem` | An accessory position of a loan. Type (Battery \| Propeller), Quantity. |

A drone is **Available** if it has no active loan, otherwise **Loaned**. This availability is derived, never entered.

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
The system SHALL list all drones with name, status (Available | Loaned) and description.

**Scenario: Show status of drones**
- GIVEN "TELLO-02" has an active loan
- AND "TELLO-07" has no active loan
- WHEN the drone list is shown
- THEN "TELLO-02" is shown as Loaned
- AND "TELLO-07" is shown as Available

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
An available drone SHALL be loaned to an existing student group. The loan date SHALL be set by the system.

**Scenario: Loan an available drone**
- GIVEN the drone "TELLO-07" is available
- AND the student group "Group 3" exists
- WHEN "TELLO-07" is loaned to "Group 3"
- THEN an active loan is created with the current date as LoanDate
- AND "TELLO-07" is Loaned

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

## Out of Scope

- Login, user accounts, roles
- Individual battery inventory (BAT-001 ...)
- Reservations in advance
- Damage management and return differences
- Statistics, reports, exports
- Editing or deleting drones and student groups

These follow later as separate change requests.

## Team Decisions

- Drone names and student group names must be unique (ignoring upper/lower case and surrounding spaces).
- A loan with 0 batteries and 0 propellers is allowed (quantities >= 0).
- LoanDate and ReturnDate are stored as date and time and shown as date.
