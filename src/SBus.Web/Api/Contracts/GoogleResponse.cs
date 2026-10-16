using SBus.Application.Features.MobileAuth;
using SBus.Web.Services;

namespace SBus.Web.Api.Contracts;

public sealed record GoogleResponse(string State, SessionResponse? Session, string? LinkingProof);