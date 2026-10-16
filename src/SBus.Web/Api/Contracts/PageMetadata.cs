using SBus.Application.Features.MobileAuth;
using SBus.Web.Services;

namespace SBus.Web.Api.Contracts;

public sealed record PageMetadata(int Page, int PageSize, bool HasMore);