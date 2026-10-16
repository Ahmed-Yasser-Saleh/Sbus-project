# Mobile API foundation â€” Phase 1

The API lives in `SBus.Web`; the Razor Pages continue to use their existing Identity cookies and HTML error pages. API requests use JWTs, application services and DTOs. Booking/office/profile feature endpoints and Flutter are Phase 2â€“4 work.

## Migration gate

The user generated and applied `20261013050141_AddMobileRefreshTokens` and confirmed it on 2026-10-16. The entity/configuration match its column names, PostgreSQL types, indexes, nullable columns and restrictive foreign keys. No agent-generated migration or snapshot edits were made. The existing disposable test fixture is explicitly authorized to apply the reviewed migrations to sbus_tests.

The user should generate, review and apply:

```powershell
dotnet ef migrations add AddMobileRefreshTokens --project src/SBus.Infrastructure --startup-project src/SBus.Web --output-dir Data/Migrations
dotnet ef database update --project src/SBus.Infrastructure --startup-project src/SBus.Web
```

Keep `AppSettings:InitializeDatabaseOnStartup=false`. Configure the existing SMTP startup settings as described in `AUTHENTICATION.md` before invoking EF tooling. Leave the mobile API disabled while generating the migration.

Expected new table `MobileRefreshTokens`:

| Column | Type / rule |
| --- | --- |
| Id | UUID primary key |
| UserId | Required varchar(450), restrictive FK to AspNetUsers |
| FamilyId | UUID; composite index with UserId |
| TokenHash | Required varchar(64), unique index |
| SecurityStampHash | Required varchar(64) |
| CreatedAtUtc / ExpiresAtUtc | Required timestamp with time zone |
| RevokedAtUtc | Nullable timestamp with time zone |
| ReplacedByTokenId | Nullable UUID, restrictive self-FK and index |
| Version | bigint optimistic concurrency token |

Only token/security-stamp SHA-256 hashes are stored. Rotation and revocation serialize each session family with a PostgreSQL transaction advisory lock, preventing logout from missing a concurrently created replacement. Rotation preserves the family's absolute expiry. A reused token or lost rotation race revokes the family.

Seven real PostgreSQL integration tests verify the migration/model match, real Identity/JWT login-refresh-logout, password-reset revocation, token reuse, concurrent refreshes, concurrent logout/rotation, hash uniqueness and restrictive foreign keys. All seven passed without skips. Mocked tests remain as separate unit coverage.

## Configuration and run

Use the existing `dotnet run --project src/SBus.Web --launch-profile https` command. After applying the migration, configure secrets locally:

```powershell
dotnet user-secrets set "Authentication:Mobile:SigningKey" "<random secret of at least 32 UTF-8 bytes>" --project src/SBus.Web
dotnet user-secrets set "Authentication:Mobile:Enabled" "true" --project src/SBus.Web
dotnet user-secrets set "Authentication:Mobile:GoogleClientIds:0" "<trusted Google ID-token audience>" --project src/SBus.Web
```

Generate a cryptographically random signing secret; do not use the placeholder literally. Environment equivalents use double underscores, for example `Authentication__Mobile__SigningKey`.

| Setting under Authentication:Mobile | Default / purpose |
| --- | --- |
| Enabled | false; disabled API routes return 503 ProblemDetails |
| Issuer | https://sbus.app |
| Audience | sbus-mobile |
| SigningKey | Secret, required when enabled; HMAC SHA-256 |
| AccessTokenMinutes | 10, allowed 1â€“60 |
| RefreshTokenDays | 30, allowed 1â€“90; absolute family lifetime |
| GoogleClientIds | Explicit allowlist of ID-token audiences; empty disables mobile Google sign-in |
| CorsOrigins | Explicit browser-origin allowlist; empty grants no cross-origin access; no credential cookies |
| AuthPermitLimit / AuthWindowSeconds | 20 POST requests per IP per 600 seconds, per process |

Native mobile clients do not need CORS. Browser clients can configure indexed origins, e.g. `Authentication:Mobile:CorsOrigins:0`. The web OAuth client configuration remains independent. Configure trusted proxy forwarding before relying on client IP rate limiting in a proxy deployment.

