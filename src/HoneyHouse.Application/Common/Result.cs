namespace HoneyHouse.Application.Common;

public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    BusinessRule
}

/// <summary>An error identified by a stable code; the message is localized by the API layer.</summary>
public sealed record Error(string Code, ErrorType Type, string? Field = null, object[]? Args = null)
{
    public static Error Validation(string code, string? field = null, params object[] args) => new(code, ErrorType.Validation, field, args);
    public static Error NotFound(string code, params object[] args) => new(code, ErrorType.NotFound, null, args);
    public static Error Conflict(string code, params object[] args) => new(code, ErrorType.Conflict, null, args);
    public static Error Unauthorized(string code) => new(code, ErrorType.Unauthorized);
    public static Error Forbidden(string code) => new(code, ErrorType.Forbidden);
    public static Error Business(string code, params object[] args) => new(code, ErrorType.BusinessRule, null, args);
}

public class Result
{
    protected Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<Error> Errors { get; }
    public Error? FirstError => Errors.Count > 0 ? Errors[0] : null;

    public static Result Success() => new(true, []);
    public static Result Failure(Error error) => new(false, [error]);
    public static Result Failure(IReadOnlyList<Error> errors) => new(false, errors);

    public static implicit operator Result(Error error) => Failure(error);
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T value) : base(true, []) => _value = value;
    private Result(IReadOnlyList<Error> errors) : base(false, errors) { }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public static Result<T> Success(T value) => new(value);
    public static new Result<T> Failure(Error error) => new([error]);
    public static new Result<T> Failure(IReadOnlyList<Error> errors) => new(errors);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}
