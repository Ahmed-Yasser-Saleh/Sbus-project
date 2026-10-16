using SBus.Application.Common.Errors;

namespace SBus.Application.Common.Exceptions;

public sealed class NotFoundException(string message, string errorCode = ErrorCodes.NotFound) : ApiException(message, errorCode);