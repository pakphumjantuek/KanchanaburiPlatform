namespace Application.Results;

public enum ServiceStatus { Success, NotFound, Forbidden, Conflict, Invalid, Failed }

public sealed record ServiceResult<T>(ServiceStatus Status, T? Data = default, string? Error = null)
{
    public static ServiceResult<T> Success(T data) => new(ServiceStatus.Success, data);
    public static ServiceResult<T> Failure(ServiceStatus status, string? error = null) => new(status, default, error);
}
