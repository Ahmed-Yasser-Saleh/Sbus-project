# Phase 2 mobile API report

The API remains in SBus.Web and reuses the existing Application commands, queries and Identity services. Phases 1 and 2 are implemented; Flutter (Phase 3) has not started. See [API configuration and authentication](API.md).

## Endpoint reference

All paths below start with /api/v1. Success responses use data; paged lists also return pagination { page, pageSize, hasMore }. Dates use yyyy-MM-dd, times use HH:mm:ss, and JSON enums use the existing numeric values. Direction is 1 (CairoToSuez) or 2 (SuezToCairo); route parameters accept the enum name or value. OpenAPI has the complete request and response schemas.

| Method / path | Access | Shared use case / behavior |
| --- | --- | --- |
| GET /trips?direction&date&page&pageSize | Public | Available trips, effective filters and booking-window days; existing normalization |
| GET /trips/{id} | Traveler, complete profile | Booking details, seat map and stops; closed/full trips remain data states |
| POST /bookings | Traveler, complete profile | CreateOnlineBookingCommand; returns bookingId/publicToken |
| GET /tickets/{token} | Existing ticket ownership rules | All ticket statuses, receipt/cancel eligibility and conditional driver information |
| POST /tickets/{token}/receipt | Existing ticket ownership rules | Multipart field receipt; same content sniffing, size and expiry behavior |
| POST /tickets/{token}/cancel | Existing ticket ownership rules | Existing passenger cancellation cutoff and state machine |
| GET /me/tickets?page | Traveler, complete profile | Existing current-user query; 20 records/page and all statuses |
| GET /me/profile | Traveler | Current profile; incomplete profiles may access |
| PUT /me/profile | Traveler | UpdateTravelerProfileCommand (fullName, phoneNumber) |
| GET /company/status | CompanyOwner or CompanyEmployee | Existing Coming Soon placeholder: available=false |
| GET /office/day?date | Office | Cairo today default, day trips and pending-payment count |
| GET /office/trips/{id}/manifest | Office | Existing manifest DTO, seat map and pickup ordering |
| POST /office/trips/{id}/bookings | Office | Passenger/phone/stops/seatNumbers; immediately confirmed walk-in booking |
| PUT /office/trips/{id}/assignment | Office | busId/driverId; preserves booked-seat coverage |
| POST /office/bookings/{id}/cancel | Office | Existing office cancellation |
| GET /office/payments?page&pageSize | Office | AwaitingConfirmation, oldest receipt first |
| POST /office/bookings/{id}/payment/confirm | Office | Existing payment confirmation |
| POST /office/bookings/{id}/payment/reject | Office | reason, required/max 200; existing rejection |
| GET /office/bookings/{id}/receipt | Office | Private JPEG/PNG/PDF stream; no-store and nosniff |
| GET /office/stops?activeOnly&page&pageSize | Office | Ordered stop DTOs |
| POST /office/stops | Office | name/description |
| PUT /office/stops/{id} | Office | name/description/isActive |
| GET /office/routes/{direction} | Office | Ordered route stop DTOs |
| PUT /office/routes/{direction} | Office | stops [{ stopId, minutesFromStart }]; existing route rules |
| GET /office/seat-layouts?page&pageSize | Office | Immutable layout DTOs and grids |
| POST /office/seat-layouts | Office | name/grid; existing layout parser |
| GET /office/buses?activeOnly&page&pageSize | Office | Fleet and layout DTOs |
| POST /office/buses | Office | plateNumber/seatLayoutId |
| PUT /office/buses/{id} | Office | plateNumber/seatLayoutId/isActive |
| GET /office/drivers?activeOnly&page&pageSize | Office | Driver DTOs |
| POST /office/drivers | Office | name/phoneNumber |
| PUT /office/drivers/{id} | Office | name/phoneNumber/isActive |
| GET /office/schedules?page&pageSize | Office | Direction/time ordered schedule DTOs |
| POST /office/schedules | Office | direction/departureTime/price/busId/driverId |
| PUT /office/schedules/{id} | Office | departureTime/price/busId/driverId/isActive; existing propagation counts |

