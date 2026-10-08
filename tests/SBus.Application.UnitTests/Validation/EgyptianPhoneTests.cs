using SBus.Application.Common.Validation;

using Xunit;

namespace SBus.Application.UnitTests.Validation;

public class EgyptianPhoneTests
{
    [Theory]
    [InlineData("01012345678", "01012345678")]
    [InlineData("010 1234 5678", "01012345678")]
    [InlineData("+201012345678", "01012345678")]
    [InlineData("00201512345678", "01512345678")]
    [InlineData("٠١٢٣٤٥٦٧٨٩٠", "01234567890")]
    [InlineData("011-2345-6789", "01123456789")]
    public void Normalize_ValidMobile_ReturnsLocalFormat(string input, string expected)
    {
        Assert.Equal(expected, EgyptianPhone.Normalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0101234567")]
    [InlineData("01312345678")]
    [InlineData("0221234567")]
    [InlineData("01012345678x")]
    public void Normalize_Invalid_ReturnsNull(string? input)
    {
        Assert.Null(EgyptianPhone.Normalize(input));
    }
}
