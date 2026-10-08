# S Bus — MVP Decisions

Last updated: 2026-10-08

## Goal

A demo to show S Bus: online seat booking instead of booking by phone.

## Business

| # | Topic | Decision |
|---|---|---|
| 1 | Customer | S Bus, a single company. The MVP is a demo to present to them. |
| 2 | Routes | One line in both directions: Cairo → Suez and Suez → Cairo. Each direction has a fixed start point and a fixed end point, and the bus stops at points along the way to pick up and drop off passengers. |
| 2a | Seats and stops | The passenger picks a pick-up and a drop-off point from a fixed list, and the seat is held for the whole trip. A seat whose passenger gets off midway is not resold on the same trip. |
| 3 | Price | One fixed price per trip, regardless of pick-up or drop-off point. |
| 4 | Schedules and buses | Fixed daily departure times in both directions. The vehicles are minibuses. The number of buses and seats is not known yet, so the seat layout is configurable and the demo runs on a default layout. |
| 5 | Payment | InstaPay only, confirmed manually by the office. Nobody pays the driver. The seat is held temporarily until the payment is confirmed. |
| 6 | Passenger account | No account. The passenger enters only a name and a mobile number. Every booking has a link with a long secret code, and that link is the key: the passenger views and cancels the ticket from it. There is no "my bookings by phone number" page, because anyone could type someone else's number and cancel their booking. A passenger who loses the link calls the office. WhatsApp or SMS codes come after the MVP. |
| 7 | Cancellation | Both the office and the passenger can cancel. Refunds are handled manually. |
| 8 | Office bookings | Staff can book through the system for people who call. This may be removed later. |

## Scope

| # | Topic | Decision |
|---|---|---|
| 9 | Tracking | Postponed until after the MVP. |
| 10 | Ticket | A ticket page behind a link: seat number, driver's number, trip details. No automatic messages. |

## Technical

| # | Topic | Decision |
|---|---|---|
| 11 | Front end | A single ASP.NET Core Razor Pages website, not a mobile app. A passenger area without sign-in and an office area with sign-in. Booking logic lives in separate class libraries so a future API or app can reuse it. A native Android app for drivers comes only with tracking. |
| 12 | Hosting | Local, on the developer's machine. |
| 13 | Team | One developer. Implementation starts only after all open points are agreed. |
| 14 | Language | Arabic and English with a switch in the header, covering the whole site (passenger pages, office panel and error messages). Arabic is the default and the choice is stored in a cookie. The Arabic text in the code is the translation key, the English text lives in `Resources/SharedResource.en.resx`, and a test fails if any text is missing a translation. Data the office types (stop and driver names) is shown exactly as entered. |
| 14a | Secrets | The database password and the office user's password live in `dotnet user-secrets`, not in `appsettings`. |
| — | Back end | .NET 10 (LTS, already installed; .NET 8 support ends in November 2026) + PostgreSQL 18 on port 5432. |
| — | Double-booking prevention | A partial unique index in the database on (trip, seat number) WHERE IsActive, not a check in code. The check in code exists only to return a clear message. |

## Architecture

The same structure as MechanicShop: `Domain` / `Application` (MediatR + FluentValidation, Features/Commands|Queries) / `Infrastructure` / `Web`, with the tests `Domain.UnitTests` / `Application.UnitTests` / `Application.SubcutaneousTests` / `Tests.Common`.

| Change from MechanicShop | Reason |
|---|---|
| `Web` uses Razor Pages instead of `Api` + Blazor, and there is no `Contracts` project | Decision 11. `Contracts` exists to share DTOs between an API and a client, and we have neither. |
| Cookie authentication with Identity instead of JWT | Razor Pages. |
| PostgreSQL instead of SQL Server, and migrations instead of `EnsureCreated` | `EnsureCreated` cannot alter tables once they exist. |
| Database tests run against a local `sbus_tests` database instead of Testcontainers | Docker is not running on the machine. The tests are skipped when no password is configured. |
| No `CachingBehavior` and no HybridCache | Free seats change every minute. A cache would show a seat as free when it is already taken. |
| The logging and performance behaviours log only the request name, the elapsed time and the user, not `{@Request}` | Requests contain the passenger's name and mobile number. |
| Serilog to the console and a file only. No Seq, OpenTelemetry, Scalar or API versioning | A local demo with no API. |
| MediatR 12.5.0 | The last Apache-2.0 release. Version 13 and later need a commercial licence. |
| Rate limiting on POST requests in the booking and ticket pages (10 per 10 minutes per IP) | Without it, a script could hold every seat every 15 minutes. |

## Decisions made during the build

| Topic | Decision |
|---|---|
| Uploading the transfer after the 15 minutes are up | Accepted as long as the job has not closed the booking yet (the status is still "waiting for payment"). The status is the source of truth, not the clock, so a passenger who already paid does not lose the seat over a few seconds. |
| Hold expiry | A job runs every minute, and before any new booking on a trip the expired holds on that trip are closed, so the seat frees up immediately. |
| Transfer screenshot | Must be JPG / PNG / WEBP / PDF, detected from the file content, not the extension. Maximum 5 MB. Stored outside `wwwroot` under a random name and viewable only from the office panel. |
| Mobile number | Egyptian mobiles only (010 / 011 / 012 / 015). Accepts +20 and Arabic-Indic digits and normalises to 01xxxxxxxxx. |
| Editing a schedule | Changes apply to upcoming trips that have no bookings. Trips with bookings stay as they are, and the office is told how many were kept. |
| Deactivating a schedule | Its trips disappear from online booking and existing bookings stay. The office can still book on them. |
| Seat layout | Cannot be edited once saved, because bookings refer to its seat numbers. Moving a bus to another layout is refused if a booked seat does not exist in the new layout. |
| Driver's number | Shown to the passenger only after the booking is confirmed. |
| Times | Stored in UTC and shown in Cairo time (which has daylight saving time). |
| Stop and driver names in English | Not translated. They are office data and stay as entered in both languages. |
