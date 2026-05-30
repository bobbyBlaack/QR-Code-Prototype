namespace QR_Code_Prototype.Contracts.Common;

public sealed class ApiResult<T>
{
    private ApiResult(T? value, ApiErrorResponse? error, int statusCode)
    {
        Value = value;
        Error = error;
        StatusCode = statusCode;
    }

    public T? Value { get; }
    public ApiErrorResponse? Error { get; }
    public int StatusCode { get; }
    public bool IsSuccess => Error is null;

    public static ApiResult<T> Success(T value, int statusCode = StatusCodes.Status200OK) => new(value, null, statusCode);
    public static ApiResult<T> Failure(string code, string message, int statusCode, object? details = null) =>
        new(default, new ApiErrorResponse(code, message, details), statusCode);
}
