using AH.IdentityService;
using AH.WebApi.Auth;
using Grpc.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProtoRole = AH.IdentityService.WorkspaceRole;

namespace AH.WebApi.Controllers;

// Membership and roles are checked by IdentityService on every call, against the database: the session
// cookie carries only the user id, so a role change or removal applies immediately.
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class WorkspacesController(WorkspaceServiceRpc.WorkspaceServiceRpcClient workspaces) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<WorkspaceSummaryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<WorkspaceSummaryResponse>>> GetMine(CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var reply = await workspaces.GetMyWorkspacesAsync(new GetMyWorkspacesRequest { UserId = userId }, cancellationToken: cancellationToken);
        return reply.Workspaces.Select(ToResponse).ToList();
    }

    // Only while ah.registration_enabled is on (checked by IdentityService; 403 otherwise).
    [HttpPost]
    [ProducesResponseType<WorkspaceSummaryResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<WorkspaceSummaryResponse>> Create(CreateWorkspaceRequest request, CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var reply = await workspaces.CreateWorkspaceAsync(
                new IdentityService.CreateWorkspaceRequest { RequesterId = userId, Name = request.Name ?? string.Empty },
                cancellationToken: cancellationToken);
            var workspace = ToResponse(reply);
            return CreatedAtAction(nameof(GetMembers), new { id = workspace.Id }, workspace);
        }
        catch (RpcException ex) when (RpcProblems.IsExpected(ex))
        {
            return this.ToProblem(ex, "Cannot create the workspace");
        }
    }

    [HttpGet("{id:int}/members")]
    [ProducesResponseType<WorkspaceMembersResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<ActionResult<WorkspaceMembersResponse>> GetMembers(int id, CancellationToken cancellationToken) =>
        CallAsync(
            requesterId => workspaces.GetWorkspaceMembersAsync(
                new WorkspaceMembersRequest { WorkspaceId = id, RequesterId = requesterId },
                cancellationToken: cancellationToken),
            "Cannot load workspace members");

    [HttpPost("{id:int}/members")]
    [ProducesResponseType<WorkspaceMembersResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkspaceMembersResponse>> AddMember(int id, AddWorkspaceMemberRequest request, CancellationToken cancellationToken)
    {
        var result = await CallAsync(
            requesterId => workspaces.AddWorkspaceMemberAsync(
                new IdentityService.AddWorkspaceMemberRequest
                {
                    WorkspaceId = id,
                    RequesterId = requesterId,
                    Email = request.Email ?? string.Empty,
                    Role = ToProto(request.Role),
                },
                cancellationToken: cancellationToken),
            "Cannot add the member");

        return result.Value is { } members
            ? CreatedAtAction(nameof(GetMembers), new { id }, members)
            : result;
    }

    [HttpPut("{id:int}/members/{userId:int}")]
    [ProducesResponseType<WorkspaceMembersResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public Task<ActionResult<WorkspaceMembersResponse>> ChangeMemberRole(int id, int userId, ChangeWorkspaceMemberRoleRequest request, CancellationToken cancellationToken) =>
        CallAsync(
            requesterId => workspaces.ChangeWorkspaceMemberRoleAsync(
                new IdentityService.ChangeWorkspaceMemberRoleRequest
                {
                    WorkspaceId = id,
                    RequesterId = requesterId,
                    UserId = userId,
                    Role = ToProto(request.Role),
                },
                cancellationToken: cancellationToken),
            "Cannot change the member's role");

    [HttpDelete("{id:int}/members/{userId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveMember(int id, int userId, CancellationToken cancellationToken)
    {
        var result = await CallAsync(
            requesterId => workspaces.RemoveWorkspaceMemberAsync(
                new RemoveWorkspaceMemberRequest { WorkspaceId = id, RequesterId = requesterId, UserId = userId },
                cancellationToken: cancellationToken),
            "Cannot remove the member");

        return result.Value is not null ? NoContent() : result.Result!;
    }

    private async Task<ActionResult<WorkspaceMembersResponse>> CallAsync(
        Func<int, AsyncUnaryCall<WorkspaceMembersReply>> call,
        string problemTitle)
    {
        if (!User.TryGetUserId(out var requesterId))
        {
            return Unauthorized();
        }

        try
        {
            return ToResponse(await call(requesterId));
        }
        catch (RpcException ex) when (RpcProblems.IsExpected(ex))
        {
            return this.ToProblem(ex, problemTitle);
        }
    }

    private static WorkspaceSummaryResponse ToResponse(WorkspaceSummary workspace) =>
        new(workspace.Id, workspace.Name, ToApi(workspace.Role), workspace.MemberCount, workspace.IsMain);

    private static WorkspaceMembersResponse ToResponse(WorkspaceMembersReply reply) => new(
        reply.Id,
        reply.Name,
        reply.IsMain,
        reply.YourRole == ProtoRole.Unspecified ? null : ToApi(reply.YourRole),
        reply.CanManageCertificates,
        [.. reply.Members.Select(m => new WorkspaceMemberResponse(
            m.UserId,
            m.Email,
            m.FirstName,
            m.LastName,
            m.AvatarUrl,
            ToApi(m.Role),
            m.IsPending,
            m.IsGlobalAdministrator,
            m.AddedAt.ToDateTimeOffset()))]);

    private static WorkspaceMemberRole ToApi(ProtoRole role) => role switch
    {
        ProtoRole.Administrator => WorkspaceMemberRole.Administrator,
        ProtoRole.Viewer => WorkspaceMemberRole.Viewer,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "IdentityService returned no role."),
    };

    // A missing or unknown role stays Unspecified, which IdentityService rejects as InvalidArgument (400).
    private static ProtoRole ToProto(WorkspaceMemberRole? role) => role switch
    {
        WorkspaceMemberRole.Administrator => ProtoRole.Administrator,
        WorkspaceMemberRole.Viewer => ProtoRole.Viewer,
        _ => ProtoRole.Unspecified,
    };
}

public enum WorkspaceMemberRole
{
    Administrator,
    Viewer,
}

public sealed record AddWorkspaceMemberRequest(string? Email, WorkspaceMemberRole? Role);

public sealed record ChangeWorkspaceMemberRoleRequest(WorkspaceMemberRole? Role);

public sealed record CreateWorkspaceRequest(string? Name);

// IsMain: the Global Administrator's workspace (operators only, no clusters).
public sealed record WorkspaceSummaryResponse(int Id, string Name, WorkspaceMemberRole Role, int MemberCount, bool IsMain);

// CanManageCertificates: the requester is the Global Administrator and may issue client certificates.
public sealed record WorkspaceMembersResponse(
    int Id,
    string Name,
    bool IsMain,
    WorkspaceMemberRole? YourRole,
    bool CanManageCertificates,
    IReadOnlyList<WorkspaceMemberResponse> Members);

public sealed record WorkspaceMemberResponse(
    int UserId,
    string Email,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    WorkspaceMemberRole Role,
    bool IsPending,
    bool IsGlobalAdministrator,
    DateTimeOffset AddedAt);
