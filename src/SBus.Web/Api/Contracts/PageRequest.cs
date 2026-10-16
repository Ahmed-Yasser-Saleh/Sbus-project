using System.ComponentModel.DataAnnotations;

namespace SBus.Web.Api.Contracts;

public sealed class PageRequest
{
    [Range(1, 10000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}