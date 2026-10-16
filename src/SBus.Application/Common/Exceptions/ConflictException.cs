using SBus.Application.Common.Errors;

namespace SBus.Application.Common.Exceptions;

public sealed class ConflictException(string message, string errorCode = ErrorCodes.Conflict) : ApiException(message, errorCode);