using AH.Application.Workspaces;
using AH.Application.Workspaces.AddWorkspaceMember;
using AH.Application.Workspaces.ChangeWorkspaceMemberRole;
using AH.Application.Workspaces.CreateWorkspace;
using AH.Application.Workspaces.GetMyWorkspaces;
using AH.Application.Workspaces.GetWorkspaceMembers;
using AH.Application.Workspaces.RemoveWorkspaceMember;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using DomainRole = AH.AppDomain.Workspaces.WorkspaceRole;

namespace AH.IdentityService.Services;

public sealed class RpcWorkspaceService(
    GetMyWorkspacesHandler getMyWorkspaces,
    GetWorkspaceMembersHandler getWorkspaceMembers,
    AddWorkspaceMemberHandler addWorkspaceMember,
    ChangeWorkspaceMemberRoleHandler changeWorkspaceMemberRole,
    RemoveWorkspaceMemberHandler removeWorkspaceMember,
    CreateWorkspaceHandler createWorkspace,
    ILogger<RpcWorkspaceService> logger)
    : WorkspaceServiceRpc.WorkspaceServiceRpcBase
{
    public override async Task<WorkspaceListReply> GetMyWorkspaces(GetMyWorkspacesRequest request, ServerCallContext context) =>
        (await getMyWorkspaces.HandleAsync(new GetMyWorkspacesQuery(request.UserId), context.CancellationToken))
            .Unwrap(ToReply, logger, nameof(GetMyWorkspaces));

    public override async Task<WorkspaceSummary> CreateWorkspace(CreateWorkspaceRequest request, ServerCallContext context) =>
        (await createWorkspace.HandleAsync(new CreateWorkspaceCommand(request.RequesterId, request.Name), context.CancellationToken))
            .Unwrap(ToProto, logger, nameof(CreateWorkspace));

    public override async Task<WorkspaceMembersReply> GetWorkspaceMembers(WorkspaceMembersRequest request, ServerCallContext context) =>
        (await getWorkspaceMembers.HandleAsync(new GetWorkspaceMembersQuery(request.WorkspaceId, request.RequesterId), context.CancellationToken))
            .Unwrap(ToReply, logger, nameof(GetWorkspaceMembers));

    public override async Task<WorkspaceMembersReply> AddWorkspaceMember(AddWorkspaceMemberRequest request, ServerCallContext context)
    {
        var command = new AddWorkspaceMemberCommand(request.WorkspaceId, request.RequesterId, request.Email, ToDomain(request.Role));

        return (await addWorkspaceMember.HandleAsync(command, context.CancellationToken)).Unwrap(ToReply, logger, nameof(AddWorkspaceMember));
    }

    public override async Task<WorkspaceMembersReply> ChangeWorkspaceMemberRole(ChangeWorkspaceMemberRoleRequest request, ServerCallContext context)
    {
        var command = new ChangeWorkspaceMemberRoleCommand(request.WorkspaceId, request.RequesterId, request.UserId, ToDomain(request.Role));

        return (await changeWorkspaceMemberRole.HandleAsync(command, context.CancellationToken)).Unwrap(ToReply, logger, nameof(ChangeWorkspaceMemberRole));
    }

    public override async Task<WorkspaceMembersReply> RemoveWorkspaceMember(RemoveWorkspaceMemberRequest request, ServerCallContext context)
    {
        var command = new RemoveWorkspaceMemberCommand(request.WorkspaceId, request.RequesterId, request.UserId);

        return (await removeWorkspaceMember.HandleAsync(command, context.CancellationToken)).Unwrap(ToReply, logger, nameof(RemoveWorkspaceMember));
    }

    private static WorkspaceListReply ToReply(IReadOnlyList<WorkspaceSummaryResponse> workspaces) => new()
    {
        Workspaces =
        {
            workspaces.Select(ToProto),
        },
    };

    private static WorkspaceSummary ToProto(WorkspaceSummaryResponse workspace) => new()
    {
        Id = workspace.Id,
        Name = workspace.Name,
        Role = ToProto(workspace.Role),
        MemberCount = workspace.MemberCount,
        IsMain = workspace.IsMain,
    };

    private static WorkspaceMembersReply ToReply(WorkspaceMembersResponse workspace) => new()
    {
        Id = workspace.Id,
        Name = workspace.Name,
        YourRole = workspace.YourRole is { } role ? ToProto(role) : WorkspaceRole.Unspecified,
        IsMain = workspace.IsMain,
        CanManageCertificates = workspace.CanManageCertificates,
        Members =
        {
            workspace.Members.Select(m => new WorkspaceMember
            {
                UserId = m.UserId,
                Email = m.Email,
                FirstName = m.FirstName,
                LastName = m.LastName,
                AvatarUrl = m.AvatarUrl,
                Role = ToProto(m.Role),
                IsPending = m.IsPending,
                AddedAt = Timestamp.FromDateTimeOffset(m.AddedAt),
                IsGlobalAdministrator = m.IsGlobalAdministrator,
            }),
        },
    };

    private static WorkspaceRole ToProto(DomainRole role) => role switch
    {
        DomainRole.Administrator => WorkspaceRole.Administrator,
        DomainRole.Viewer => WorkspaceRole.Viewer,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null),
    };

    private static DomainRole ToDomain(WorkspaceRole role) => role switch
    {
        WorkspaceRole.Administrator => DomainRole.Administrator,
        WorkspaceRole.Viewer => DomainRole.Viewer,
        _ => throw RpcResults.InvalidArgument("A workspace role (Administrator or Viewer) is required."),
    };
}