Development Swagger: `https://localhost:7116/api/docs`; JSON: `/api/docs/v1/swagger.json`. Swagger describes Bearer authentication and `application/problem+json` responses. Swagger is not exposed in Production.

Persist/protect ASP.NET Data Protection keys across server instances for the five-minute Google account-link proof. Google ID-token signatures, issuer, audience and expiry use Google's verifier, with explicit audience and verified-email checks. The adapter reuses `TravelerAccounts` for subject resolution and password linking. See [Google verification](https://docs.cloud.google.com/dotnet/docs/reference/Google.Apis/latest/Google.Apis.Auth.GoogleJsonWebSignature) and [ASP.NET JWT validation](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0).

## Implemented endpoints

| Method / route | Request / result |
| --- | --- |
| POST /api/v1/auth/login | email/password â†’ session |
| POST /api/v1/auth/refresh | refreshToken â†’ rotated session |
| POST /api/v1/auth/google | idToken â†’ Ready/session, PasswordProofRequired/linkingProof, or EmailConfirmationRequired |
| POST /api/v1/auth/google/link | linkingProof/password â†’ session |
| POST /api/v1/auth/logout | Bearer JWT; derives user/session from claims â†’ true acknowledgement |
| GET /api/v1/auth/options | Google availability, confirmed-email requirement, password limits |
| POST /api/v1/auth/register | Existing RegisterTravelerCommand → confirmation requirement |
| POST /api/v1/auth/email/resend | email → generic acknowledgement |
| POST /api/v1/auth/email/confirm | userId/code → confirmation acknowledgement |
| POST /api/v1/auth/password/forgot | email → generic acknowledgement |
| POST /api/v1/auth/password/reset | userId/code/password/confirmPassword → acknowledgement |

Success JSON is `{ "data": ... }`. Session DTOs include access/refresh tokens and expiry timestamps, user ID, roles, the existing landing destination, and profile-completion state. They do not expose an Identity entity or security stamp.

Each JWT request validates signature, issuer, audience, lifetime, current account eligibility/security stamp, and live session family. Roles are refreshed from Identity on each request, so role removal takes effect immediately. Password reset changes the security stamp and invalidates mobile access/refresh tokens. Logout invalidates the entire current family, including its access tokens. Other device families remain active. Two-factor-enabled accounts are rejected until a mobile two-factor flow is implemented; this application currently exposes no two-factor screen.

CompanyOwner/CompanyEmployee policies and legacy Office remain distinct. Existing company-role exclusions and landing precedence are preserved. API authentication never sets a Razor authentication cookie.

Pagination foundation uses validated `PageRequest` (page 1â€“10000, pageSize 1â€“100, default 20), `PagedResponse<T>` and `PageMetadata`. Feature-specific filtering/query pagination will be implemented in Application during Phase 2, preserving current defaults.

## Errors

API errors have `type`, `title`, `status`, safe `detail`, request-path `instance`, `traceId`, and stable `errorCode`. Validation includes an `errors` dictionary with camelCase field paths. API error responses use `application/problem+json` even when the client requests HTML. Unexpected Production exceptions never return internal exception messages. Development includes exception messages for debugging. Every 5xx is logged with its trace ID; 4xx use Information logging.

Categories use `https://sbus.app/errors/` plus `validation`, `authentication`, `forbidden`, `not-found`, `conflict`, `business-rule`, `request`, `rate-limit`, `unavailable`, or `internal`.

| HTTP | Foundation codes |
| --- | --- |
| 400 | VALIDATION_FAILED |
| 401 | AUTH_UNAUTHENTICATED, AUTH_INVALID_CREDENTIALS, AUTH_REFRESH_INVALID, AUTH_GOOGLE_TOKEN_INVALID, AUTH_GOOGLE_SIGN_IN_FAILED, AUTH_GOOGLE_LINK_INVALID, AUTH_GOOGLE_LINK_FAILED |
| 403 | AUTH_FORBIDDEN, AUTH_EMAIL_CONFIRMATION_REQUIRED |
| 404 | API_ROUTE_NOT_FOUND, RESOURCE_NOT_FOUND |
| 405 | METHOD_NOT_ALLOWED; includes Allow |
| 409 | BUSINESS_CONFLICT, CONCURRENCY_CONFLICT |
| 413 | REQUEST_TOO_LARGE |
| 415 | UNSUPPORTED_MEDIA_TYPE |
| 422 | BUSINESS_RULE_VIOLATION |
| 429 | RATE_LIMIT_EXCEEDED; includes Retry-After |
| 500 | INTERNAL_ERROR |
| 503 | API_UNAVAILABLE, AUTH_GOOGLE_UNAVAILABLE |

