# 🚁 Drone Inventory – Drohnenverwaltung

Webanwendung zur Verwaltung von Drohnen und Zubehör, die an Schülergruppen ausgeliehen werden.
Sie ersetzt den Zettel an der Schranktür – nachvollziehbar und dauerhaft.

> Einstiegsprojekt im Unterrichtsfach **Systemplanung und Projektentwicklung (SYP)**, 4. INF, 2026/27.
> Vorbereitung auf das zweite Semester (Tello-Drohne, Scrum).

---

## ✨ Funktionen

| Bereich | Beschreibung |
|---|---|
| **Verwalten** | Drohnen und Schülergruppen anlegen und anzeigen |
| **Ausleihen** | Drohne inklusive Akkus und Propellern an eine Gruppe ausgeben |
| **Zurücknehmen** | Ausleihe abschließen, Drohne wieder freigeben |
| **Nachvollziehen** | Aktive Ausleihen anzeigen, Historie dauerhaft aufbewahren |

## 🧱 Architektur

```
Angular Frontend   (frontend/drone-inventory-ui)
      │  REST / JSON
      ▼
ASP.NET Core Web API   (backend/DroneInventory.Api)
      │
      ▼
Entity Framework Core
      │
      ▼
SQLite
```

- Das Frontend spricht ausschließlich über die REST-API mit dem Backend.
- Geschäftslogik und Validierung liegen **im Backend**.
- Das Frontend greift nie direkt auf die Datenbank zu.

## 🛠️ Tech-Stack

- **Backend:** C#, ASP.NET Core 10 Web API, Entity Framework Core, SQLite
- **Frontend:** Angular, TypeScript
- **Werkzeuge:** VS Code, Git/GitHub, GitHub CLI (`gh`), OpenSpec, KI-Coding-Agent

## 📚 Domänenmodell (Ubiquitous Language)

| Begriff | Bedeutung |
|---|---|
| `Drone` | Fluggerät, das ausgeliehen werden kann (Id, Name, Description) |
| `StudentGroup` | Schülergruppe, die ausleiht und verantwortlich ist (Id, Name) |
| `Loan` | Ausleihvorgang mit Gruppe, Drohne, `LoanDate`, `ReturnDate`, Status |
| `LoanItem` | Zubehörposition einer Ausleihe: Typ (`Battery` \| `Propeller`) und Menge |

### Domain Rules

- Eine Drohne hat höchstens eine aktive Ausleihe.
- Nur verfügbare Drohnen dürfen ausgeliehen werden.
- Eine Rückgabe macht die Drohne wieder verfügbar.
- Zubehörmengen sind nie negativ.
- Zurückgegebene Ausleihen bleiben gespeichert.
- Akkus und Propeller werden nur als Menge geführt.
- Die Verfügbarkeit ergibt sich aus der aktiven Ausleihe (kein eigenes Statusfeld).

## 📋 Anforderungen

| ID | Anforderung |
|---|---|
| REQ-001 | Drohne anlegen |
| REQ-002 | Drohnen anzeigen |
| REQ-003 | Schülergruppe anlegen |
| REQ-004 | Drohne ausleihen |
| REQ-005 | Keine doppelte Ausleihe |
| REQ-006 | Zubehör ausleihen |
| REQ-007 | Aktive Ausleihen anzeigen |
| REQ-008 | Ausleihe zurückgeben |
| REQ-009 | Rückgabedatum speichern |
| REQ-010 | Ausleihhistorie anzeigen |

## 🚫 Out of Scope

- Login, Benutzerkonten, Rollen
- Einzelinventarisierung von Akkus (BAT-001 …)
- Reservierungen im Voraus
- Schadensverwaltung und Rückgabedifferenzen
- Auswertungen, Statistiken, Exporte

Alles Weitere folgt später als eigener Change Request.

## 🔄 Workflow

Spec-Driven Development mit OpenSpec:

```
Problem → Requirements → DDD → OpenSpec → Tasks → GitHub Issues
   → Implementierung → Tests → Pull Request → Review → Merge → Archive
```

- Verhalten ändern heißt: **zuerst die Spec ändern**.
- Ein Issue = ein Branch = ein Pull Request.
- Jedes Given/Then/When-Szenario braucht einen Test.
- Specs liegen unter `openspec/specs`, geplante Änderungen unter `openspec/changes`.

## 📁 Projektstruktur

```
drone-inventory/
├── backend/DroneInventory.Api/
├── frontend/drone-inventory-ui/
├── openspec/
│   ├── specs/       # aktueller Sollzustand
│   ├── changes/     # geplante Änderungen
│   └── config.yaml
├── AGENTS.md
└── README.md
```

## 🚀 Erste Schritte

```bash
# Backend
cd backend/DroneInventory.Api
dotnet run

# Frontend
cd frontend/drone-inventory-ui
npm install
ng serve

# Tests
dotnet test
ng test
```

## 🔗 Traceability

Jede Codezeile ist rückführbar:

`REQ → openspec/specs → Task → GitHub Issue → Branch → Pull Request → Code + Tests`

## 👥 Team

_Teammitglieder hier eintragen._
