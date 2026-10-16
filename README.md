# S Bus — Minibus Seat Booking (MVP)

A booking site for the Cairo ⇄ Suez minibus line. Passengers pick a trip and a seat, pay with InstaPay and upload the transfer screenshot; the office confirms the payment and prints the passenger manifest.

Decisions are recorded in [docs/DECISIONS.md](docs/DECISIONS.md) and the build plan in [docs/PLAN.md](docs/PLAN.md).

## Mobile API (Phases 1 and 2)

The API runs inside `SBus.Web` at `/api/v1`, independently of Razor cookie authentication. It provides password/Google sign-in, refresh rotation, logout, API-only ProblemDetails, CORS, rate limiting and development Swagger at `https://localhost:7116/api/docs`.

The API is disabled by default. The user generated and applied `AddMobileRefreshTokens` and confirmed it on 2026-10-16. Its schema matches the entity/EF configuration. Configure the signing secret before enabling `Authentication:Mobile:Enabled`; store `Authentication:Mobile:SigningKey` in user-secrets/environment variables. See [docs/API.md](docs/API.md) for the schema, settings, endpoints, error codes and database-free test commands. Trips, bookings, tickets, profiles and Office operations/settings are implemented; see [the Phase 2 endpoint and error reference](docs/API-PHASE2.md). Flutter has not started.

```powershell
dotnet test tests/SBus.Api.Tests/SBus.Api.Tests.csproj
```

This project uses mocked authentication/storage and database-free `WebApplicationFactory` hosts. The PostgreSQL suite is now authorized: its existing fixture recreates sbus_tests and applies the user-reviewed migrations. Configure the dedicated SBUS_TEST_CONNECTION environment variable or ConnectionStrings:TestConnection in Web user-secrets before running it.

## Stack

- .NET 10, ASP.NET Core Razor Pages, PostgreSQL 18
- Clean Architecture modelled on MechanicShop: `Domain` / `Application` (MediatR + FluentValidation) / `Infrastructure` (EF Core + Npgsql, Identity) / `Web`
- Arabic (default, RTL) and English (LTR), switchable from the header

## First run

1. Store the database connection and the office password in user-secrets (they are never committed):

   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=sbus;Username=postgres;Password=<your password>" --project src/SBus.Web
   ```

   ```bash
   dotnet user-secrets set "AppSettings:OfficeUserPassword" "<office password>" --project src/SBus.Web
   ```

2. Run the site:

   ```bash
   dotnet run --project src/SBus.Web --launch-profile https
   ```

   Generate, review and apply migrations yourself before running the site. Database initialization on startup is disabled by default. Configure SMTP and optional Google login using [docs/AUTHENTICATION.md](docs/AUTHENTICATION.md). Explicitly enabling `AppSettings:InitializeDatabaseOnStartup` in Development restores migration/seeding on startup, including demo stops, routes, fleet and schedules.

3. Open `https://localhost:7116` for passengers and `https://localhost:7116/Office` for the office. All accounts use `/Account/Login`. The office email is `AppSettings:OfficeUserEmail` in `appsettings.Development.json`.

## Tests

```bash
dotnet test
```

| Project | What it covers |
|---|---|
| `SBus.Domain.UnitTests` | Booking state machine, seat layout parsing, route rules |
| `SBus.Api.Tests` | Database-free JWT/controller/ProblemDetails/endpoint contracts and auth service tests |
| `SBus.Application.UnitTests` | Validators, phone normalisation, Cairo time, receipt file detection |
| `SBus.Application.SubcutaneousTests` | Booking flows against a real PostgreSQL database, including 20 concurrent bookings of one seat; translation coverage |

The database tests drop/recreate only a local `sbus_tests` database. Their connection comes exclusively from `SBUS_TEST_CONNECTION` or `ConnectionStrings:TestConnection` in Web user-secrets, with the environment variable taking precedence. They never read or rewrite `DefaultConnection`. Without a dedicated setting, database tests skip; an unsafe dedicated setting hard-fails.

```powershell
dotnet user-secrets set "ConnectionStrings:TestConnection" "Host=localhost;Port=5432;Database=sbus_tests;Username=<test user>;Password=<test password>" --project src/SBus.Web
dotnet test tests/SBus.Application.SubcutaneousTests/SBus.Application.SubcutaneousTests.csproj --filter FullyQualifiedName~TestDatabaseSafetyTests
```

The fixture validates the actual EF connection immediately before both EnsureDeletedAsync and MigrateAsync. It requires the exact database name `sbus_tests` and an explicit host of `localhost`, `127.0.0.1` or `::1` (bracketed IPv6 is also accepted). Remote hosts, host lists, sockets, absent database names and all other database names are rejected. Safety unit tests need no database and never open a connection.

## Languages

- Arabic is the default. The header link switches to English and the choice is stored in a cookie.
- Arabic text in the code is the translation key. English translations live in `src/SBus.Web/Resources/SharedResource.en.resx`.
- `TranslationCoverageTests` fails if any Arabic text in the pages, domain errors or validators has no English translation, or if a translation drops a `{0}` placeholder.
- Data the office types (stop names, driver names) is shown as entered and is not translated.

## New migration

```bash
dotnet ef migrations add <Name> --project src/SBus.Infrastructure --startup-project src/SBus.Web --output-dir Data/Migrations
```

## Key settings

| Key | Meaning | Default |
|---|---|---|
| `Booking:HoldMinutes` | How long a seat is held before the transfer is uploaded | 15 |
| `Booking:PassengerCancellationCutoffHours` | Last moment a passenger can cancel online, before departure | 2 |
| `Booking:BookingWindowDays` | How many days ahead booking opens | 7 |
| `Booking:MaxReceiptBytes` | Maximum receipt upload size | 5 MB |
| `Booking:InstaPayAddress` | InstaPay address shown to passengers | — |
| `Booking:OfficePhone` | Office phone shown when something goes wrong | — |
| `AppSettings:SeedDemoData` | Seed demo data when the database is empty | `true` in Development |
| `AppSettings:ReceiptsPath` | Where transfer screenshots are stored (outside `wwwroot`) | `App_Data/receipts` |