Authentication, registration, email confirmation and password recovery endpoints are listed in [API.md](API.md#implemented-endpoints).

Company roles retain their existing exclusion from Traveler/legacy Office operations, including mixed-role accounts. Owned tickets return BOOKING_NOT_FOUND to other users and anonymous clients; unowned legacy token links retain their existing anonymous access. Receipts submitted before hold expiry still do not expire automatically.

Paged Office queries use database Skip/Take with one lookahead record, deterministic ordering and a maximum pageSize of 100. Page defaults to 1, pageSize to 20; page must be 1–10000. The optional paging parameter is absent from existing Razor queries, preserving their unpaged behavior. My Tickets retains its existing 20-item next-page heuristic.

## Error contracts

Every API error uses application/problem+json with type, title, status, detail, instance, traceId and errorCode; validation adds errors keyed by request fields. All endpoints can return 500 INTERNAL_ERROR or 503 API_UNAVAILABLE. Authenticated endpoints return 401 AUTH_UNAUTHENTICATED / 403 AUTH_FORBIDDEN as applicable. Malformed bodies, GUIDs, dates, paging and fields return 400 VALIDATION_FAILED. Unknown routes use 404 API_ROUTE_NOT_FOUND; unsupported methods use 405 METHOD_NOT_ALLOWED with Allow.

| Feature | Additional status / errorCode |
| --- | --- |
| Complete-profile guards | 422 TRAVELER_PROFILE_INCOMPLETE |
| Booking creation | 404 TRIP_NOT_FOUND; 409 BOOKING_SEAT_UNAVAILABLE, TRIP_DEPARTED, TRIP_NOT_BOOKABLE; 400 BOOKING_STOPS_NOT_ON_ROUTE, BOOKING_STOP_ORDER_INVALID, BOOKING_SEATS_NOT_IN_LAYOUT, PROFILE_UPDATE_FAILED |
| Ticket reads/mutations | 404 BOOKING_NOT_FOUND; cancellation 409 BOOKING_INVALID_TRANSITION, BOOKING_CANCELLATION_CLOSED; upload 409 BOOKING_RECEIPT_NOT_ALLOWED |
| Receipt upload | 400 RECEIPT_TOO_LARGE, RECEIPT_UNSUPPORTED_FILE; 413 REQUEST_TOO_LARGE; 415 UNSUPPORTED_MEDIA_TYPE |
| Public booking/ticket writes | 429 RATE_LIMIT_EXCEEDED with Retry-After |
| Profile update | 400 PROFILE_UPDATE_FAILED |
| Office booking | 404 TRIP_NOT_FOUND; 409 TRIP_DEPARTED, BOOKING_SEAT_UNAVAILABLE; same booking stop/seat validation codes |
| Office assignment | 404 TRIP_NOT_FOUND, BUS_NOT_FOUND, DRIVER_NOT_FOUND; 409 TRIP_DEPARTED, BUS_INACTIVE, DRIVER_INACTIVE, BUS_LAYOUT_BOOKED_SEATS_CONFLICT |
| Office payment/cancel | 404 BOOKING_NOT_FOUND; 409 BOOKING_INVALID_TRANSITION |
| Office receipt | 404 RECEIPT_NOT_FOUND |
| Stops | 404 STOP_NOT_FOUND; 409 STOP_NAME_EXISTS |
| Routes | 400 ROUTE_STOPS_UNAVAILABLE, or VALIDATION_FAILED for route ordering/duplicates |
| Seat layouts | 409 SEAT_LAYOUT_NAME_EXISTS; 400 VALIDATION_FAILED with grid/name fields |
| Buses | 404 BUS_NOT_FOUND, SEAT_LAYOUT_NOT_FOUND; 409 BUS_PLATE_NUMBER_EXISTS, BUS_LAYOUT_BOOKED_SEATS_CONFLICT |
| Drivers | 404 DRIVER_NOT_FOUND |
| Schedules | 404 SCHEDULE_NOT_FOUND, BUS_NOT_FOUND, DRIVER_NOT_FOUND; 409 SCHEDULE_EXISTS, BUS_INACTIVE, DRIVER_INACTIVE |
| Concurrent database writes | 409 CONCURRENCY_CONFLICT |

Office success responses use no-store. Receipt responses stream the stored file without exposing its storage path. Production 5xx responses omit internal diagnostics. Razor cookie challenges, form field keys and HTML errors are unchanged.

## Assumptions and verification

- Auth POST requests share the configured IP quota; the read-only options endpoint remains available after that quota is exhausted.
- Mobile printing uses the shared manifest response; no server PDF generation or new business operation was added.
- Existing My Tickets status visibility, profile gates, booking windows and receipt expiry are authoritative.
- Settings retain their existing create/update operations; no deletion API or tenant-management functionality was invented.
- API request DTOs take update IDs from the URL. Shared validators and handlers enforce the existing rules.
- The user-reviewed migration and EF snapshot remain unchanged; no additional migration is required.
- Database integration uses the approved disposable sbus_tests fixture. Its hard guard verifies the actual EF connection before destructive setup. See the [development database incident](API.md#development-database-incident).
- Live Google credentials and actual SMTP delivery need deployment verification. API enabling, signing secrets and trusted Google audiences remain local configuration tasks.

## Final phase gate — 2026-10-16

| Suite | Passed | Failed | Skipped |
| --- | ---: | ---: | ---: |
| Domain unit tests | 32 | 0 | 0 |
| Application unit tests | 111 | 0 | 0 |
| Database-free API/service/contract tests | 418 | 0 | 0 |
| Subcutaneous tests, including PostgreSQL integration | 102 | 0 | 0 |
| .NET total | 663 | 0 | 0 |
| JavaScript regressions | 10 | 0 | 0 |

The full dotnet test run used the approved disposable sbus_tests fixture, with a Npgsql connection-string serialization round-trip check before launch. No assertions were weakened. Tests include real JWT login/refresh/logout, password-reset revocation, refresh races, ticket ownership, receipts, profile persistence, Office payment review and settings paging. Swagger generation and multipart upload metadata are verified.

All nine migration/designer/snapshot files retain their original SHA-256 hashes. EF reports no pending model changes. MobileRefreshTokens matches all ten columns, PostgreSQL types/nullability, three indexes (including unique TokenHash), both restrictive foreign keys and Version concurrency metadata. There are no differences to report and no new migration to run.

Reviewed build warnings: only the two existing CS8981 warnings for the addfullname migration type; the migration was left untouched.

Local commits (no push):

| Commit | Completed feature |
| --- | --- |
| 3df86eb | Phase 1 authentication foundation and database verification |
| 298e71c | Trips search/details |
| d31c85d | Booking/tickets and ownership integration |
| 084e8db | Profile/company status |
| 81652af | Office operations and database workflow |
| fea5ff7 | Office settings and database paging |

The final hardening/report commit follows these feature commits. Phase 2 is complete. Configure secrets, SMTP and optional Google audiences before enabling the API. Flutter remains for Phase 3.

The earlier destructive test-target error recreated development sbus and may have lost development data; no recovery was attempted. That incident is recorded in [API.md](API.md#development-database-incident). Subsequent integration runs target sbus_tests and the fixture rejects any other resolved database name.
