using AH.Application.Common;
using Grpc.Core;

namespace AH.IdentityService.Services;

// Expected Application failures travel to WebApi as gRPC status codes, which it maps to HTTP statuses.
// The Error.Code rides along in the "error-code" trailer, for callers that branch on it (sign-in errors).
internal static class RpcResults
{
    public const string ErrorCodeTrailer = "error-code";

    public static TReply Unwrap<T, TReply>(this Result<T> result, Func<T, TReply> toReply, ILogger logger, string operation)
    {
        if (result.IsSuccess)
        {
            return toReply(result.Value);
        }

        var error = result.Error!;
        logger.LogWarning("{Operation} failed: {ErrorCode} {ErrorMessage}", operation, error.Code, error.Message);
        throw new RpcException(new Status(ToStatusCode(error.Type), error.Message), new Metadata { { ErrorCodeTrailer, error.Code } });
    }

    public static RpcException InvalidArgument(string message) => new(new Status(StatusCode.InvalidArgument, message));

    private static StatusCode ToStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCode.InvalidArgument,
        ErrorType.NotFound => StatusCode.NotFound,
        ErrorType.Conflict => StatusCode.Aborted,
        ErrorType.Forbidden => StatusCode.PermissionDenied,
        _ => StatusCode.Unknown,
    };
}
