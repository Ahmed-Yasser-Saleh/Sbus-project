using SBus.Application.Features.MobileAuth;
using SBus.Web.Services;

namespace SBus.Web.Api.Contracts;

public sealed record PagedResponse<T>(IReadOnlyList<T> Data, PageMetadata Pagination);