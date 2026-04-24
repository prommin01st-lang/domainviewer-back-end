namespace DomainViewer.API.Common;

/// <summary>
/// Result pattern สำหรับ return type ที่ชัดเจน
/// ใช้แทน exceptions สำหรับ business logic errors
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? Error { get; }
    public string? ErrorCode { get; }

    protected Result(bool isSuccess, string? error, string? errorCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result Success() => new(true, null, null);
    public static Result Failure(string error, string? errorCode = null) => new(false, error, errorCode);
    public static Result<T> Success<T>(T value) => new(value, true, null, null);
    public static Result<T> Failure<T>(string error, string? errorCode = null) => new(default, false, error, errorCode);
}

/// <summary>
/// Generic Result with value
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; }

    internal Result(T? value, bool isSuccess, string? error, string? errorCode)
        : base(isSuccess, error, errorCode)
    {
        Value = value;
    }

    public static implicit operator Result<T>(T value) => Success(value);
}

/// <summary>
/// Error codes สำหรับใช้ทั่วทั้งระบบ
/// </summary>
public static class ErrorCodes
{
    public const string NotFound = "NOT_FOUND";
    public const string Duplicate = "DUPLICATE";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string InvalidOperation = "INVALID_OPERATION";
    // Validation removed, use ValidationFailed
    public const string Conflict = "CONFLICT";
    public const string InternalError = "INTERNAL_ERROR";
}
