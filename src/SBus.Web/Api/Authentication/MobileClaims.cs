using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using SBus.Application.Features.MobileAuth;

namespace SBus.Web.Api.Authentication;

public static class MobileClaims
{
    public const string SecurityStampHash = "stamp_hash";
    public const string FamilyId = "session_id";
    public const string Role = "role";
}