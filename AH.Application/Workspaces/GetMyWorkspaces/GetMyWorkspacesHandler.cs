using AH.Application.Abstractions;
using AH.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace AH.Application.Workspaces.GetMyWorkspaces;

public sealed class GetMyWorkspacesHandler(IAppDbContext db)
{
    public async Task<Result<IReadOnlyList<WorkspaceSummaryResponse>>> HandleAsync(GetMyWorkspacesQuery query, CancellationToken cancellationToken)
    {
        var workspaces = await db.Workspaces
            .AsNoTracking()
            .Where(w => w.Members.Any(m => m.UserId == query.UserId))
            .OrderByDescending(w => w.IsMain)
            .ThenBy(w => w.Name)
            .ThenBy(w => w.Id)
            .Select(w => new WorkspaceSummaryResponse(
                w.Id,
                w.Name,
                w.Members.First(m => m.UserId == query.UserId).Role,
                w.Members.Count,
                w.IsMain))
            .ToListAsync(cancellationToken);

        return workspaces;
    }
}