`ErrorCodes` also defines initial booking adapter codes: BOOKING_NOT_FOUND, TRIP_NOT_FOUND, BOOKING_SEAT_UNAVAILABLE, TRIP_DEPARTED, TRIP_NOT_BOOKABLE, BOOKING_RECEIPT_NOT_ALLOWED, BOOKING_CANCELLATION_CLOSED, BOOKING_INVALID_TRANSITION. Feature contracts and remaining codes are Phase 2 work. The shared result adapter maps typed Application/Domain results into the same exception/ProblemDetails pipeline without changing Razor handling.

## Database-free checks

Run these sequentially to avoid concurrent builds writing shared output files:

```powershell
dotnet test tests/SBus.Domain.UnitTests/SBus.Domain.UnitTests.csproj
dotnet test tests/SBus.Application.UnitTests/SBus.Application.UnitTests.csproj
dotnet test tests/SBus.Api.Tests/SBus.Api.Tests.csproj
dotnet test tests/SBus.Application.SubcutaneousTests/SBus.Application.SubcutaneousTests.csproj --filter "FullyQualifiedName~TravelerAccessTests|FullyQualifiedName~TranslationCoverageTests|FullyQualifiedName~LoginConfirmationTests|FullyQualifiedName~PartialNavigationTests|FullyQualifiedName~BookingPassengerTests"
```

The API tests mock Identity/provider verification/persistence as appropriate and exercise the real JWT/authorization/error/controller pipeline through `WebApplicationFactory`. Hosted database jobs are removed. EF model checks do not open a database connection. Provider cryptography depends on Google's library; live Google sign-in, SMTP delivery and PostgreSQL refresh-family concurrency remain deployment/integration verification tasks.

The full subcutaneous suite is now authorized against disposable sbus_tests. Its fixture drops/recreates that database and calls MigrateAsync; it never targets the development database. Set SBUS_TEST_CONNECTION or use Web user-secrets. Avoid unfiltered runs without a connection: existing DatabaseFact tests then report skips.

## Phase 1 verification report (2026-10-16)

Full solution: 368 passed, zero failed/skipped (Domain 32, Application 111, API 126, Subcutaneous 99). Seven subcutaneous tests exercise real refresh-token/Identity/PostgreSQL behavior. Registration and recovery endpoints reuse the existing registration command and a recovery service shared with Razor Pages; redirects, messages and eligibility rules are preserved.

Assumptions: API authentication/recovery belongs to the Phase 1 authentication foundation; feature endpoints follow in Phase 2. The user's migration is included unchanged for review. Live Google and SMTP still need deployment credentials/manual verification.

The test fixture now refuses every destructive operation unless the resolved EF connection targets exactly sbus_tests, and overrides EF test options explicitly. Its Traveler seed now has a valid email/normalized identity so existing profile-write tests reach their intended business scenarios. No assertions were removed or weakened.

### Development database incident

An earlier test invocation used a generic connection-string builder whose indexer showed sbus_tests while the serialized connection still parsed as sbus. The existing destructive fixture consequently recreated the development sbus database. Development data may have been lost. No backup restoration has been attempted. This was an agent error, not a schema mismatch. Subsequent runs use Npgsql's builder with serialization round-trip verification and the hard fixture guard, and run only on sbus_tests. Migration files and the snapshot remain unchanged.

## Phase 2 progress

### Trips feature

GET /api/v1/trips returns data, pagination, effective direction/date and booking-window days. Its Application handler reuses GetAvailableTripsQuery, normalizes defaults exactly like the home page, and paginates the results. GET /api/v1/trips/{id} reuses GetTripForBookingQuery and requires the existing Traveler policy plus a complete profile. Full/departed trips remain data states. Ten feature tests passed; no schema or Razor behavior changes.
