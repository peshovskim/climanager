using System.Net;
using System.Net.Http;
using Google;
using SharedKernel;

namespace CliManager.Application.Common;

internal static class DriveCommandResults
{
    public static Result FromException(Exception exception)
    {
        ResultError error = MapError(exception);

        return error.Type switch
        {
            ResultType.Invalid => Result.Invalid(error.Code, error.Message),
            ResultType.NotFound => Result.NotFound(error.Code, error.Message),
            ResultType.Conflicted => Result.Conflicted(error.Code, error.Message),
            ResultType.Forbidden => Result.Forbidden(error.Code, error.Message),
            ResultType.Unauthorized => Result.Unauthorized(error.Code, error.Message),
            _ => Result.InternalError(error.Code, error.Message),
        };
    }

    public static Result<T> FromException<T>(Exception exception)
    {
        ResultError error = MapError(exception);

        return error.Type switch
        {
            ResultType.Invalid => Result<T>.Invalid(error.Code, error.Message),
            ResultType.NotFound => Result<T>.NotFound(error.Code, error.Message),
            ResultType.Conflicted => Result<T>.Conflicted(error.Code, error.Message),
            ResultType.Forbidden => Result<T>.Forbidden(error.Code, error.Message),
            ResultType.Unauthorized => Result<T>.Unauthorized(error.Code, error.Message),
            _ => Result<T>.InternalError(error.Code, error.Message),
        };
    }

    private static ResultError MapError(Exception exception) =>
        exception switch
        {
            GoogleApiException googleApi => MapGoogleApiException(googleApi),
            FileNotFoundException fileNotFound => new(
                ResultType.NotFound,
                ResultCodes.NotFound,
                fileNotFound.Message),
            DirectoryNotFoundException directoryNotFound => new(
                ResultType.NotFound,
                ResultCodes.NotFound,
                directoryNotFound.Message),
            ArgumentException argument => new(
                ResultType.Invalid,
                ResultCodes.Validation,
                argument.Message),
            HttpRequestException => NetworkError(),
            _ => exception.InnerException is not null
                ? MapError(exception.InnerException)
                : new(ResultType.InternalError, ResultCodes.InternalError, exception.Message),
        };

    private static ResultError MapGoogleApiException(GoogleApiException exception) =>
        exception.HttpStatusCode switch
        {
            HttpStatusCode.TooManyRequests => new(
                ResultType.Conflicted,
                ResultCodes.RateLimited,
                "Google Drive rate limit exceeded. Wait a moment and try again."),
            HttpStatusCode.Unauthorized => new(
                ResultType.Unauthorized,
                ResultCodes.Unauthorized,
                "Google Drive authentication failed or expired. Run 'auth' again."),
            HttpStatusCode.Forbidden => new(
                ResultType.Forbidden,
                ResultCodes.Forbidden,
                "Google Drive denied access to this resource."),
            HttpStatusCode.NotFound => new(
                ResultType.NotFound,
                ResultCodes.NotFound,
                "The requested file or folder was not found on Google Drive."),
            HttpStatusCode.BadRequest => new(
                ResultType.Invalid,
                ResultCodes.Validation,
                $"Google Drive rejected the request: {exception.Message}"),
            HttpStatusCode.RequestTimeout
                or HttpStatusCode.GatewayTimeout
                or HttpStatusCode.ServiceUnavailable
                or HttpStatusCode.BadGateway => NetworkError(),
            _ => new(
                ResultType.InternalError,
                ResultCodes.InternalError,
                $"Google Drive API error ({(int)exception.HttpStatusCode}): {exception.Message}"),
        };

    private static ResultError NetworkError() =>
        new(
            ResultType.InternalError,
            ResultCodes.Network,
            "A network error occurred while contacting Google Drive. Check your connection and try again.");
}
