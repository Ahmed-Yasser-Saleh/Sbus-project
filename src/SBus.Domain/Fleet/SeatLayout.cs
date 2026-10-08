using System.Globalization;
using System.Text;

using SBus.Domain.Common;
using SBus.Domain.Common.Constants;
using SBus.Domain.Common.Results;

namespace SBus.Domain.Fleet;

public sealed class SeatLayout : AuditableEntity
{
    public const string EmptyCell = "_";
    public const string DriverCell = "D";
    public const int MaxRows = 20;
    public const int MaxColumns = 6;
    public const int MaxSeats = 60;

    public string Name { get; private set; } = null!;
    public int Rows { get; private set; }
    public int Columns { get; private set; }
    public int? DriverRow { get; private set; }
    public int? DriverColumn { get; private set; }

    private readonly List<LayoutSeat> _seats = [];
    public IEnumerable<LayoutSeat> Seats => _seats.AsReadOnly();

    public int Capacity => _seats.Count;

    private SeatLayout()
    { }

    private SeatLayout(Guid id, string name)
        : base(id)
    {
        Name = name;
    }

    public static Result<SeatLayout> FromGrid(Guid id, string name, string grid)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return SeatLayoutErrors.NameRequired;
        }

        if (name.Trim().Length > SBusConstants.NameMaxLength)
        {
            return SeatLayoutErrors.NameTooLong;
        }

        if (string.IsNullOrWhiteSpace(grid))
        {
            return SeatLayoutErrors.GridRequired;
        }

        var lines = grid
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0)
            .ToList();

        if (lines.Count > MaxRows)
        {
            return SeatLayoutErrors.TooManyRows;
        }

        var layout = new SeatLayout(id, name.Trim());
        var seen = new HashSet<int>();

        for (var row = 0; row < lines.Count; row++)
        {
            var cells = lines[row].Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (cells.Length > MaxColumns)
            {
                return SeatLayoutErrors.TooManyColumns(row + 1);
            }

            layout.Columns = Math.Max(layout.Columns, cells.Length);

            for (var column = 0; column < cells.Length; column++)
            {
                var cell = cells[column];

                if (cell == EmptyCell)
                {
                    continue;
                }

                if (string.Equals(cell, DriverCell, StringComparison.OrdinalIgnoreCase))
                {
                    if (layout.DriverRow is not null)
                    {
                        return SeatLayoutErrors.MultipleDrivers;
                    }

                    layout.DriverRow = row + 1;
                    layout.DriverColumn = column + 1;
                    continue;
                }

                if (!int.TryParse(cell, NumberStyles.None, CultureInfo.InvariantCulture, out var seatNumber) || seatNumber <= 0)
                {
                    return SeatLayoutErrors.InvalidCell(cell, row + 1);
                }

                if (!seen.Add(seatNumber))
                {
                    return SeatLayoutErrors.DuplicateSeat(seatNumber);
                }

                layout._seats.Add(new LayoutSeat(Guid.CreateVersion7(), layout.Id, seatNumber, row + 1, column + 1));
            }
        }

        if (layout._seats.Count == 0)
        {
            return SeatLayoutErrors.NoSeats;
        }

        if (layout._seats.Count > MaxSeats)
        {
            return SeatLayoutErrors.TooManySeats;
        }

        layout.Rows = lines.Count;

        return layout;
    }

    public bool HasSeat(int seatNumber) => _seats.Any(s => s.SeatNumber == seatNumber);

    public string ToGrid()
    {
        var builder = new StringBuilder();

        for (var row = 1; row <= Rows; row++)
        {
            var cells = new List<string>();

            for (var column = 1; column <= Columns; column++)
            {
                if (DriverRow == row && DriverColumn == column)
                {
                    cells.Add(DriverCell);
                    continue;
                }

                var seat = _seats.FirstOrDefault(s => s.Row == row && s.Column == column);
                cells.Add(seat is null ? EmptyCell : seat.SeatNumber.ToString(CultureInfo.InvariantCulture));
            }

            builder.AppendLine(string.Join(' ', cells));
        }

        return builder.ToString().TrimEnd();
    }
}
