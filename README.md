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
- **Frontend:** Angular 22, TypeScript, Signal Forms, Vitest
- **Tests:** xUnit (Backend), Vitest (Frontend)
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

## 🖥️ Bedienung

| Seite | Pfad | Was man dort tut | Anforderung |
|---|---|---|---|
| Drohnen | `/drones` | Drohnen mit Status (Verfügbar / Ausgeliehen) sehen, neue Drohne hinzufügen, Name und Beschreibung bearbeiten | REQ-001, REQ-002, REQ-013 |
| Schülergruppen | `/student-groups` | Gruppen sehen (mit der Drohne, die sie gerade haben), hinzufügen, umbenennen und löschen | REQ-003, REQ-011, REQ-012 |
| Neue Ausleihe | `/loans/new` | Drohne mit Akkus und Propellern an eine Gruppe ausleihen | REQ-004 – REQ-006 |
| Aktive Ausleihen | `/loans` | Ausgeliehene Drohnen sehen und zurückgeben | REQ-007 – REQ-009 |
| Verlauf | `/loans/history` | Alle zurückgegebenen Ausleihen | REQ-010 |

Fehlermeldungen (z. B. "Die Drohne … ist bereits ausgeliehen.") kommen immer vom Backend; das Frontend prüft keine Fachregeln.

## 📋 Anforderungen

Ausformuliert mit SHALL und Given/When/Then in [`docs/requirements.md`](docs/requirements.md).

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
| REQ-011 | Schülergruppe umbenennen |
| REQ-012 | Schülergruppe löschen (nur ohne Ausleihen) |
| REQ-013 | Drohne bearbeiten (Name, Beschreibung) |

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
- Jedes Given/When/Then-Szenario braucht einen Test.
- Specs liegen unter `openspec/specs`, geplante Änderungen unter `openspec/changes`.

## 📁 Projektstruktur

```
dronenverwaltung/
├── backend/
│   ├── DroneInventory.slnx
│   ├── DroneInventory.Api/        # Web API: Domain, Data (EF Core, Migrationen), Services, Controllers
│   └── DroneInventory.Api.Tests/  # xUnit: Domain-, Datenbank-, Service- und API-Tests
├── frontend/drone-inventory-ui/   # Angular: core/ (API-Services, Models), features/ (Seiten)
├── docs/
│   ├── requirements.md            # Anforderungen REQ-001 … REQ-012
│   └── end-to-end-check.md        # Testprotokoll: Beispiel aus dem Auftrag, einmal komplett
├── openspec/
│   ├── specs/       # aktueller Sollzustand
│   ├── changes/     # geplante Änderungen
│   └── config.yaml
├── .claude/skills/  # Skills für Claude Code
├── .agents/skills/  # Skills für Codex
├── .config/dotnet-tools.json      # lokales Tool dotnet-ef
├── AGENTS.md        # Agentenregeln (Codex, Claude Code liest sie über CLAUDE.md)
├── CLAUDE.md
└── README.md
```

## 🚀 Erste Schritte

Voraussetzungen: .NET 10 SDK, Node.js 22+, GitHub CLI (`gh`), OpenSpec (`npm install -g @fission-ai/openspec`).

Alle Blöcke starten im **Repository-Root** (`dronenverwaltung/`).

**Einmalig: lokale .NET-Tools** (`dotnet-ef` aus `.config/dotnet-tools.json`)
```bash
dotnet tool restore
```

**Backend starten** – legt `droneinventory.db` an und migriert sie beim Start
```bash
cd backend/DroneInventory.Api
dotnet run
```

**Neue EF-Core-Migration anlegen**
```bash
cd backend
dotnet ef migrations add <Name> --project DroneInventory.Api --output-dir Data/Migrations
```

**Frontend starten** (Angular 22) – läuft auf http://localhost:4200
```bash
cd frontend/drone-inventory-ui
npm install
npx ng serve
```
`ng serve` leitet alle Aufrufe an `/api` über `proxy.conf.json` an das Backend auf `http://localhost:5087` weiter. Das Backend muss also gleichzeitig laufen.

**Tests**
```bash
dotnet test backend/DroneInventory.slnx

cd frontend/drone-inventory-ui
npx ng test --watch=false
```

**Neu anfangen** – Backend stoppen und `backend/DroneInventory.Api/droneinventory.db*` löschen; beim nächsten Start wird die Datenbank leer neu angelegt.

Den kompletten Ablauf aus dem Auftrag (anlegen, ausleihen, zurückgeben, Historie, Neustart) beschreibt [`docs/end-to-end-check.md`](docs/end-to-end-check.md).

## 🔗 Traceability

Jede Codezeile ist rückführbar:

`REQ → openspec/specs → Task → GitHub Issue → Branch → Pull Request → Code + Tests`

## 👥 Team

Luca Aigner
Jonathan Rath
