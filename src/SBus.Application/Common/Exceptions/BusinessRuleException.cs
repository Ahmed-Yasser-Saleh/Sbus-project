using SBus.Application.Common.Errors;

namespace SBus.Application.Common.Exceptions;

public sealed class BusinessRuleException(string message, string errorCode = ErrorCodes.BusinessRule) : ApiException(message, errorCode);