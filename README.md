# S Bus — Minibus Seat Booking (MVP)

A booking site for the Cairo ⇄ Suez minibus line. Passengers pick a trip and a seat, pay with InstaPay and upload the transfer screenshot; the office confirms the payment and prints the passenger manifest.

Decisions are recorded in [docs/DECISIONS.md](docs/DECISIONS.md) and the build plan in [docs/PLAN.md](docs/PLAN.md).

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
   dotnet run --project src/SBus.Web --launch-profile http
   ```

   In Development the first run creates the `sbus` database, applies the migrations, seeds demo data (stops, route, a 14-seat minibus, drivers and 6 daily schedules), creates the office user, and generates trips for the next 7 days.

3. Open `http://localhost:5056` for passengers and `http://localhost:5056/Office` for the office. The office email is `AppSettings:OfficeUserEmail` in `appsettings.Development.json`.

## Tests

```bash
dotnet test
```

| Project | What it covers |
|---|---|
| `SBus.Domain.UnitTests` | Booking state machine, seat layout parsing, route rules |
| `SBus.Application.UnitTests` | Validators, phone normalisation, Cairo time, receipt file detection |
| `SBus.Application.SubcutaneousTests` | Booking flows against a real PostgreSQL database, including 20 concurrent bookings of one seat; translation coverage |

The database tests use a separate `sbus_tests` database (dropped and recreated on every run) and read the connection from the same user-secrets, or from the `SBUS_TEST_CONNECTION` environment variable. Without either they are skipped.

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
