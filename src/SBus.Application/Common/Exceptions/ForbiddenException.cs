using SBus.Application.Common.Errors;

namespace SBus.Application.Common.Exceptions;

public sealed class ForbiddenException(string message = "You cannot access this resource.", string errorCode = ErrorCodes.Forbidden) : ApiException(message, errorCode);