# Traveler profile changes

Travelers enter their full name and Egyptian mobile number at registration and can edit them at `/Account/Profile`. Google accounts use the Google name claims and initially have no phone number. Existing and Google travelers with missing details complete them at `/Account/CompleteProfile` before booking or viewing My Tickets.

A reusable `CompleteTravelerProfileFilter` protects the Trip and My Tickets Razor Pages. Login, logout, profile pages, static files, and company roles do not pass through this gate. Password login, Google callbacks, Google linking, and profile saving preserve local return URLs. Profile forms use the existing Razor partial and fetch navigation, with ordinary form submission as a fallback.

Online booking commands accept optional passenger overrides. Missing values fall back to the current traveler's persisted profile and all resolved values are validated with the Register/Edit Profile rules. The handler stores these values in the booking's existing snapshot columns and assigns the current traveler as the owner. A pencil button beside each passenger field unlocks that field in place, preserving trip/date/seats/stops. Account details remain unchanged unless the traveler checks Also update my account details; that profile update and booking creation share one PostgreSQL transaction, including rollback after a seat race or save failure. Office bookings retain their own passenger fields and validation.

Names are required and limited to 100 characters. Egyptian mobile phones accept local, +20, 0020, Arabic/Persian digits, and common separators; profile and online snapshot values use E.164 (`+201012345678`). Phone numbers are not unique. Changing the phone clears Identity's phone-confirmed flag.

## Model and migration scope

- Add `AppUser.FullName`: nullable string, EF maximum length 100; PostgreSQL `AspNetUsers.FullName`, nullable `character varying(100)`.
- Nullable storage supports existing accounts, missing Google name claims, and company/office users. Registration and profile-update validation require a name for travelers. Do not populate full names from `UserName`, which is the login email.
- Reuse Identity's existing `PhoneNumber` and `PhoneNumberConfirmed`; no new phone column, index, or uniqueness constraint.
- Booking passenger name and phone snapshot columns already exist; no booking schema change.
- No migration was generated or applied by the agent. Migration files and the model snapshot added during the work were left untouched.

## Initial profile-task verification

- Solution build succeeded using `--artifacts-path .artifacts/profile-verification` because the running Web app locks the default build output. The user's newly added migration produces two CS8981 naming warnings on a fresh build.
- Application unit tests: 103 passed.
- Domain unit tests: 32 passed.
- Web and localization tests: 61 passed, including Arabic RTL/English LTR rendering, profile prefilling, localized validation fragments, local-only return URLs, and the Google challenge's preserved destination.
- JavaScript syntax check and `git diff --check` passed.
- Database-backed tests were not executed because their existing fixture applies EF migrations. Their callers and traveler fixture were updated, and a regression test verifies that editing a saved profile leaves an earlier booking's passenger snapshot unchanged. Run those after reviewing your migration.

## Inline passenger-edit follow-up

No entity properties, EF mappings, or schema changes were added in this follow-up. No migration is required for these edits. `MyTicketDto` now exposes the booking's existing name/phone snapshot, and My Tickets and the ticket/payment page display those values. Existing office manifest and payment queries already read the snapshots.

The booking form uses `data-partial="booking-content"` with the existing fetch mechanism. Server validation returns a Razor fragment containing the submitted seat selections, pickup/dropoff stops, passenger text and checkbox state. Inline client errors disable Book the seat while editing invalid values. There are no Change or Cancel actions. Each pencil opens only its own field, and the account-update checkbox remains available after editing starts. The profile completeness filter is unchanged.

Follow-up validation: solution build succeeded; 111 application unit tests, 68 Web/localization tests, and 8 vanilla-JavaScript tests passed. Database tests were not run because their fixture applies migrations. Added database regressions cover booking-only overrides, opt-in updates, snapshot reads, and rolling back profile updates after booking save failure.

### Every file changed in the follow-up

