namespace SBus.Application.Common.Models;

/// <summary>Optional bounded database paging; null preserves existing unpaged Razor queries.</summary>
public sealed record QueryPage(int Page, int PageSize)
{
    public IQueryable<T> Apply<T>(IQueryable<T> source)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(Page, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Page, 10000);
        ArgumentOutOfRangeException.ThrowIfLessThan(PageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(PageSize, 100);
        return source.Skip((Page - 1) * PageSize).Take(PageSize + 1);
    }
}

