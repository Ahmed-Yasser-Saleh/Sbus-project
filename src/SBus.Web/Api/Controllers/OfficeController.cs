using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SBus.Application.Common.Security;
using SBus.Application.Common.Time;
using SBus.Application.Features.Bookings.Commands.CancelBookingByOffice;
using SBus.Application.Features.Bookings.Commands.ConfirmPayment;
using SBus.Application.Features.Bookings.Commands.CreateOfficeBooking;
using SBus.Application.Features.Bookings.Commands.RejectPayment;
using SBus.Application.Features.Bookings.Dtos;
using SBus.Application.Features.Bookings.Queries.GetPendingPayments;
using SBus.Application.Features.Bookings.Queries.GetReceipt;
using SBus.Application.Features.Trips.Commands.UpdateTripAssignment;
using SBus.Application.Features.Trips.Dtos;
using SBus.Application.Features.Trips.Queries.GetOfficeDay;
using SBus.Application.Features.Trips.Queries.GetTripManifest;
using SBus.Domain.Common.Results;
using SBus.Web.Api.Contracts;

namespace SBus.Web.Api.Controllers;

[ApiController]
[Route("api/v1/office")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = AuthPolicies.LegacyOffice)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType(typeof(ValidationProblemDetails), 400)]
[ProducesResponseType(typeof(ProblemDetails), 401)]
[ProducesResponseType(typeof(ProblemDetails), 403)]
[ProducesResponseType(typeof(ProblemDetails), 404)]
[ProducesResponseType(typeof(ProblemDetails), 409)]
[ProducesResponseType(typeof(ProblemDetails), 500)]
[ProducesResponseType(typeof(ProblemDetails), 503)]
public sealed class OfficeController(ApiSender sender, TimeProvider clock) : ControllerBase
{
    [HttpGet("day")]
    [ProducesResponseType(typeof(ApiResponse<OfficeDayResponse>), 200)]
    public async Task<ApiResponse<OfficeDayResponse>> Day([FromQuery] DateOnly? date, CancellationToken ct)
    {
        var today = CairoTime.Today(clock.GetUtcNow());
        var selected = date ?? today;
        var trips = await sender.Send(new GetOfficeDayQuery(selected), ct);
        var pending = await sender.Send(new GetPendingPaymentsQuery(), ct);
        return new(new(selected, today, trips, pending.Count));
    }

    [HttpGet("trips/{id:guid}/manifest")]
    [ProducesResponseType(typeof(ApiResponse<TripManifestDto>), 200)]
    public async Task<ApiResponse<TripManifestDto>> Manifest(Guid id, CancellationToken ct) =>
        new(await sender.Send(new GetTripManifestQuery(id), ct));

    [HttpPost("trips/{id:guid}/bookings")]
    [ProducesResponseType(typeof(ApiResponse<BookingCreatedDto>), 200)]
    public async Task<ApiResponse<BookingCreatedDto>> Book(Guid id, OfficeBookingRequest request, CancellationToken ct) =>
        new(await sender.Send(new CreateOfficeBookingCommand(id, request.PassengerName, request.PhoneNumber, request.PickupStopId, request.DropoffStopId, request.SeatNumbers), ct));

    [HttpPut("trips/{id:guid}/assignment")]
    [ProducesResponseType(typeof(ApiResponse<Updated>), 200)]
    public async Task<ApiResponse<Updated>> Assign(Guid id, TripAssignmentRequest request, CancellationToken ct) =>
        new(await sender.Send(new UpdateTripAssignmentCommand(id, request.BusId, request.DriverId), ct));

    [HttpPost("bookings/{id:guid}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<Updated>), 200)]
    public async Task<ApiResponse<Updated>> Cancel(Guid id, CancellationToken ct) =>
        new(await sender.Send(new CancelBookingByOfficeCommand(id), ct));

    [HttpGet("payments")]
    [ProducesResponseType(typeof(PagedResponse<PendingPaymentDto>), 200)]
    public async Task<PagedResponse<PendingPaymentDto>> Payments([FromQuery] PageRequest pagination, CancellationToken ct) =>
        ApiPages.Response(await sender.Send(new GetPendingPaymentsQuery(ApiPages.Query(pagination)), ct), pagination);

    [HttpPost("bookings/{id:guid}/payment/confirm")]
    [ProducesResponseType(typeof(ApiResponse<Updated>), 200)]
    public async Task<ApiResponse<Updated>> Confirm(Guid id, CancellationToken ct) =>
        new(await sender.Send(new ConfirmPaymentCommand(id), ct));

    [HttpPost("bookings/{id:guid}/payment/reject")]
    [ProducesResponseType(typeof(ApiResponse<Updated>), 200)]
    public async Task<ApiResponse<Updated>> Reject(Guid id, RejectPaymentRequest request, CancellationToken ct) =>
        new(await sender.Send(new RejectPaymentCommand(id, request.Reason), ct));

    [HttpGet("bookings/{id:guid}/receipt")]
    [ProducesResponseType(typeof(FileStreamResult), 200)]
    public async Task<IActionResult> Receipt(Guid id, CancellationToken ct)
    {
        var receipt = await sender.Send(new GetReceiptQuery(id), ct);
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(receipt.Content, receipt.ContentType);
    }
}

