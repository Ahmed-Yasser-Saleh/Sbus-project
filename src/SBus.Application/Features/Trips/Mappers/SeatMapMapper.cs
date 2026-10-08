using SBus.Application.Features.Trips.Dtos;
using SBus.Domain.Fleet;

namespace SBus.Application.Features.Trips.Mappers;

public static class SeatMapMapper
{
    public static SeatMapDto ToSeatMap(this SeatLayout layout, IReadOnlySet<int> takenSeats)
    {
        ArgumentNullException.ThrowIfNull(layout);

        var cells = layout.Seats
            .Select(s => new SeatCellDto(s.Row, s.Column, SeatCellKind.Seat, s.SeatNumber, takenSeats.Contains(s.SeatNumber)))
            .ToList();

        if (layout.DriverRow is int row && layout.DriverColumn is int column)
        {
            cells.Add(new SeatCellDto(row, column, SeatCellKind.Driver, null, true));
        }

        return new SeatMapDto(layout.Rows, layout.Columns, cells);
    }
}
