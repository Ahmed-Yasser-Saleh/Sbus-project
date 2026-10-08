using System.Buffers.Text;
using System.Security.Cryptography;

namespace SBus.Application.Common.Security;

public static class PublicToken
{
    public const int Length = 32;

    public static string Create() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(24));
}
