using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using GrpcStatusCode = Grpc.Core.StatusCode;

namespace AH.WebApi.Controllers;

// IdentityService reports expected failures as gRPC status codes (see RpcResults there); these become
// ProblemDetails with the matching HTTP status. Anything else is unexpected and left to the global handler.
public static class RpcProblems
{
    public static bool IsExpected(RpcException ex) => ToHttpStatus(ex.StatusCode) is not null;

    public static ObjectResult ToProblem(this ControllerBase controller, RpcException ex, string title) =>
        controller.Problem(
            title: title,
            detail: ex.Status.Detail,
            statusCode: ToHttpStatus(ex.StatusCode) ?? StatusCodes.Status500InternalServerError);

    private static int? ToHttpStatus(GrpcStatusCode code) => code switch
    {
        GrpcStatusCode.InvalidArgument => StatusCodes.Status400BadRequest,
        GrpcStatusCode.PermissionDenied => StatusCodes.Status403Forbidden,
        GrpcStatusCode.NotFound => StatusCodes.Status404NotFound,
        GrpcStatusCode.Aborted or GrpcStatusCode.AlreadyExists => StatusCodes.Status409Conflict,
        _ => null,
    };
}
