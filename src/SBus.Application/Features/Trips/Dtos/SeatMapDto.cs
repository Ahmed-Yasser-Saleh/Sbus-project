namespace SBus.Application.Features.Trips.Dtos;

public sealed record SeatMapDto(int Rows, int Columns, IReadOnlyList<SeatCellDto> Cells)
{
    public int Capacity => Cells.Count(c => c.Kind == SeatCellKind.Seat);

    public int Available => Cells.Count(c => c.Kind == SeatCellKind.Seat && !c.IsTaken);

    public SeatCellDto? At(int row, int column) => Cells.FirstOrDefault(c => c.Row == row && c.Column == column);
}
