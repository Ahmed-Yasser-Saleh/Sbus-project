using SBus.Application.Common.Errors;

namespace SBus.Application.Common.Exceptions;

public abstract class ApiException(string message, string errorCode) : Exception(message)
{
    public string ErrorCode { get; } = errorCode;
}