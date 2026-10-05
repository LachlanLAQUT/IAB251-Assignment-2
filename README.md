# AeroLink – Departure Baggage Loading Verification & Exception Management

IAB251 Assessment 2 prototype. ASP.NET Core 8 Razor Pages, SQLite, xUnit. Employee login goes through the supplied HR API.

## Run it

1. Open `HRSystem.sln` (the supplied HR API) and run it. It listens on `https://localhost:5001`.
2. Open `AeroLink.sln` and run **AeroLink.Web**. It listens on `https://localhost:7100` (http `5100`), so there's no port clash.
3. The home page should show **10 flights, 100 bags, 6 open exceptions**. If it does, the skeleton works.

Run tests: **Test → Run All Tests** in Visual Studio, or `dotnet test` from the repo root.

### Resetting the database

`aerolink.db` is created on first run from the seed data and is **not** committed.
- **Clean demo data:** set `"Database": { "ResetOnStartup": true }` in `appsettings.json`, run once, then set it back.
- **Changed an entity?** Delete `src/AeroLink.Web/aerolink.db`. `EnsureCreated` won't alter an existing schema.

## Structure

```
AeroLink.sln
src/AeroLink.Web/
  Data/          AppDbContext, DatabaseInitializer, ManifestSeedData (generated)
  Models/        Flight, Bag, BaggageException, Enums
  Services/      ← business logic goes here (one interface + class per service)
  Shared/        Roles, SessionKeys constants
  Pages/         Razor Pages (one folder per area, e.g. Pages/Baggage, Pages/Account)
tests/AeroLink.Tests/
  TestDatabase.cs   in-memory SQLite with full seed – use in every service test
data/             original manifest xlsx
tools/            generate_seed.py (regenerates ManifestSeedData.cs from the xlsx)
```

## Domain model (frozen – agree as a team before changing)

| Entity | Key fields |
|---|---|
| `Flight` | `FlightId`, `FlightNumber`, `ArrivalAirport`, `ScheduledDepartureUtc`, `LoadingStatus` (NotStarted / InProgress / Completed), `LoadingClosedAtUtc`, `LoadingClosedByEmployeeId` |
| `Bag` | `BagId`, `FlightId`, `Tag` (unique), `HandlingType`, `HandlingInstruction`, `Outcome` (Pending / Loaded / ApprovedNotToLoad), `HandlingAcknowledged`, `LoadedAtUtc`, `LoadedByEmployeeId` |
| `BaggageException` | `ExceptionId`, `BagId`, `FlightId`, `Category` (MissingBag / DamagedBag / Other), `Description`, `Status` (Open / Resolved / ApprovedNotToLoad), `ReportedAtUtc`, `DecisionReason`, `DecidedAtUtc` |

Helper properties (not stored, **can't be used inside EF queries**): `Bag.IsSpecial`, `Bag.IsAccountedFor`, `BaggageException.IsOpen`, `Flight.IsLoadingClosed`, `Flight.DisplayName`.

Employees are **not** stored locally. Store the HR `EmployeeId` only (e.g. `LoadedByEmployeeId`).

## HR API facts

- `GET /api/employees/{id}` returns `employeeId`, `email`, `jobTitle` and more. It returns 404 if the employee isn't found.
- The **role is the `jobTitle` field**: `Baggage Handler` or `BG_Supervisor`. Use `Roles.IsBaggageRole()` and `Roles.IsSupervisor()`.
- Test logins:

| Employee ID | Email | Role |
|---|---|---|
| 100101 | s.mitchell@company.com | Baggage Handler |
| 300100 | p.sharma@company.com | Baggage Handler |
| 200100 | j.cooper@company.com | BG_Supervisor |
| 500100 | e.johnson@company.com | Developer (should be rejected) |

## Data notes (from the supplied manifest)

- **Flight numbers aren't unique.** Flights 101 and 110 are both `DEMO101`, so always use `FlightId`.
- **Exception 6** is listed against flight 101, but bag 11 (`DEMO-102-001`) is on flight 102. It's seeded under flight 102.
- **Exceptions 3–5** had a decision reason despite being `Open`. They're seeded with no reason, so I5 can decide them.
- Flights 102, 104, 105, 107 and 108 each start with one open exception, so they can't be closed until I5 decides it.

## Team workflow

- **Never push to `main`.** Branch → PR → one review from another member → merge.
- **Branches:** `feature/<story>-<short-name>`, e.g. `feature/I2-verify-bag`, `feature/T2-hr-login`. Use `fix/<story>-<name>` for bug fixes.
- **Commits and PR titles** include the Azure Boards task ID: `AB#123 Add duplicate tag check`.
- Commit small and often. The marker reads the history.
- Register new services in `Program.cs` in the commented block, one line per service.
- Every public class and method needs `/// <summary>` XML docs. The build warns (CS1591) where they're missing.

## Ownership

| Story | Owner | Unit tests |
|---|---|---|
| Skeleton | Ryan | – |
| _fill in after team meeting_ | | |
