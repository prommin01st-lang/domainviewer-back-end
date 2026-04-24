using System.Text.Json.Serialization;

namespace DomainViewer.API.Common;

/// <summary>
/// Standard API response wrapper
/// </summary>
public class ApiResponse<T>
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message
    };

    public static ApiResponse<T> Fail(string? message, string? errorCode = null) => new()
    {
        Success = false,
        Message = message ?? "Unknown Error",
        ErrorCode = errorCode
    };

    public static ApiResponse<T> FromResult(Result<T> result, string? successMessage = null) =>
        result.IsSuccess
            ? Ok(result.Value!, successMessage)
            : Fail(result.Error, result.ErrorCode);
}

/// <summary>
/// Non-generic version for simple responses
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse Ok(string? message = null) => new()
    {
        Success = true,
        Message = message
    };

    public new static ApiResponse Fail(string? message, string? errorCode = null) => new()
    {
        Success = false,
        Message = message ?? "Unknown Error",
        ErrorCode = errorCode
    };

    public static ApiResponse FromResult(Result result, string? successMessage = null) =>
        result.IsSuccess
            ? Ok(successMessage)
            : Fail(result.Error, result.ErrorCode);
}