- [docs/PROFILE-CHANGES.md](../docs/PROFILE-CHANGES.md)
- [src/SBus.Application/Common/Interfaces/IBookingTransaction.cs](../src/SBus.Application/Common/Interfaces/IBookingTransaction.cs)
- [src/SBus.Application/Features/Bookings/Commands/CreateOnlineBooking/CreateOnlineBookingCommand.cs](../src/SBus.Application/Features/Bookings/Commands/CreateOnlineBooking/CreateOnlineBookingCommand.cs)
- [src/SBus.Application/Features/Bookings/Commands/CreateOnlineBooking/CreateOnlineBookingCommandHandler.cs](../src/SBus.Application/Features/Bookings/Commands/CreateOnlineBooking/CreateOnlineBookingCommandHandler.cs)
- [src/SBus.Application/Features/Bookings/Commands/CreateOnlineBooking/CreateOnlineBookingCommandValidator.cs](../src/SBus.Application/Features/Bookings/Commands/CreateOnlineBooking/CreateOnlineBookingCommandValidator.cs)
- [src/SBus.Application/Features/Bookings/Queries/GetMyTickets/GetMyTicketsQueryHandler.cs](../src/SBus.Application/Features/Bookings/Queries/GetMyTickets/GetMyTicketsQueryHandler.cs)
- [src/SBus.Application/Features/Bookings/Queries/GetMyTickets/MyTicketDto.cs](../src/SBus.Application/Features/Bookings/Queries/GetMyTickets/MyTicketDto.cs)
- [src/SBus.Infrastructure/Data/BookingTransaction.cs](../src/SBus.Infrastructure/Data/BookingTransaction.cs)
- [src/SBus.Infrastructure/DependencyInjection.cs](../src/SBus.Infrastructure/DependencyInjection.cs)
- [src/SBus.Web/Pages/MyTickets.cshtml](../src/SBus.Web/Pages/MyTickets.cshtml)
- [src/SBus.Web/Pages/Shared/_BookingForm.cshtml](../src/SBus.Web/Pages/Shared/_BookingForm.cshtml)
- [src/SBus.Web/Pages/Shared/_BookingPassengerDetails.cshtml](../src/SBus.Web/Pages/Shared/_BookingPassengerDetails.cshtml)
- [src/SBus.Web/Pages/Ticket.cshtml](../src/SBus.Web/Pages/Ticket.cshtml)
- [src/SBus.Web/Pages/Trip.cshtml](../src/SBus.Web/Pages/Trip.cshtml)
- [src/SBus.Web/Pages/Trip.cshtml.cs](../src/SBus.Web/Pages/Trip.cshtml.cs)
- [src/SBus.Web/Resources/SharedResource.en.resx](../src/SBus.Web/Resources/SharedResource.en.resx)
- [src/SBus.Web/wwwroot/js/partial-navigation.js](../src/SBus.Web/wwwroot/js/partial-navigation.js)
- [src/SBus.Web/wwwroot/js/traveler-profile.js](../src/SBus.Web/wwwroot/js/traveler-profile.js)
- [tests/SBus.Application.SubcutaneousTests/Features/Bookings/BookingFlowTests.cs](../tests/SBus.Application.SubcutaneousTests/Features/Bookings/BookingFlowTests.cs)
- [tests/SBus.Application.SubcutaneousTests/Web/BookingPassengerTests.cs](../tests/SBus.Application.SubcutaneousTests/Web/BookingPassengerTests.cs)
- [tests/SBus.Application.UnitTests/Bookings/CreateOnlineBookingCommandValidatorTests.cs](../tests/SBus.Application.UnitTests/Bookings/CreateOnlineBookingCommandValidatorTests.cs)
- [tests/SBus.Application.UnitTests/Identity/TravelerProfileTests.cs](../tests/SBus.Application.UnitTests/Identity/TravelerProfileTests.cs)
- [tests/SBus.Web.JavaScriptTests/booking-passenger.test.cjs](../tests/SBus.Web.JavaScriptTests/booking-passenger.test.cjs)

## All files changed across both tasks

