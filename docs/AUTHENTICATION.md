# Authentication deployment and verification

## Design and compatibility

S Bus remains a server-rendered Razor Pages application using its existing ASP.NET Core Identity stores and password hasher. Authentication uses secure cookies, not JWTs. The single login is `/Account/Login`; `/Office/Login` redirects there.

Traveler self-registration accepts email and password only. CompanyOwner and CompanyEmployee have centralized policies and a coming-soon page, with no management features. Existing Office access is preserved separately, as requested. Company roles take landing-page precedence and cannot satisfy Traveler/Office policies even if an account also holds those roles.

Registration requires public-email syntax, including a domain suffix; addresses such as `test@gmi` and domains with empty labels are rejected in the form and on the server. Format validation does not prove deliverability or ownership: email confirmation remains required by default. Successful registration redirects to the shared login page with a one-time confirmation reminder and a resend link. When confirmation is disabled, the notice says the user can sign in immediately.

Registration follows `RegisterModel → ISender → ValidationBehavior → RegisterTravelerCommandHandler`. FluentValidation owns email syntax, password length and password confirmation. Infrastructure enforces Identity's password policies before looking up the email, then reuses transactional account creation and fixed Traveler assignment. The command returns success only when an account was created; duplicate emails return an explicit email field error; other unsuccessful creation returns generic sign-in/reset guidance. This observable success/failure distinction can reveal account existence; request rate limiting reduces automated probing. New-account details stay internal. Confirmation delivery uses an application interface implemented by Web's SMTP service. The page retains browser validation and maps server errors to their fields; it does not duplicate registration rules in DataAnnotations.

Booking is the ticket entity: its existing Id, TripId, BookingStatus and CreatedAtUtc are reused. New web bookings get UserId from the authenticated principal, never from form input. My Tickets filters by that ID and returns 20 records per page. Ticket reads, receipt uploads and passenger cancellations filter ownership in the application layer. Legacy bookings remain unowned and retain their existing bearer-token links; never infer ownership from passenger names or phone numbers.

