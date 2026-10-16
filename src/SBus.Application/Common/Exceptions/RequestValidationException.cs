using SBus.Application.Common.Errors;

namespace SBus.Application.Common.Exceptions;

public sealed class RequestValidationException(IReadOnlyDictionary<string, string[]> errors, string errorCode = ErrorCodes.ValidationFailed)
    : ApiException("One or more validation errors occurred.", errorCode)
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}