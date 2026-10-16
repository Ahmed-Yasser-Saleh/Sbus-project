using MediatR;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SBus.Application.Common.Interfaces;
using SBus.Application.Common.Security;
using SBus.Application.Common.Settings;
using SBus.Application.Common.Validation;
using SBus.Application.Features.Accounts.Commands.UpdateTravelerProfile;
using SBus.Application.Features.Bookings.Common;
using SBus.Application.Features.Bookings.Dtos;
using SBus.Domain.Bookings;
using SBus.Domain.Common.Results;

namespace SBus.Application.Features.Bookings.Commands.CreateOnlineBooking;

public class CreateOnlineBookingCommandHandler(
    ILogger<CreateOnlineBookingCommandHandler> logger,
    IAppDbContext context,
    TimeProvider timeProvider,
    IOptions<BookingOptions> options,
    IUser user,
    ITravelerProfiles profiles,
    IBookingTransaction transaction)
    : IRequestHandler<CreateOnlineBookingCommand, Result<BookingCreatedDto>>
{
    private readonly ILogger<CreateOnlineBookingCommandHandler> _logger = logger;
    private readonly IAppDbContext _context = context;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly BookingOptions _options = options.Value;

    public async Task<Result<BookingCreatedDto>> Handle(CreateOnlineBookingCommand command, CancellationToken ct)
    {
        var profile = await profiles.GetCurrentAsync(ct);
        if (!user.IsTraveler || user.Id is null || profile?.IsComplete != true)
        {
            return Error.Validation(string.Empty, "كمّل بياناتك قبل الحجز.") with { ReasonCode = SBus.Application.Common.Errors.ErrorCodes.ProfileIncomplete };
        }

        var passengerName = string.IsNullOrWhiteSpace(command.PassengerName) ? profile.FullName : command.PassengerName;
        var phoneNumber = string.IsNullOrWhiteSpace(command.PhoneNumber) ? profile.PhoneNumber : command.PhoneNumber;
        var validation = await new UpdateTravelerProfileCommandValidator()
            .ValidateAsync(new UpdateTravelerProfileCommand(passengerName, phoneNumber), ct);
        if (!validation.IsValid)
        {
            return validation.Errors.ConvertAll(e => Error.Validation(
                e.PropertyName == nameof(UpdateTravelerProfileCommand.FullName) ? nameof(command.PassengerName) : e.PropertyName,
                e.ErrorMessage));
        }

        passengerName = passengerName!.Trim();
        phoneNumber = TravelerProfileRules.NormalizePhone(phoneNumber)!;
        return command.UpdateAccountDetails
            ? await transaction.ExecuteAsync(() => PlaceAsync(command, passengerName, phoneNumber, ct), result => result.IsSuccess, ct)
            : await PlaceAsync(command, passengerName, phoneNumber, ct);
    }

    private async Task<Result<BookingCreatedDto>> PlaceAsync(
        CreateOnlineBookingCommand command, string passengerName, string phoneNumber, CancellationToken ct)
    {
        var nowUtc = _timeProvider.GetUtcNow();

        var prepared = await BookingPlacement.PrepareAsync(_context, command, nowUtc, requireActiveSchedule: true, ct);

        if (prepared.IsError)
        {
            return prepared.Errors;
        }

        var booking = Booking.CreateHold(
            Guid.CreateVersion7(),
            command.TripId,
            PublicToken.Create(),
            passengerName,
            phoneNumber,
            command.PickupStopId,
            command.DropoffStopId,
            command.SeatNumbers,
            prepared.Value.Price,
            nowUtc,
            _options.HoldDuration);

        if (booking.IsError)
        {
            return booking.Errors;
        }

        if (user.Id is not null)
        {
            booking.Value.AssignTraveler(user.Id);
        }

        if (command.UpdateAccountDetails)
        {
            var updated = await profiles.UpdateCurrentAsync(passengerName, phoneNumber, ct);
            if (updated.IsError)
            {
                return updated.Errors;
            }
        }

        var saved = await BookingPlacement.SaveAsync(_context, booking.Value, ct);

        if (saved.IsSuccess)
        {
            _logger.LogInformation("Online hold created. Booking: {BookingId}, Trip: {TripId}, Seats: {SeatCount}", booking.Value.Id, command.TripId, command.SeatNumbers.Count);
        }

        return saved;
    }
}