Google uses Microsoft's OAuth authorization-code handler with PKCE, correlation/state validation, a short-lived external cookie, server-side userinfo and no persisted provider tokens. A verified Google email is required. New Gmail/Workspace accounts can be confirmed by Google; other addresses require local confirmation. Existing confirmed password accounts require password proof with lockout before linking to the same account. Privileged accounts cannot be linked by matching email. Subsequent logins use the stable Google subject. See [Google's email-authority guidance](https://developers.google.com/identity/sign-in/android/backend-auth).

Registration, reset and resend responses avoid disclosing whether an email exists. SMTP failure logs contain only failure type. Passwords require 5–128 characters, without uppercase, lowercase, digit or symbol requirements. Identity lockout is five failures for 15 minutes. Confirmation/reset tokens expire after two hours. Cookie security stamps are validated on every request, revoking existing sessions after password reset.

## Migration: generate and apply yourself

No migration was generated or applied for this change. Database initialization on startup is disabled by default, including Development.

Run from the repository root, using your installed EF Core 10 tooling:

```powershell
dotnet ef migrations add TravelerAuthentication --project src/SBus.Infrastructure --startup-project src/SBus.Web --output-dir Data/Migrations
```

Configure SMTP settings below before running the command, because Web startup validates its configuration. If EF reports a tooling version mismatch, use a matching EF Core 10 tool in your environment; this task installed no tools.

Review the generated migration and model snapshot for:

- A nullable `Bookings.UserId` column, PostgreSQL `character varying(450)`.
- A restrictive FK to `AspNetUsers.Id`; deleting a user must not cascade-delete tickets.
- A composite index on `Bookings(UserId, CreatedAtUtc)`.
- Replacement of `AspNetUsers.EmailIndex` with a unique normalized-email index. PostgreSQL still permits multiple NULL entries.
- Three inserts into existing `AspNetRoles`: `sbus-traveler` / Traveler / TRAVELER; `sbus-company-owner` / CompanyOwner / COMPANYOWNER; `sbus-company-employee` / CompanyEmployee / COMPANYEMPLOYEE. Concurrency stamps are deterministic.
- No new ticket/Identity tables, no removed accounts/bookings, and no modifications to password hashes, confirmation flags, or existing Office memberships.

**EF generates schema and role seeds, but cannot infer existing-user role mapping.** Add this to the generated migration's `Up` after the role inserts:

```csharp
migrationBuilder.Sql("""
    INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
    SELECT u."Id", 'sbus-traveler'
    FROM "AspNetUsers" AS u
    ON CONFLICT ("UserId", "RoleId") DO NOTHING;
    """);
```

This adds Traveler to all existing users while retaining Office and other memberships. Existing unconfirmed emails stay unconfirmed; those users must confirm before signing in when confirmation is enabled. Leave legacy Booking.UserId NULL. Do not attach old tickets by email/phone guesswork.

Before applying, inspect duplicate normalized emails:

```sql
SELECT "NormalizedEmail", COUNT(*)
FROM "AspNetUsers"
WHERE "NormalizedEmail" IS NOT NULL
GROUP BY "NormalizedEmail"
HAVING COUNT(*) > 1;
```

Resolve duplicates through an explicit account-reconciliation process; the unique-index migration will fail safely rather than merge or delete accounts. Also verify that the reserved role names/IDs do not conflict with preexisting custom data. Back up production and review generated SQL. Apply the migration yourself before deploying this code. Rolling back the role seeds removes Traveler memberships; it does not restore legacy authentication cookies. All users must sign in again because the application cookie name changed.

## Configuration and local run

Nonsecret settings live in `src/SBus.Web/appsettings.json`. Supply the following via Web user-secrets or environment variables:

| Key | Purpose |
| --- | --- |
| ConnectionStrings:DefaultConnection | PostgreSQL connection |
| Authentication:RequireConfirmedEmail | Defaults to true; configurable for password sign-in |
| Authentication:Google:ClientId / ClientSecret | Both configured enables Google; both empty disables it |
| Authentication:Email:PublicOrigin | HTTPS origin used in email links, e.g. https://localhost:7116 |
| Authentication:Email:Host / Port | SMTP server with STARTTLS, normally port 587 |
| Authentication:Email:From | Valid sender address |
| Authentication:Email:Username / Password | SMTP credentials |
| AppSettings:InitializeDatabaseOnStartup | Default false; explicitly opting in permits Development migration/seeding |

Examples (replace values locally; never commit credentials):

```powershell
dotnet user-secrets set "Authentication:Google:ClientId" "<client-id>" --project src/SBus.Web
dotnet user-secrets set "Authentication:Google:ClientSecret" "<client-secret>" --project src/SBus.Web
dotnet user-secrets set "Authentication:Email:Host" "smtp.example.com" --project src/SBus.Web
dotnet user-secrets set "Authentication:Email:From" "accounts@example.com" --project src/SBus.Web
dotnet user-secrets set "Authentication:Email:Username" "<smtp-user>" --project src/SBus.Web
dotnet user-secrets set "Authentication:Email:Password" "<smtp-password>" --project src/SBus.Web
dotnet run --project src/SBus.Web --launch-profile https
```

Environment equivalents use double underscores, e.g. `Authentication__Google__ClientSecret`. SMTP uses STARTTLS with certificate validation; an implicit-TLS-only SMTP endpoint on port 465 is not supported by this sender. Failed sends can be retried using Resend Confirmation/Forgot Password.

HTTPS is required even locally. The application cookie uses `__Host-`, Secure, HttpOnly, Path=/ and SameSite=Lax; external/correlation cookies retain OAuth-compatible settings. Password reset updates the security stamp. Persist and protect Data Protection keys across deployments and share them between instances. Restrict AllowedHosts to your deployed hostname. Behind a proxy, configure trusted forwarded headers before HTTPS redirection; do not trust arbitrary forwarded IPs. Rate limiting is per-process/per-IP (20 account POST requests per ten minutes); opening or refreshing account pages does not consume this limit. Use ingress-level limits for multi-instance deployments.

The former Development seeder reset the Office password from settings on every startup. That behavior was removed. It now provisions only a new configured Office user and never promotes an existing account with a matching email.

## Google Cloud Console

Each Google sign-in requests `prompt=select_account` so users can choose their account even when a Google session is already active. This does not force re-entry of the Google password.

Following [Microsoft's Google setup documentation](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins?view=aspnetcore-10.0):

1. Select/create a Google Cloud project and configure Google Auth Platform branding, support email, audience and contact information.
2. Create an OAuth client of type Web application.
3. Register the exact redirect URIs `https://localhost:7116/signin-google` and `https://<your-host>/signin-google`. The callback is middleware, not a Razor page.
4. Add test accounts while the consent configuration is in Testing; complete publishing/verification as required for your production audience.
5. Store client credentials in secrets. Only openid/profile/email scopes are requested. Use separate development and production credentials.

## Automated checks

These selected checks do not connect to PostgreSQL or execute migrations:

```powershell
dotnet test tests/SBus.Domain.UnitTests/SBus.Domain.UnitTests.csproj
dotnet test tests/SBus.Application.UnitTests/SBus.Application.UnitTests.csproj
dotnet test tests/SBus.Application.SubcutaneousTests/SBus.Application.SubcutaneousTests.csproj --filter "FullyQualifiedName~TravelerAccessTests|FullyQualifiedName~TranslationCoverageTests"
```

Unit checks cover registration, fixed Traveler assignment, transaction commit decisions, verified-email trust, duplicate prevention, Google password proof, privileged-account linking rejection and ownership. Endpoint checks use the real Razor/authorization/antiforgery pipeline with a test authentication handler, without a database. They do not exercise a live Google or SMTP service.

Run the full existing database suite only after generating/reviewing the migration and configuring a disposable test database. That suite explicitly drops/recreates its test database and applies migrations; it was not run during this task.

## Manual checklist

- Register with email/password; confirm through SMTP; sign in and book a trip. My Tickets shows only your tickets with Pending/Confirmed and the existing terminal statuses.
- Confirm duplicate registration displays one email field error; unknown-email reset and incorrect login use generic messages. Repeated bad passwords lock the account; repeated account requests return 429.
- Request/reset a password; old password and existing sessions stop working. Invalid/expired tokens fail without exposing account existence.
- First Google login creates exactly one Traveler. Repeat login reuses it. Existing confirmed password account requires its password before linking; wrong password never links. Unverified Google email fails; third-party email requires local confirmation.
- Traveler B cannot read, upload a receipt or cancel Traveler A's ticket even with its token. Anonymous access to owned tickets fails. Legacy unowned links retain their previous behavior.
- Anonymous My Tickets/booking requests use the shared login. Travelers cannot open Office or company pages. Company roles land on Coming Soon and cannot access Traveler/Office features; legacy Office users retain Office access.
- POST without antiforgery fails; logout is POST-only. Check Secure/HttpOnly cookie flags, HTTPS redirects, and Arabic RTL/English LTR views.

## Source map

Full implementation is in these files (page pairs are `.cshtml` and `.cshtml.cs`):

- Application: `Common/Security/AuthPolicies.cs`, `BookingOwnership.cs`; `Features/Bookings/Queries/GetMyTickets/{GetMyTicketsQuery,GetMyTicketsQueryHandler,MyTicketDto}.cs`; existing CreateOnlineBooking, GetTicket, SubmitReceipt and CancelBookingByPassenger handlers.
- Domain: `src/SBus.Domain/Bookings/Booking.cs`.
- Infrastructure: `Identity/{Roles,TravelerAccounts,ExternalAccountState,ExternalAccountResult,IIdentityTransaction,IdentityTransaction}.cs`; `Data/AppDbContext.cs`, `Data/Configurations/BookingConfiguration.cs`, `Data/ApplicationDbContextInitialiser.cs`; `DependencyInjection.cs`.
- Web: `Pages/Account/{Login,Register,ExternalLogin,LinkGoogle,ConfirmEmail,ForgotPassword,ResendConfirmation,ResetPassword,Logout,ComingSoon}` page pairs; CheckEmail and AccessDenied pages; MyTickets page pair; Trip/Ticket page models; Office login/ logout compatibility; shared layout; `Services/{AccountLanding,AccountEmail,AccountEmailOptions,IAccountEmail}.cs`; Program, DependencyInjection, RateLimitPolicies, appsettings and English resources.
- Dependencies: `Directory.Packages.props` and `src/SBus.Web/SBus.Web.csproj` add only Microsoft's Google handler.
- Tests: `tests/SBus.Application.UnitTests/Identity/TravelerAccountsTests.cs`, `Bookings/BookingOwnershipTests.cs`; `tests/SBus.Application.SubcutaneousTests/Web/TravelerAccessTests.cs`; existing OfficeAccessTests/WebAppFactory updated for shared login and SMTP validation.

