using SBus.Application.Common.Validation;

using Xunit;

namespace SBus.Application.UnitTests.Validation;

public class PublicEmailAddressTests
{
    [Theory]
    [InlineData("test@gmi")]
    [InlineData("test@")]
    [InlineData("test@gmail.")]
    [InlineData("test@.com")]
    [InlineData("test@-gmail.com")]
    [InlineData("test@gmail-.com")]
    [InlineData("test@gmail..com")]
    [InlineData(".test@gmail.com")]
    [InlineData("test..name@gmail.com")]
    [InlineData("test@gmail.com\n")]
    [InlineData("test name@gmail.com")]
    [InlineData("test@gmail.123")]
    [InlineData("Name <test@gmail.com>")]
    [InlineData("")]
    [InlineData(null)]
    public void MalformedPublicAddress_IsRejected(string? email)
    {
        Assert.False(PublicEmailAddress.IsValid(email));
    }

    [Theory]
    [InlineData("test@gmail.com")]
    [InlineData("first.last+trip@example.co.uk")]
    [InlineData("test@sub.example.com")]
    [InlineData("TEST@EXAMPLE.COM")]
    [InlineData("test@xn--mgbh0fb.xn--kgbechtv")]
    public void ValidPublicAddress_IsAccepted(string email)
    {
        Assert.True(PublicEmailAddress.IsValid(email));
    }

    [Fact]
    public void LocalPartLongerThan64Characters_IsRejected()
    {
        Assert.False(PublicEmailAddress.IsValid(new string('a', 65) + "@example.com"));
    }
}

