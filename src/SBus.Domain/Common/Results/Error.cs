using System.Globalization;

namespace SBus.Domain.Common.Results;

public readonly record struct Error
{
    private readonly object[]? _args;

    private Error(string code, string template, ErrorKind type, object[]? args)
    {
        Code = code;
        Template = template;
        Type = type;
        _args = args is { Length: > 0 } ? args : null;
    }

    public string Code { get; }

    public string Template { get; }

    public IReadOnlyList<object> Args => _args ?? [];

    public string Description => _args is null
        ? Template
        : string.Format(CultureInfo.InvariantCulture, Template, _args);

    public ErrorKind Type { get; }

    public static Error Failure(string code = nameof(Failure), string description = "General failure.", params object[] args)
        => new(code, description, ErrorKind.Failure, args);

    public static Error Unexpected(string code = nameof(Unexpected), string description = "Unexpected error.", params object[] args)
        => new(code, description, ErrorKind.Unexpected, args);

    public static Error Validation(string code = nameof(Validation), string description = "Validation error", params object[] args)
        => new(code, description, ErrorKind.Validation, args);

    public static Error Conflict(string code = nameof(Conflict), string description = "Conflict error", params object[] args)
        => new(code, description, ErrorKind.Conflict, args);

    public static Error NotFound(string code = nameof(NotFound), string description = "Not found error", params object[] args)
        => new(code, description, ErrorKind.NotFound, args);

    public static Error Unauthorized(string code = nameof(Unauthorized), string description = "Unauthorized error", params object[] args)
        => new(code, description, ErrorKind.Unauthorized, args);

    public static Error Forbidden(string code = nameof(Forbidden), string description = "Forbidden error", params object[] args)
        => new(code, description, ErrorKind.Forbidden, args);

    public static Error Create(int type, string code, string description)
        => new(code, description, (ErrorKind)type, null);
}
