using SBus.Application.Common.Models;

namespace SBus.Web.Api.Contracts;

public static class ApiPages
{
    public static QueryPage Query(PageRequest page) => new(page.Page, page.PageSize);

    public static PagedResponse<T> Response<T>(List<T> rows, PageRequest page) =>
        new(rows.Take(page.PageSize).ToList(), new PageMetadata(page.Page, page.PageSize, rows.Count > page.PageSize));
}

