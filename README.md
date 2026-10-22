# AeroLink – Baggage Loading

This is our IAB251 Assessment 2 prototype. It's the part of AeroLink that ground staff use to load bags onto a departing flight.

Staff sign in, pick a flight, and check each bag tag before it goes on the plane. If a bag is missing or damaged, they report it. A supervisor then decides what happens to that bag and closes loading once every bag is accounted for.

It's built with ASP.NET Core 8 (Razor Pages), a SQLite database and xUnit tests. Sign-in is checked against the HR system the unit gave us.

## Running it

You need **.NET 8** installed. If you only have .NET 10, run `winget install Microsoft.DotNet.SDK.8` first.

You run two programs at the same time, each in its own terminal:

1. **The HR system** (keep this one running). It's the staff database we sign in against.
   ```
   cd <your HRSystem folder>\HRSystem
   dotnet run
   ```
   Wait until it says `Now listening on: https://localhost:5001`.

2. **AeroLink**, from this repo's folder:
   ```
   dotnet run --project src/AeroLink.Web
   ```
   Then open `https://localhost:7100` in your browser.

If sign-in says "HR system unavailable", the HR system isn't running. If it's running but you still get that message, run `dotnet dev-certs https --trust` once and restart both programs.

## Logins to try

| Employee ID | Email | What happens |
|---|---|---|
| 100101 | s.mitchell@company.com | Baggage Handler: can load bags and report problems |
| 300100 | p.sharma@company.com | Baggage Handler |
| 200100 | j.cooper@company.com | Baggage Supervisor: can also decide problems and close loading |
| 500100 | e.johnson@company.com | Developer: turned away, because they're not baggage staff |

## Running the tests

In Visual Studio go to **Test → Run All Tests**, or run `dotnet test` from this folder. There are 234 tests, and they should all pass.

The tests don't touch the real database. Each one gets its own fresh copy of the starting data in memory.

## Starting fresh

The database (`aerolink.db`) is created on first run from the supplied baggage spreadsheet. It isn't saved to GitHub, so everyone starts with the same data.

To wipe it back to the start, either delete `src/AeroLink.Web/aerolink.db`, or set `"ResetOnStartup": true` in `appsettings.json`, run the app once, then set it back to `false`.

## What's where

```
AeroLink.sln              Opens both projects in Visual Studio
src/AeroLink.Web/
  Models/                 Flight, Bag and BaggageException (a reported problem)
  Data/                   The database setup and the starting data
  Services/               All the rules, one folder per feature
  Pages/                  The screens people click through
  Shared/                 Small helpers used everywhere (roles, sign-in checks)
tests/AeroLink.Tests/     The unit tests, one file per feature
data/                     The original baggage spreadsheet
tools/generate_seed.py    Turns the spreadsheet into starting data if it ever changes
```

The pages don't hold any rules themselves. They ask a service in `Services/`, which is what lets us unit test everything.

## Who did what

| Story | What it does | Built by | Tests |
|---|---|---|---|
| Skeleton | Project setup, database, starting data | Ryan | `SeedDataTests` |
| T1 | Main screen with the modules, locked until you sign in | Ryan | `MainScreenServiceTests` |
| T2 | Sign in using the HR system | Torsten | `LoginServiceTests` |
| T3 | Log out | Torsten | |
| T4 | Live loading status for a flight | Lachlan | `LoadingStatusServiceTests` |
| I1 | Pick a flight and see its bags | Ryan | `ManifestServiceTests` |
| I2 | Check a bag tag and confirm it's loaded | Ryan | `VerificationServiceTests` |
| I3 | Special bags need their instructions acknowledged first | Torsten | `SpecialHandlingServiceTests` |
| I4 | Report a missing or damaged bag | Torsten | `ProblemReportServiceTests` |
| I5 | Supervisor resolves a problem or approves a bag to stay behind | Lachlan | `ExceptionDecisionServiceTests` |
| I6 | Supervisor closes loading and gets a summary | Lachlan | `LoadingClosureServiceTests` |

## Things worth knowing about the data

- **Two flights share a number.** Flights 101 and 110 are both called DEMO101 but leave on different days, so the app always works from the flight ID, not the number.
- **One problem report was on the wrong flight.** In the spreadsheet, problem 6 says flight 101, but its bag (DEMO-102-001) is on flight 102. We put it under 102.
- **Some reports already had a decision written in** even though they were still open. We left those blank so a supervisor can decide them in the app.
- **Flights 102, 104, 105, 107 and 108 start with an open problem,** so they can't be closed until a supervisor deals with it.

## How we worked

- Nobody pushed straight to `main`. Each story had its own branch, like `feature/I2-verify-bag`, went in through a pull request, and was reviewed by someone else.
- Most commits mention the Azure Boards task, like `AB#68`, so the board links to the code.
- Every public class and method has a short `///` comment explaining what it does.
