using System.Text;
using System.Text.RegularExpressions;

namespace SBus.Application.Common.Validation;

public static partial class EgyptianPhone
{
    public static string? Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        var digits = new StringBuilder(input.Length);

        foreach (var c in input)
        {
            if (c is >= '0' and <= '9')
            {
                digits.Append(c);
            }
            else if (c is >= '٠' and <= '٩')
            {
                digits.Append((char)('0' + (c - '٠')));
            }
            else if (c is >= '۰' and <= '۹')
            {
                digits.Append((char)('0' + (c - '۰')));
            }
            else if (c is not (' ' or '-' or '+' or '(' or ')'))
            {
                return null;
            }
        }

        var number = digits.ToString();

        if (number.StartsWith("0020", StringComparison.Ordinal))
        {
            number = "0" + number[4..];
        }
        else if (number.StartsWith("20", StringComparison.Ordinal) && number.Length == 12)
        {
            number = "0" + number[2..];
        }

        return MobilePattern().IsMatch(number) ? number : null;
    }

    [GeneratedRegex("^01[0125][0-9]{8}$")]
    private static partial Regex MobilePattern();
}
