using AH.Application.Common;
using AH.Application.Users;
using AH.Application.Users.GetOrCreateUser;
using AH.Application.Users.GetUser;
using AH.Application.Users.UpdateUserProfile;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace AH.IdentityService.Services;

public sealed class RpcUserService(
    GetOrCreateUserHandler getOrCreateUser,
    GetUserHandler getUser,
    UpdateUserProfileHandler updateUserProfile,
    ILogger<RpcUserService> logger)
    : UserServiceRpc.UserServiceRpcBase
{
    public override async Task<GetUserReply> GetOrCreateUser(GetUserRequest request, ServerCallContext context)
    {
        var command = new GetOrCreateUserCommand(request.Email, request.FirstName, request.LastName ?? string.Empty, request.AvatarUrl);

        return Unwrap(await getOrCreateUser.HandleAsync(command, context.CancellationToken), nameof(GetOrCreateUser));
    }

    public override async Task<GetUserReply> GetUser(GetUserByIdRequest request, ServerCallContext context) =>
        Unwrap(await getUser.HandleAsync(new GetUserQuery(request.Id), context.CancellationToken), nameof(GetUser));

    public override async Task<GetUserReply> UpdateUserProfile(UpdateUserProfileRequest request, ServerCallContext context)
    {
        var command = new UpdateUserProfileCommand(request.Id, request.FirstName, request.LastName);

        return Unwrap(await updateUserProfile.HandleAsync(command, context.CancellationToken), nameof(UpdateUserProfile));
    }

    private GetUserReply Unwrap(Result<UserResponse> result, string operation)
    {
        if (result.IsSuccess)
        {
            return ToReply(result.Value);
        }

        var error = result.Error!;
        logger.LogWarning("{Operation} failed: {ErrorCode} {ErrorMessage}", operation, error.Code, error.Message);
        throw new RpcException(new Status(ToStatusCode(error.Type), error.Message));
    }

    private static GetUserReply ToReply(UserResponse user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        CreatedAt = Timestamp.FromDateTimeOffset(user.CreatedAt),
        AvatarUrl = user.AvatarUrl,
        PreferredLanguage = user.PreferredLanguage,
    };

    private static StatusCode ToStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCode.InvalidArgument,
        ErrorType.NotFound => StatusCode.NotFound,
        ErrorType.Conflict => StatusCode.Aborted,
        _ => StatusCode.Unknown,
    };
}
