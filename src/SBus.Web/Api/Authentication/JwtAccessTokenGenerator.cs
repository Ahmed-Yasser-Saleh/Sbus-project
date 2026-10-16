using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using SBus.Application.Features.MobileAuth;

namespace SBus.Web.Api.Authentication;

public sealed class JwtAccessTokenGenerator(IOptions<MobileAuthOptions> options) : IAccessTokenGenerator
{
    public string Generate(MobileAccount account, Guid familyId, DateTimeOffset issuedAtUtc, DateTimeOffset expiresAtUtc)
    {
        var settings = options.Value;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, account.Id), new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, issuedAtUtc.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
            new(MobileClaims.SecurityStampHash, account.SecurityStampHash), new(MobileClaims.FamilyId, familyId.ToString()),
        };
        claims.AddRange(account.Roles.Select(role => new Claim(MobileClaims.Role, role)));
        var token = new JwtSecurityToken(settings.Issuer, settings.Audience, claims, issuedAtUtc.UtcDateTime, expiresAtUtc.UtcDateTime,
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}