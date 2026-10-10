# Repository Guidelines

## Project Structure & Module Organization

S Bus is a Cairo–Suez minibus booking application using .NET 10, Razor Pages, and PostgreSQL 18. Keep changes within the appropriate Clean Architecture layer:

- `src/SBus.Domain`: entities, booking state transitions, and business rules.
- `src/SBus.Application`: MediatR commands/queries, FluentValidation validators, DTOs, and interfaces; organize features under `Features/<Feature>/Commands` or `Queries`.
- `src/SBus.Infrastructure`: EF Core persistence, migrations, Identity, and receipt storage.
- `src/SBus.Web`: Razor Pages, localization resources, and static assets in `wwwroot`.
- `tests/`: domain/application unit tests, subcutaneous integration tests, and shared helpers in `SBus.Tests.Common`.
- `docs/DECISIONS.md` and `docs/PLAN.md`: architecture decisions and implementation plan.

## Build, Test, and Development Commands

Use the SDK specified by `global.json`. Run commands from the repository root:

- `dotnet restore`: restore centrally managed NuGet dependencies.
- `dotnet build`: compile the solution and run analyzers.
- `dotnet run --project src/SBus.Web --launch-profile https`: start at `https://localhost:7116`; configure SMTP and apply reviewed migrations first (see `docs/AUTHENTICATION.md`).
- `dotnet test`: run all test projects.
- `dotnet test --collect:"XPlat Code Coverage"`: collect coverage with Coverlet.
- `dotnet ef migrations add <Name> --project src/SBus.Infrastructure --startup-project src/SBus.Web --output-dir Data/Migrations`: add a schema migration.

## Coding Style & Naming Conventions

Follow `.editorconfig`: spaces, four-space C# indentation, two-space XML indentation, and braces on new lines. StyleCop analyzers are configured centrally. Use PascalCase for types/methods, `I`-prefixed interfaces, camelCase locals, and `_camelCase` private fields. Match existing feature names such as `CreateOnlineBookingCommand`, `CommandHandler`, and `CommandValidator`. Manage package versions in `Directory.Packages.props`.

## Testing Guidelines

Use xUnit and NSubstitute; name classes `*Tests` and methods descriptively, e.g. `CreateHold_DuplicateSeats_Fails`. Add regression coverage in the corresponding layer. No numeric coverage threshold is configured. Database tests recreate `sbus_tests`; configure user-secrets or `SBUS_TEST_CONNECTION`, otherwise they skip. Preserve translation coverage: Arabic keys require English entries in `Resources/SharedResource.en.resx`, including matching placeholders.

## Commit & Pull Request Guidelines

History uses short imperative subjects such as `add auth` and `update UI design`. Keep commits focused. PRs should describe behavior changes, link relevant issues, report test results/skips, and include screenshots for UI changes in Arabic RTL and English LTR.

## Security & Configuration

Store `ConnectionStrings:DefaultConnection` and `AppSettings:OfficeUserPassword` in Web user-secrets; see `README.md` for setup. Keep uploaded receipts outside `wwwroot` and exclude credentials and passenger data from commits.
