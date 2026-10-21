using AH.Application.Users;
using AH.Application.Users.GetOrCreateUser;
using AH.Application.Users.GetUser;
using AH.Application.Users.UpdateUserProfile;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using DomainSignInMethod = AH.AppDomain.Users.SignInMethod;

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
        var command = new GetOrCreateUserCommand(
            request.Email,
            request.FirstName,
            request.LastName ?? string.Empty,
            request.AvatarUrl,
            ToDomain(request.SignInMethod),
            request.CertificateThumbprint);

        return (await getOrCreateUser.HandleAsync(command, context.CancellationToken)).Unwrap(ToReply, logger, nameof(GetOrCreateUser));
    }

    public override async Task<GetUserReply> GetUser(GetUserByIdRequest request, ServerCallContext context) =>
        (await getUser.HandleAsync(new GetUserQuery(request.Id), context.CancellationToken)).Unwrap(ToReply, logger, nameof(GetUser));

    public override async Task<GetUserReply> UpdateUserProfile(UpdateUserProfileRequest request, ServerCallContext context)
    {
        var command = new UpdateUserProfileCommand(request.Id, request.FirstName, request.LastName);

        return (await updateUserProfile.HandleAsync(command, context.CancellationToken)).Unwrap(ToReply, logger, nameof(UpdateUserProfile));
    }

    private static DomainSignInMethod ToDomain(SignInMethod method) => method switch
    {
        SignInMethod.External => DomainSignInMethod.External,
        SignInMethod.Certificate => DomainSignInMethod.Certificate,
        _ => throw RpcResults.InvalidArgument("A sign-in method (External or Certificate) is required."),
    };

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
}