- [docs/PROFILE-CHANGES.md](../docs/PROFILE-CHANGES.md)
- [src/SBus.Application/Common/Interfaces/IBookingTransaction.cs](../src/SBus.Application/Common/Interfaces/IBookingTransaction.cs)
- [src/SBus.Application/Common/Interfaces/ITravelerProfiles.cs](../src/SBus.Application/Common/Interfaces/ITravelerProfiles.cs)
- [src/SBus.Application/Common/Interfaces/ITravelerRegistration.cs](../src/SBus.Application/Common/Interfaces/ITravelerRegistration.cs)
- [src/SBus.Application/Common/Validation/TravelerProfileRules.cs](../src/SBus.Application/Common/Validation/TravelerProfileRules.cs)
- [src/SBus.Application/Features/Accounts/Commands/RegisterTraveler/RegisterTravelerCommand.cs](../src/SBus.Application/Features/Accounts/Commands/RegisterTraveler/RegisterTravelerCommand.cs)
- [src/SBus.Application/Features/Accounts/Commands/RegisterTraveler/RegisterTravelerCommandHandler.cs](../src/SBus.Application/Features/Accounts/Commands/RegisterTraveler/RegisterTravelerCommandHandler.cs)
- [src/SBus.Application/Features/Accounts/Commands/RegisterTraveler/RegisterTravelerCommandValidator.cs](../src/SBus.Application/Features/Accounts/Commands/RegisterTraveler/RegisterTravelerCommandValidator.cs)
- [src/SBus.Application/Features/Accounts/Commands/UpdateTravelerProfile/UpdateTravelerProfileCommand.cs](../src/SBus.Application/Features/Accounts/Commands/UpdateTravelerProfile/UpdateTravelerProfileCommand.cs)
- [src/SBus.Application/Features/Accounts/Commands/UpdateTravelerProfile/UpdateTravelerProfileCommandHandler.cs](../src/SBus.Application/Features/Accounts/Commands/UpdateTravelerProfile/UpdateTravelerProfileCommandHandler.cs)
- [src/SBus.Application/Features/Accounts/Commands/UpdateTravelerProfile/UpdateTravelerProfileCommandValidator.cs](../src/SBus.Application/Features/Accounts/Commands/UpdateTravelerProfile/UpdateTravelerProfileCommandValidator.cs)
- [src/SBus.Application/Features/Accounts/Dtos/TravelerProfileDto.cs](../src/SBus.Application/Features/Accounts/Dtos/TravelerProfileDto.cs)
- [src/SBus.Application/Features/Bookings/Commands/CreateOfficeBooking/CreateOfficeBookingCommandValidator.cs](../src/SBus.Application/Features/Bookings/Commands/CreateOfficeBooking/CreateOfficeBookingCommandValidator.cs)
- [src/SBus.Application/Features/Bookings/Commands/CreateOnlineBooking/CreateOnlineBookingCommand.cs](../src/SBus.Application/Features/Bookings/Commands/CreateOnlineBooking/CreateOnlineBookingCommand.cs)
- [src/SBus.Application/Features/Bookings/Commands/CreateOnlineBooking/CreateOnlineBookingCommandHandler.cs](../src/SBus.Application/Features/Bookings/Commands/CreateOnlineBooking/CreateOnlineBookingCommandHandler.cs)
- [src/SBus.Application/Features/Bookings/Commands/CreateOnlineBooking/CreateOnlineBookingCommandValidator.cs](../src/SBus.Application/Features/Bookings/Commands/CreateOnlineBooking/CreateOnlineBookingCommandValidator.cs)
- [src/SBus.Application/Features/Bookings/Common/BookingRequestValidator.cs](../src/SBus.Application/Features/Bookings/Common/BookingRequestValidator.cs)
- [src/SBus.Application/Features/Bookings/Common/IBookingRequest.cs](../src/SBus.Application/Features/Bookings/Common/IBookingRequest.cs)
- [src/SBus.Application/Features/Bookings/Queries/GetMyTickets/GetMyTicketsQueryHandler.cs](../src/SBus.Application/Features/Bookings/Queries/GetMyTickets/GetMyTicketsQueryHandler.cs)
- [src/SBus.Application/Features/Bookings/Queries/GetMyTickets/MyTicketDto.cs](../src/SBus.Application/Features/Bookings/Queries/GetMyTickets/MyTicketDto.cs)
- [src/SBus.Infrastructure/Data/BookingTransaction.cs](../src/SBus.Infrastructure/Data/BookingTransaction.cs)
- [src/SBus.Infrastructure/Data/Configurations/AppUserConfiguration.cs](../src/SBus.Infrastructure/Data/Configurations/AppUserConfiguration.cs)
- [src/SBus.Infrastructure/DependencyInjection.cs](../src/SBus.Infrastructure/DependencyInjection.cs)
- [src/SBus.Infrastructure/Identity/AppUser.cs](../src/SBus.Infrastructure/Identity/AppUser.cs)
- [src/SBus.Infrastructure/Identity/TravelerAccounts.cs](../src/SBus.Infrastructure/Identity/TravelerAccounts.cs)
- [src/SBus.Infrastructure/Identity/TravelerProfiles.cs](../src/SBus.Infrastructure/Identity/TravelerProfiles.cs)
- [src/SBus.Web/DependencyInjection.cs](../src/SBus.Web/DependencyInjection.cs)
- [src/SBus.Web/Infrastructure/CompleteTravelerProfileFilter.cs](../src/SBus.Web/Infrastructure/CompleteTravelerProfileFilter.cs)
- [src/SBus.Web/Pages/Account/CompleteProfile.cshtml](../src/SBus.Web/Pages/Account/CompleteProfile.cshtml)
- [src/SBus.Web/Pages/Account/ExternalLogin.cshtml.cs](../src/SBus.Web/Pages/Account/ExternalLogin.cshtml.cs)
- [src/SBus.Web/Pages/Account/LinkGoogle.cshtml](../src/SBus.Web/Pages/Account/LinkGoogle.cshtml)
- [src/SBus.Web/Pages/Account/LinkGoogle.cshtml.cs](../src/SBus.Web/Pages/Account/LinkGoogle.cshtml.cs)
- [src/SBus.Web/Pages/Account/Login.cshtml](../src/SBus.Web/Pages/Account/Login.cshtml)
- [src/SBus.Web/Pages/Account/Login.cshtml.cs](../src/SBus.Web/Pages/Account/Login.cshtml.cs)
- [src/SBus.Web/Pages/Account/Profile.cshtml](../src/SBus.Web/Pages/Account/Profile.cshtml)
- [src/SBus.Web/Pages/Account/Profile.cshtml.cs](../src/SBus.Web/Pages/Account/Profile.cshtml.cs)
- [src/SBus.Web/Pages/Account/Register.cshtml](../src/SBus.Web/Pages/Account/Register.cshtml)
- [src/SBus.Web/Pages/Account/Register.cshtml.cs](../src/SBus.Web/Pages/Account/Register.cshtml.cs)
- [src/SBus.Web/Pages/MyTickets.cshtml](../src/SBus.Web/Pages/MyTickets.cshtml)
- [src/SBus.Web/Pages/MyTickets.cshtml.cs](../src/SBus.Web/Pages/MyTickets.cshtml.cs)
- [src/SBus.Web/Pages/Shared/_BookingForm.cshtml](../src/SBus.Web/Pages/Shared/_BookingForm.cshtml)
- [src/SBus.Web/Pages/Shared/_BookingPassengerDetails.cshtml](../src/SBus.Web/Pages/Shared/_BookingPassengerDetails.cshtml)
- [src/SBus.Web/Pages/Shared/_Layout.cshtml](../src/SBus.Web/Pages/Shared/_Layout.cshtml)
- [src/SBus.Web/Pages/Shared/_ProfileForm.cshtml](../src/SBus.Web/Pages/Shared/_ProfileForm.cshtml)
- [src/SBus.Web/Pages/Shared/_TravelerProfileFields.cshtml](../src/SBus.Web/Pages/Shared/_TravelerProfileFields.cshtml)
- [src/SBus.Web/Pages/Ticket.cshtml](../src/SBus.Web/Pages/Ticket.cshtml)
- [src/SBus.Web/Pages/Trip.cshtml](../src/SBus.Web/Pages/Trip.cshtml)
- [src/SBus.Web/Pages/Trip.cshtml.cs](../src/SBus.Web/Pages/Trip.cshtml.cs)
- [src/SBus.Web/Resources/SharedResource.en.resx](../src/SBus.Web/Resources/SharedResource.en.resx)
- [src/SBus.Web/wwwroot/js/partial-navigation.js](../src/SBus.Web/wwwroot/js/partial-navigation.js)
- [src/SBus.Web/wwwroot/js/traveler-profile.js](../src/SBus.Web/wwwroot/js/traveler-profile.js)
- [tests/SBus.Application.SubcutaneousTests/Common/WebAppFactory.cs](../tests/SBus.Application.SubcutaneousTests/Common/WebAppFactory.cs)
- [tests/SBus.Application.SubcutaneousTests/Features/Bookings/BookingFlowTests.cs](../tests/SBus.Application.SubcutaneousTests/Features/Bookings/BookingFlowTests.cs)
- [tests/SBus.Application.SubcutaneousTests/Web/BookingPassengerTests.cs](../tests/SBus.Application.SubcutaneousTests/Web/BookingPassengerTests.cs)
- [tests/SBus.Application.SubcutaneousTests/Web/PartialNavigationTests.cs](../tests/SBus.Application.SubcutaneousTests/Web/PartialNavigationTests.cs)
- [tests/SBus.Application.SubcutaneousTests/Web/TravelerAccessTests.cs](../tests/SBus.Application.SubcutaneousTests/Web/TravelerAccessTests.cs)
- [tests/SBus.Application.UnitTests/Bookings/CreateOnlineBookingCommandValidatorTests.cs](../tests/SBus.Application.UnitTests/Bookings/CreateOnlineBookingCommandValidatorTests.cs)
- [tests/SBus.Application.UnitTests/Identity/RegisterTravelerCommandTests.cs](../tests/SBus.Application.UnitTests/Identity/RegisterTravelerCommandTests.cs)
- [tests/SBus.Application.UnitTests/Identity/TravelerAccountsTests.cs](../tests/SBus.Application.UnitTests/Identity/TravelerAccountsTests.cs)
- [tests/SBus.Application.UnitTests/Identity/TravelerProfileTests.cs](../tests/SBus.Application.UnitTests/Identity/TravelerProfileTests.cs)
- [tests/SBus.Web.JavaScriptTests/booking-passenger.test.cjs](../tests/SBus.Web.JavaScriptTests/booking-passenger.test.cjs)
