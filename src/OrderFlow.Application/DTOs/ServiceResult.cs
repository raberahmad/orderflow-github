namespace OrderFlow.Application.DTOs;

public record ServiceResult<T>
{
    public bool Succeeded { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public int StatusCode { get; init; }

    public static ServiceResult<T> Ok(T value) =>
        new() { Succeeded = true, Value = value, StatusCode = 200 };

    public static ServiceResult<T> Created(T value) =>
        new() { Succeeded = true, Value = value, StatusCode = 201 };

    public static ServiceResult<T> NotFound(string error) =>
        new() { Succeeded = false, Error = error, StatusCode = 404 };

    public static ServiceResult<T> BadRequest(string error) =>
        new() { Succeeded = false, Error = error, StatusCode = 400 };

    public static ServiceResult<T> Conflict(string error) =>
        new() { Succeeded = false, Error = error, StatusCode = 409 };
}
