using System.Text.RegularExpressions;

namespace SBus.Application.Common.Validation;

public static partial class PublicEmailAddress
{
    public const int MaxLength = 254;
    public const string Pattern = @"^[a-zA-Z0-9!#$%&'*+\/=?^_\x60\{\|\}~\-]+(?:\.[a-zA-Z0-9!#$%&'*+\/=?^_\x60\{\|\}~\-]+)*@(?:[a-zA-Z0-9](?:[a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+(?:[a-zA-Z]{2,63}|xn--[a-zA-Z0-9](?:[a-zA-Z0-9\-]{0,57}[a-zA-Z0-9])?)$";

    public static bool IsValid(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Length <= MaxLength
        && value.IndexOf('@') is > 0 and <= 64
        && AddressPattern().Match(value) is { Success: true } match
        && match.Length == value.Length;

    [GeneratedRegex(Pattern, RegexOptions.CultureInvariant, 100)]
    private static partial Regex AddressPattern();
}

