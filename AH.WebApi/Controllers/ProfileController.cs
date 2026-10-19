using AH.IdentityService;
using AH.WebApi.Auth;
using Grpc.Core;
using GrpcStatusCode = Grpc.Core.StatusCode;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AH.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ProfileController(UserServiceRpc.UserServiceRpcClient users) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfileResponse>> Get(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var id))
        {
            return Unauthorized();
        }

        try
        {
            var user = await users.GetUserAsync(new GetUserByIdRequest { Id = id }, cancellationToken: cancellationToken);
            return ToResponse(user);
        }
        catch (RpcException ex) when (ex.StatusCode == GrpcStatusCode.NotFound)
        {
            return await EndOrphanedSessionAsync();
        }
    }

    [HttpPut]
    [ProducesResponseType<ProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProfileResponse>> Update(UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var id))
        {
            return Unauthorized();
        }

        GetUserReply user;
        try
        {
            user = await users.UpdateUserProfileAsync(
                new UpdateUserProfileRequest { Id = id, FirstName = request.FirstName ?? string.Empty, LastName = request.LastName ?? string.Empty },
                cancellationToken: cancellationToken);
        }
        catch (RpcException ex) when (ex.StatusCode == GrpcStatusCode.InvalidArgument)
        {
            return Problem(title: "Invalid profile", detail: ex.Status.Detail, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (RpcException ex) when (ex.StatusCode == GrpcStatusCode.NotFound)
        {
            return await EndOrphanedSessionAsync();
        }

        await RefreshSessionAsync(user);
        return ToResponse(user);
    }

    // The session cookie outlived its user (deleted, or a recreated dev database): the cookie
    // itself is still valid, so drop it and answer 401, which sends the SPA back to /login.
    private async Task<UnauthorizedResult> EndOrphanedSessionAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Unauthorized();
    }

    // The name lives in the session cookie's claims (served by /api/auth/me), so the cookie is
    // reissued with the same properties (expiry, persistence) to show the new name right away.
    private async Task RefreshSessionAsync(GetUserReply user)
    {
        var session = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var provider = User.FindFirst(AstraHiveClaimTypes.Provider)?.Value ?? string.Empty;
        var principal = UserClaims.CreatePrincipal(user, provider, User.Identity?.AuthenticationType);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, session.Properties);
    }

    private static ProfileResponse ToResponse(GetUserReply user) => new(
        user.Id,
        user.Email,
        user.FirstName,
        user.LastName ?? string.Empty,
        user.AvatarUrl,
        user.CreatedAt.ToDateTimeOffset());
}

public sealed record UpdateProfileRequest(string? FirstName, string? LastName);

public sealed record ProfileResponse(int Id, string Email, string FirstName, string LastName, string? AvatarUrl, DateTimeOffset CreatedAt);
