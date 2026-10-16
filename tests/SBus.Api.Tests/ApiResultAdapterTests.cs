using FluentValidation;

using Microsoft.Extensions.Localization;

using NSubstitute;

using SBus.Application.Common.Errors;
using SBus.Application.Common.Exceptions;
using SBus.Domain.Common.Results;
using SBus.Web;
using SBus.Web.Api.Errors;

using Xunit;

namespace SBus.Api.Tests;

public sealed class ApiResultAdapterTests
{
    [Fact]
    public void Success_ReturnsSameDto()
    {
        Assert.Equal("value", Create().Value<string>("value"));
    }

    [Fact]
    public void ResultSeatConflict_UsesStableCode()
    {
        var exception = Assert.Throws<ConflictException>(() => Create().Value<string>(ApplicationErrors.SeatsJustTaken));
        Assert.Equal(ErrorCodes.BookingSeatUnavailable, exception.ErrorCode);
    }

    [Fact]
    public void ResultValidation_PreservesFieldPaths()
    {
        var exception = Assert.Throws<ValidationException>(() => Create().Value<string>(Error.Validation("PhoneNumber", "Invalid phone.")));
        Assert.Equal("phoneNumber", exception.Errors.Single().PropertyName);
    }

    [Theory]
    [InlineData(ErrorKind.NotFound, typeof(NotFoundException))]
    [InlineData(ErrorKind.Unauthorized, typeof(UnauthenticatedException))]
    [InlineData(ErrorKind.Forbidden, typeof(ForbiddenException))]
    [InlineData(ErrorKind.Failure, typeof(BusinessRuleException))]
    [InlineData(ErrorKind.Unexpected, typeof(InvalidOperationException))]
    public void OtherResultKinds_MapToExceptionHierarchy(ErrorKind kind, Type expected)
    {
        var exception = Record.Exception(() => Create().Value<string>(Error.Create((int)kind, "error", "Safe detail.")));
        Assert.IsType(expected, exception);
    }

    private static ApiResultAdapter Create()
    {
        var localizer = Substitute.For<IStringLocalizer<SharedResource>>();
        localizer[Arg.Any<string>(), Arg.Any<object[]>()].Returns(call => new LocalizedString(call.ArgAt<string>(0), call.ArgAt<string>(0)));
        return new(localizer);
    }
}