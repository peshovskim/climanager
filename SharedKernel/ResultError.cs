namespace SharedKernel;

public sealed record ResultError(ResultType Type, string Code, string Message);
