namespace SBus.Application.Features.MobileAuth;

public interface IAccessTokenGenerator
{
    string Generate(MobileAccount account, Guid familyId, DateTimeOffset issuedAtUtc, DateTimeOffset expiresAtUtc);
}