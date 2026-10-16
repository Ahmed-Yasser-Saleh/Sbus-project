using SBus.Application.Common.Errors;

namespace SBus.Application.Common.Exceptions;

public sealed class UnauthenticatedException(string message = "Sign in to continue.", string errorCode = ErrorCodes.Unauthenticated) : ApiException(message, errorCode);