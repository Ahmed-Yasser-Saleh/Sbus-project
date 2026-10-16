using SBus.Application.Common.Errors;

namespace SBus.Application.Common.Exceptions;

public sealed class ServiceUnavailableException(string message, string errorCode = ErrorCodes.ApiUnavailable) : ApiException(message, errorCode);