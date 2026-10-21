using AH.Application.Abstractions;
using AH.Application.Common;

namespace AH.Application.Workspaces.GetWorkspaceMembers;

public sealed class GetWorkspaceMembersHandler(IAppDbContext db)
{
    public async Task<Result<WorkspaceMembersResponse>> HandleAsync(GetWorkspaceMembersQuery query, CancellationToken cancellationToken)
    {
        var workspace = await WorkspaceAccess.LoadForMemberAsync(db, query.WorkspaceId, query.RequesterId, cancellationToken);

        return workspace.IsSuccess
            ? WorkspaceMembersResponse.From(workspace.Value, query.RequesterId)
            : workspace.Error!;
    }
}
