namespace SBus.Application.Features.Bookings.Dtos;

public sealed record ReceiptFileDto(Stream Content, string ContentType, string FileName);
